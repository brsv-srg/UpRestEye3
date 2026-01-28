using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Tensorflow;
using UpRestEye3.Components.Pages;
using UpRestEye3.Data;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;


namespace UpRestEye3.Services.Recognition
{
    public interface IInvoiceMappingHistoryRepository
    {
        Task<List<StoredExactHit>> FindLatestExactMatchesAsync(
            int consumerId,
            string supplierTaxNumber,
            List<NormalizedInvoiceKey> keys,
            CancellationToken ct);

        Task<Dictionary<int, List<StoredCandidate>>> FindNameCandidatesAsync(
            int consumerId,
            string supplierTaxNumber,
            List<NormalizedInvoiceKey> keys,
            CancellationToken ct);
    }

    public sealed class InvoiceMappingHistoryRepository : IInvoiceMappingHistoryRepository
    {
        private readonly ApplicationDbContext _db;

        public InvoiceMappingHistoryRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<StoredExactHit>> FindLatestExactMatchesAsync(
            int consumerId,
            string supplierTaxNumber,
            List<NormalizedInvoiceKey> keys,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(supplierTaxNumber) || keys == null || keys.Count == 0)
                return new List<StoredExactHit>();

            // Вытаскиваем ВСЮ историю по поставщику в успешных стадиях, затем матчим в памяти по нормализованным ключам.
            // Это соответствует подходу RAGFileService: вы тоже грузите инвойсы с продуктами, supplier, RMSProduct, RMSContainer. :contentReference[oaicite:1]{index=1}
            var invoices = await _db.Invoices
                .AsNoTracking()
                .Where(inv =>
                    inv.Supplier != null &&
                    inv.Supplier.TaxNumber == supplierTaxNumber &&
                    (
                        (inv.Stage == InvoiceStageEnum.ProductsMapping && inv.StageStatus == InvoiceStatusEnum.Ok) ||
                        (inv.Stage == InvoiceStageEnum.SavingToSystem && inv.StageStatus == InvoiceStatusEnum.Ok)
                    ) &&
                    inv.ConsumerId == consumerId
                    )
                .Include(inv => inv.Supplier)
                .Include(inv => inv.Products)
                    .ThenInclude(p => p.RMSProduct)
                        .ThenInclude(rp => rp.Containers)
                .Include(inv => inv.Products)
                    .ThenInclude(p => p.RMSContainer)
                .ToListAsync(ct);

            // Индекс: (nameNorm, containerNorm, countNormRounded) -> freshest
            // keys уже нормализованы в GPTMappingEnvironment (NormalizeName/NormalizeContainer/NormalizeCount)
            var dict = new Dictionary<(string name, string cont, decimal? count), (DateTime date, StoredExactHit hit)>();

            foreach (var inv in invoices)
            {
                var invDate = inv.InvoiceDate; // как в RAGFileService: OrderByDescending(inv.InvoiceDate) :contentReference[oaicite:2]{index=2}

                foreach (var prod in inv.Products)
                {
                    if (prod.RMSProduct == null)
                        continue;

                    var nameNorm = InvoiceHelper.NormalizeName(prod.ProductName ?? "");
                    var contNorm = InvoiceHelper.NormalizeContainer(prod.Container ?? "");
                    var countNorm = InvoiceHelper.NormalizeCount(prod.Count ?? 0m);

                    var k = (nameNorm, contNorm, countNorm);

                    var hit = new StoredExactHit(
                        ProductId: 0, // будет выставлено ниже под конкретный входной ключ
                        RmsProduct: MapRmsProduct(prod.RMSProduct),
                        RmsContainer: prod.RMSContainer != null ? MapRmsContainer(prod.RMSContainer) : null,
                        StorageName: TryGetStorageName(prod),
                        TaxCategoryCodeOrName: TryGetTaxCategory(prod)
                    );

                    if (!dict.TryGetValue(k, out var existing) || invDate > existing.date)
                        dict[k] = (invDate, hit);
                }
            }

            // Теперь раздаём результат по входным keys
            var result = new List<StoredExactHit>(keys.Count);

            foreach (var key in keys)
            {
                // ключи пришли как NormalizeName/NormalizeContainer — чтобы совпадали с Canon(),
                // Canon() должен быть согласован с вашей NormalizeName().
                // Если вы используете более жёсткую нормализацию — замените Canon на ту же функцию.
                var k = (key.NameNorm, key.ContainerNorm, key.CountNorm);

                if (dict.TryGetValue(k, out var stored))
                {
                    result.Add(stored.hit with { ProductId = key.ProductId });
                }
            }

            return result;
        }

