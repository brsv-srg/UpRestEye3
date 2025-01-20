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
 
        public List<ProductDAO> Products { get; set; } = [];
        public List<TaxesDAO> TaxCategories { get; set; } = [];

        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadTime { get; set; } = DateTime.Now;
        public string Comments { get; set; } = string.Empty;
        public InvoiceStatus Status { get; set; } = InvoiceStatus.New;

        // Default constructor
        public InvoiceDAO()
        {
        }
         
    }

    public class TaxesDAO
    {
        public int? Id { get; set; }
        public TaxCategory Category { get; set; } = TaxCategory.Intermediate;
        public decimal Base { get; set; } = 0.0m;
        public decimal IVA { get; set; } = 0.0m;
        public decimal Total { get; set; } = 0.0m;
    }

    public class ProductDAO
    {
        public int? Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float Quantity { get; set; } = 0.0f;
        public decimal Price { get; set; } = 0.0m;
    }
}

