using Microsoft.EntityFrameworkCore;
using System.Text;
using UpRestEye3.Data;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;

namespace UpRestEye3.Services.Recognition
{
    public interface IProductCatalogProvider
    {
        Task<string> GetProductCatalogTextAsync(InvoiceDTO invoice, CancellationToken ct);
    }
    public sealed class ProductCatalogProvider : IProductCatalogProvider
    {
        private readonly ApplicationDbContext _db;

        public ProductCatalogProvider(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<string> GetProductCatalogTextAsync(InvoiceDTO invoice, CancellationToken ct)
        {
            var consumerTaxId = invoice.Consumer.TaxNumber;
            if (string.IsNullOrWhiteSpace(consumerTaxId))
                throw new ArgumentException("Invoice.ConsumerTaxId is empty; cannot load product catalog.");

            // Measure units как в RAGFileService :contentReference[oaicite:6]{index=6}
            var units = await _db.MeasureUnits
                .AsNoTracking()
                .Include(p => p.Consumer)
                .Where(p => p.Consumer.TaxNumber == consumerTaxId)
                .ToListAsync(ct);

            var unitByGuid = units
                .Where(u => !string.IsNullOrWhiteSpace(u.Name))
                .ToDictionary(u => u.EntityExtGuid!, u => u.Name ?? "");

            // RMS products как в RAGFileService :contentReference[oaicite:7]{index=7}
            var rmsProducts = await _db.RMSProducts
                .AsNoTracking()
                .Where(p => p.Consumer.TaxNumber == consumerTaxId && p.Status == RMSProductStatusEnum.Synchronized)
                .Include(p => p.Containers)
                .ToListAsync(ct);

            var ordered = rmsProducts
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .ToList();

            var sb = new StringBuilder(capacity: ordered.Count * 120);

            foreach (var p in ordered)
            {
                unitByGuid.TryGetValue(p.MainUnit, out var mu);
                var mainUnit = mu;

                sb.Append("ID=").Append(p.Id)
                  .Append(" | NAME=").Append(p.Name ?? "")
                  .Append(" | UNIT=").Append(mainUnit)
                  .Append(" | CONTAINERS=[");

                if (p.Containers == null || p.Containers.Count == 0)
                {
                    sb.Append("none");
                }
                else
                {
                    // стабильный порядок контейнеров
                    var cont = p.Containers.OrderBy(c => c.Count).ThenBy(c => c.Name).ThenBy(c => c.Id);
                    var first = true;
                    foreach (var c in cont)
                    {
                        if (!first) sb.Append(" ; ");
                        first = false;

                        sb.Append("CID=").Append(c.Id)
                          .Append(",NAME=").Append(c.Name ?? "")
                          .Append(",COUNT=").Append(c.Count.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                    }
                }

                sb.Append("]");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