        public async Task<Dictionary<int, List<StoredCandidate>>> FindNameCandidatesAsync(
            int consumerId,
            string supplierTaxNumber,
            List<NormalizedInvoiceKey> keys,
            CancellationToken ct)
        {
            var result = new Dictionary<int, List<StoredCandidate>>();
            if (string.IsNullOrWhiteSpace(supplierTaxNumber) || keys == null || keys.Count == 0)
                return result;

            // Грузим историю по supplier, как в RAGFileService :contentReference[oaicite:3]{index=3}
            var invoices = await _db.Invoices
                .AsNoTracking()
                .Where(inv =>
                    inv.Supplier != null &&
                    inv.Supplier.TaxNumber == supplierTaxNumber &&
                    (
                        (inv.Stage == InvoiceStageEnum.ProductsMapping && inv.StageStatus == InvoiceStatusEnum.Ok) ||
                        (inv.Stage == InvoiceStageEnum.SavingToSystem && inv.StageStatus == InvoiceStatusEnum.Ok)
                    ) &&
                    inv.ConsumerId == consumerId
                )
                .Include(inv => inv.Supplier)
                .Include(inv => inv.Products)
                    .ThenInclude(p => p.RMSProduct)
                        .ThenInclude(rp => rp.Containers)
                .Include(inv => inv.Products)
                    .ThenInclude(p => p.RMSContainer)
                .ToListAsync(ct);

            // Индекс: nameNorm -> list of historical rows (freshness important)
            var byName = new Dictionary<string, List<(DateTime date, InvoiceProductDAO prod)>>();

            foreach (var inv in invoices)
            {
                var date = inv.InvoiceDate;

                foreach (var prod in inv.Products ?? Enumerable.Empty<InvoiceProductDAO>())
                {
                    if (prod.RMSProduct == null)
                        continue;

                    var nameNorm = InvoiceHelper.NormalizeName(prod.ProductName ?? "");


                    if (!byName.TryGetValue(nameNorm, out var list))
                    {
                        list = new List<(DateTime, InvoiceProductDAO)>();
                        byName[nameNorm] = list;
                    }

                    list.Add((date, prod));
                }
            }

            // Для каждого входного invoice product: вернуть уникальные RMS продукты (+контейнеры), ранее привязанные к этому имени
            foreach (var key in keys)
            {

                if (!byName.TryGetValue(key.NameNorm, out var rows) || rows.Count == 0)
                    continue;

                // сортируем от новых к старым — чтобы hints брались из самой свежей маппинговой записи
                rows.Sort((a, b) => b.date.CompareTo(a.date));

                // Unique RMS by Id
                var unique = new Dictionary<int, StoredCandidate>();

                foreach (var row in rows)
                {
                    var rms = row.prod.RMSProduct!;
                    var rmsId = (int)rms.Id;

                    if (!unique.TryGetValue(rmsId, out var candidate))
                    {
                        candidate = new StoredCandidate(
                            ProductId: key.ProductId,
                            RmsProduct: MapRmsProduct(rms),
                            Containers: rms.Containers?.Select(MapRmsContainer).ToList() ?? new List<RMSContainerDTO>(),
                            LastChosenContainer: row.prod.RMSContainer != null ? MapRmsContainer(row.prod.RMSContainer) : null,
                            LastChosenStorage: TryGetStorageName(row.prod),
                            LastChosenTaxCategory: TryGetTaxCategory(row.prod)
                        );
                        unique[rmsId] = candidate;
                    }
                    else
                    {
                        // если уже есть, но у нового ряда есть выбранный контейнер — можно обновить LastChosenContainer на самый свежий
                        if (candidate.LastChosenContainer == null && row.prod.RMSContainer != null)
                        {
                            unique[rmsId] = candidate with
                            {
                                LastChosenContainer = MapRmsContainer(row.prod.RMSContainer),
                                LastChosenStorage = candidate.LastChosenStorage ?? TryGetStorageName(row.prod),
                                LastChosenTaxCategory = candidate.LastChosenTaxCategory ?? TryGetTaxCategory(row.prod)
                            };
                        }
                    }
                }

                result[key.ProductId] = unique.Values.ToList();
            }

            return result;
        }

        // ---------------- helpers ----------------

        private static RMSProductDTO MapRmsProduct(RMSProductDAO rms)
        {
            return new RMSProductDTO
            {
                Id = (int)rms.Id,
                Name = rms.Name ?? "",
                Description = rms.Description ?? "",
                Num = rms.Num,
                MainUnit = rms.MainUnit, // если у вас хранится GUID, то тут лучше конвертить как в RAGFileService через MeasureUnits :contentReference[oaicite:4]{index=4}

                Containers = rms.Containers?.Select(MapRmsContainer).ToList() ?? new List<RMSContainerDTO>()
            };
        }

        private static RMSContainerDTO MapRmsContainer(RMSContainerDAO c)
        {
            return new RMSContainerDTO
            {
                Id = (int)c.Id,
                Num = c.Num,
                Name = c.Name ?? "",
                Count = c.Count
            };
        }

        private static string? TryGetStorageName(InvoiceProductDAO prod)
        {
            // В RAGFileService storage не сохраняется.
            // Здесь — мягкий доступ: если у вас есть поле Storage/StorageName — подставьте.
            // Например:
            // return prod.Storage?.Name ?? prod.StorageName;
            return null;
        }

        private static string? TryGetTaxCategory(InvoiceProductDAO prod)
        {
            // Аналогично: если в вашей модели есть TaxCategory — подставьте.
            // Например:
            // return prod.TaxCategory?.Code ?? prod.TaxCategoryName;
            return null;
        }
    }
}
