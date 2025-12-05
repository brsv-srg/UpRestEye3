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
                            MainUnit = latest.Product.Unit,
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
