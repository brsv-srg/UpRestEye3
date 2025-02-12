using UpRestEye3.Models.BLO;

namespace UpRestEye3.Models.DAO
{
    public class InvoiceDAO
    {
        public int? Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public decimal TotalIVA { get; set; } = 0.0m;
        public decimal TotalAmount { get; set; } = 0.0m;

        public int? ConsumerId { get; set; }
        public ConsumerDAO? Consumer { get; set; }

        public int? SupplierId { get; set; }
        public SupplierDAO? Supplier { get; set; } 
 
        public List<InvoiceProductDAO> Products { get; set; } = [];
        public List<TaxesDAO> TaxCategories { get; set; } = [];

        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadTime { get; set; } = DateTime.Now;
        public string Comments { get; set; } = string.Empty;
        public InvoiceStatusEnum Status { get; set; } = InvoiceStatusEnum.New;

        // Default constructor
        public InvoiceDAO()
        {
        }
         
    }

    public class TaxesDAO
    {
        public int? Id { get; set; }
        public int? InvoiceId { get; set; }
        public InvoiceDAO? Invoice { get; set; }
        public TaxCategoryEnum TaxCategory { get; set; } = TaxCategoryEnum.Intermediate;
        public decimal Base { get; set; } = 0.0m;
        public decimal IVA { get; set; } = 0.0m;
        public decimal Total { get; set; } = 0.0m;
    }

    public class InvoiceProductDAO
    {
        public int? Id { get; set; }
        public int? InvoiceId { get; set; }
        public InvoiceDAO? Invoice { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float Quantity { get; set; } = 0.0f;
        public decimal Price { get; set; } = 0.0m;
        public TaxCategoryEnum TaxCategory { get; set; } = TaxCategoryEnum.Intermediate;
        public int? RMSProductId { get; set; } = null;
        public RMSProductDAO? RMSProduct { get; set; } = null;
        public int? RMSContainerId { get; set; } = null;
        public RMSContainerDAO? RMSContainer { get; set; } = null;

    }
}

