using Ghostscript.NET.PDFA3Converter.ZUGFeRD;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using UpRestEye3.Data;
using UpRestEye3.Models.DTO;


namespace UpRestEye3.Services.Recognition
{

    public interface IRAGFileService
    {
        Task<List<RAGFlatRecordDTO>> GenerateRAGFileAsync(string currentConsumerTaxId);
        Task<List<RAGFlatRecordDTO>> LoadRAGFlatRecordsAsync(string path);
    }

    public class RAGFileService : IRAGFileService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly HttpClient _httpClient;
        public RAGFileDTO? RAGData { get; private set; }

        public RAGFileService(ApplicationDbContext dbContext, HttpClient httpClient)
        {
            _dbContext = dbContext;
            _httpClient = httpClient;
        }


        public async Task<List<RAGFlatRecordDTO>> GenerateRAGFileAsync(string currentConsumerTaxId)
        {


            // Получаем единицы измерения
            var units = await _dbContext.MeasureUnits
            .AsNoTracking()
            .Include(p => p.Consumer)
            .Where(p => p.Consumer.TaxNumber == currentConsumerTaxId)
            .ToListAsync();

            var unitList = units.Select(p => new RMSMeasureUnitDTO()
            {
                Id = p.Id,
                ConsumerId = p.ConsumerId,
                ConsumerTaxId = p.Consumer.TaxNumber,
                EntityExtGuid = p.EntityExtGuid,
                RootType = p.RootType,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status
            }).ToList();


            // Получаем все Invoice с продуктами и поставщиками для текущего Consumer
            var invoices = await _dbContext.Invoices
                .Where(inv => inv.Consumer.TaxNumber == currentConsumerTaxId)
                .Include(inv => inv.Supplier)
                .Include(inv => inv.Products)
                    .ThenInclude(p => p.RMSProduct)
                .Include(inv => inv.Products)
                    .ThenInclude(p => p.RMSContainer)
                .ToListAsync();

            // Собираем все продукты с нужными связями
            var allProducts = invoices
                .SelectMany(inv => inv.Products.Select(prod => new
                {
                    Invoice = inv,
                    Product = prod
                }));

            // Группируем по уникальным полям для flat-структуры
            var flatRecords = allProducts
                .Where(x => x.Invoice.Supplier != null)
                .GroupBy(x => new
                {
                    SupplierName = x.Invoice.Supplier.Name,
                    ProductName = x.Product.ProductName,
                    Unit = x.Product.Unit,
                    Container = x.Product.Container
                })
                .Select(g =>
                {
                    // Берём самую свежую запись по дате инвойса
                    var latest = g.OrderByDescending(x => x.Invoice.InvoiceDate).First();

                    // Формируем структуру RMSProduct и Containers
                    RAGRMSProductDTO? mappedRmsProduct = null;
                    if (latest.Product.RMSProduct != null)
                    {
                        mappedRmsProduct = new RAGRMSProductDTO
                        {
                            Name = latest.Product.RMSProduct.Name,
                            MainUnit = unitList.FirstOrDefault(u => u.EntityExtGuid == latest.Product.RMSProduct.MainUnit)?.Name ?? string.Empty,
                            Containers = latest.Product.RMSContainer != null
                                ? new List<RAGRMSContainerDTO>
                                {
                            new RAGRMSContainerDTO
                            {
                                Name = latest.Product.RMSContainer.Name,
                                Count = latest.Product.Count ?? 0
                            }
                                }
                                : new List<RAGRMSContainerDTO>()
                        };
                    }
                    RAGRMSContainerDTO? mappedRmsContainer = null;
                    if (latest.Product.RMSContainer != null)
                    {
                        mappedRmsContainer = new RAGRMSContainerDTO
                        {
                            Name = latest.Product.RMSContainer.Name,
                            Count = latest.Product.RMSContainer.Count

                        };
                    }

                    return new RAGFlatRecordDTO
                    {
                        SupplierName = g.Key.SupplierName,
                        InvoiceProductName = g.Key.ProductName,
                        InvoiceUnit = g.Key.Unit,
                        InvoiceContainer = g.Key.Container ?? string.Empty,
                        MappedRmsProduct = mappedRmsProduct,
                        MappedRMSContainer = mappedRmsContainer,
                        Comment = latest.Product.Comments
                    };
                })
                .ToList();


            // ---------------------------------------------------------------------
            // НОВЫЙ БЛОК: добавляем полный каталог RMS-продуктов для Consumer
            // ---------------------------------------------------------------------

            // 1. Собираем ключи RMS-продуктов, которые уже присутствуют в flatRecords (по имени + MainUnit)
            var existingRmsKeys = flatRecords
                .Where(r => r.MappedRmsProduct != null)
                .Select(r => new
                {
                    Name = r.MappedRmsProduct!.Name,
                    MainUnit = r.MappedRmsProduct!.MainUnit
                })
                .Distinct()
                .ToHashSet();

            // 2. Загружаем все RMS-продукты для текущего Consumer
            // ПРЕДПОЛОЖЕНИЕ: у вас есть DbSet<RMSProduct> RMSProducts
            // и у RMSProduct есть Consumer, Name, MainUnit, Containers (коллекция RMSContainer)
            var rmsProducts = await _dbContext.RMSProducts
                .Where(p => p.Consumer.TaxNumber == currentConsumerTaxId)
                .Include(p => p.Containers) // если навигационное свойство называется иначе — поправьте
                .ToListAsync();

            // 3. Преобразуем каждую RMS-позицию в RAGFlatRecordDTO (без привязки к Supplier/Invoice)
            var catalogRecords = rmsProducts
                .Select(rms => new RAGFlatRecordDTO
                {
                    // Для чистого RMS-каталога нет конкретного поставщика/инвойса
                    SupplierName = string.Empty,
                    InvoiceProductName = string.Empty,
                    InvoiceUnit = string.Empty,         // или другое поле, если MainUnit называется иначе
                    InvoiceContainer = string.Empty,

                    MappedRmsProduct = new RAGRMSProductDTO
                    {
                        Name = rms.Name,
                        MainUnit = unitList.FirstOrDefault(u => u.EntityExtGuid == rms.MainUnit)?.Name ?? string.Empty,

                        Containers = rms.Containers?
                            .Select(c => new RAGRMSContainerDTO
                            {
                                Name = c.Name,
                                Count = c.Count
                            })
                            .ToList() ?? new List<RAGRMSContainerDTO>()
                    },

                    MappedRMSContainer = null,
                    Comment = "RMS catalog product"
                })
                // фильтруем, чтобы не дублировать уже существующие в flatRecords RMS-продукты
                .Where(r =>
                    r.MappedRmsProduct != null &&
                    !existingRmsKeys.Contains(new
                    {
                        Name = r.MappedRmsProduct.Name,
                        MainUnit = r.MappedRmsProduct.MainUnit
                    }))
                .ToList();

            flatRecords.AddRange(catalogRecords);

            // ---------------------------------------------------------------------


            return flatRecords;
        }



        public async Task<List<RAGFlatRecordDTO>> LoadRAGFlatRecordsAsync(string path)
        {
            if (!File.Exists(path))
                return [];

            var json = await File.ReadAllTextAsync(path);
            var records = JsonSerializer.Deserialize<List<RAGFlatRecordDTO>>(json);
            return records ?? [];
        }

    }
}
