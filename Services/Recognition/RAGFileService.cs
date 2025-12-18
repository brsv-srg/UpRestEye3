using Ghostscript.NET.PDFA3Converter.ZUGFeRD;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using UpRestEye3.Data;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;


namespace UpRestEye3.Services.Recognition
{

    public interface IRAGFileService
    {
        Task<List<RAGMappingRecordDTO>> GenerateMappingRAGFileAsync(string currentConsumerTaxId);
        Task<List<RAGProductsRecordDTO>> GenerateProductsRAGFileAsync(string currentConsumerTaxId);
        
    }

    public class RAGFileService : IRAGFileService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly HttpClient _httpClient;

        public RAGFileService(ApplicationDbContext dbContext, HttpClient httpClient)
        {
            _dbContext = dbContext;
            _httpClient = httpClient;
        }


        public async Task<List<RAGMappingRecordDTO>> GenerateMappingRAGFileAsync(string currentConsumerTaxId)
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
                .Where(inv => inv.Consumer.TaxNumber == currentConsumerTaxId
                    && (inv.Stage == InvoiceStageEnum.ProductsMapping && inv.StageStatus == InvoiceStatusEnum.Ok
                        || inv.Stage == InvoiceStageEnum.SavingToSystem && inv.StageStatus == InvoiceStatusEnum.Ok))
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
                    SupplierTaxNumber = x.Invoice.Supplier.TaxNumber,
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
                            Id = (int)latest.Product.RMSProduct.Id,
                            Name = latest.Product.RMSProduct.Name,
                            MainUnit = unitList.FirstOrDefault(u => u.EntityExtGuid == latest.Product.RMSProduct.MainUnit)?.Name ?? string.Empty,
                            Containers = latest.Product.RMSProduct.Containers != null
                                ? latest.Product.RMSProduct.Containers
                                    .Select(c => new RAGRMSContainerDTO
                                    {
                                        Id = (int)c.Id,
                                        Name = c.Name,
                                        Count = c.Count
                                    })
                                    .ToList()
                                : new List<RAGRMSContainerDTO>()
                        };

                    }
                    RAGRMSContainerDTO? mappedRmsContainer = null;
                    if (latest.Product.RMSContainer != null)
                    {
                        mappedRmsContainer = new RAGRMSContainerDTO
                        {
                            Id = (int)latest.Product.RMSContainer.Id,
                            Name = latest.Product.RMSContainer.Name,
                            Count = latest.Product.RMSContainer.Count

                        };
                    }

                    string embeddingText = $"SupplierTaxNumber: {latest.Invoice.Supplier.TaxNumber} | InvoiceProductName: {latest.Product.ProductName}";


                    return new RAGMappingRecordDTO
                    {
                        SupplierTaxNumber = latest.Invoice.Supplier.TaxNumber, // g.Key.SupplierTaxNumber,
                        SupplierName = latest.Invoice.Supplier.Name, // g.Key.SupplierName,
                        InvoiceProductName = latest.Product.ProductName, // g.Key.ProductName,
                        InvoiceProductId = latest.Product.Id, // g.Key.ProductId,
                        InvoiceUnit = latest.Product.Unit, // g.Key.Unit,
                        InvoiceContainer = latest.Product.Container ?? string.Empty, // g.Key.Container ?? string.Empty,
                        MappedRmsProduct = mappedRmsProduct,
                        MappedRMSContainer = mappedRmsContainer,
                        EmbeddingText = embeddingText

                    };
                })
                .ToList();

            return flatRecords;
        }


        public async Task<List<RAGProductsRecordDTO>> GenerateProductsRAGFileAsync(string currentConsumerTaxId)
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

            // ---------------------------------------------------------------------
            // Создаем полный каталог RMS-продуктов для Consumer
            // ---------------------------------------------------------------------

            // 1. Берем все RMS-продукты для текущего Consumer
            var rmsProducts = await _dbContext.RMSProducts
                .Where(p => p.Consumer.TaxNumber == currentConsumerTaxId && p.Status == RMSProductStatusEnum.Synchronized)
                .Include(p => p.Containers) // если навигационное свойство называется иначе — поправьте
                .ToListAsync();

            // В методе GenerateProductsRAGFileAsync замените формирование EmbeddingText на следующее:
            var catalogRecords = rmsProducts
                .Select(rms =>
                {
                    var mainUnit = unitList.FirstOrDefault(u => u.EntityExtGuid == rms.MainUnit)?.Name ?? string.Empty;
                    var containers = rms.Containers?
                        .Select(c => new RAGRMSContainerDTO
                        {
                            Id = (int)c.Id,
                            Name = c.Name,
                            Count = c.Count
                        })
                        .ToList() ?? new List<RAGRMSContainerDTO>();

                  
                    var embeddingText = $"RMS Product Name: {rms.Name}";

                    return new RAGProductsRecordDTO
                    {
                        Id = (int)rms.Id,
                        Name = rms.Name,
                        MainUnit = mainUnit,
                        Containers = containers,
                        EmbeddingText = embeddingText
                    };
                }).ToList();

            

            // ---------------------------------------------------------------------


            return catalogRecords;
        }
    }
}
