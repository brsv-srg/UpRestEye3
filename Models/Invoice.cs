using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;


namespace UpRestEye3.Models
{
    public class Invoice
    {
        [JsonIgnore]
        public int? Id { get; set; }


        [JsonIgnore]
        public int? SupplierId { get; set; }
        public SupplierInfo Supplier { get; set; } // = new SupplierInfo();


        [JsonIgnore]
        public int? ConsumerId { get; set; }
        public ConsumerInfo Consumer { get; set; } // = new ConsumerInfo();

        public InvoiceInfo Info { get; set; } = new InvoiceInfo();
        public List<Product> Products { get; set; } // = [];
        public List<Taxes> TaxCategories { get; set; } // = [];

        [JsonIgnore]
        public string? FilePath { get; set; }
        [JsonIgnore]
        public DateTime UploadTime { get; set; } = DateTime.Now;

        public string? Comments { get; set; }
        
        [JsonIgnore]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.New;

        // Default constructor
        public Invoice()
        {
        }

        [Owned]
        public class InvoiceInfo
        {
            public string InvoiceNumber { get; set; } = string.Empty;
            public DateTime InvoiceDate { get; set; }
            public decimal TotalIVA { get; set; }
            public decimal TotalAmount { get; set; }
        }
    }
    public enum InvoiceStatus
    {
        New,
        RawFile,
        QRCodeProcessed,
        TextProcessed,
        ProductsMapped,
        SavedToSystem,
        Error
    }

    public enum TaxCategory
    {
        Normal,
        Intermediate,
        Reduced,
        Zero
    }   

    public class SupplierInfo
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string? BankAccount { get; set; } = string.Empty;
    }
    
    public class ConsumerInfo
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
    }

    public class Taxes
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public TaxCategory Category { get; set; } = TaxCategory.Intermediate;
        public decimal Base { get; set; } = 0.0m;
        public decimal IVA { get; set; } = 0.0m;
        public decimal Total { get; set; } = 0.0m;
    }

    public class Product
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float Quantity { get; set; } = 0.0f;
        public decimal Price { get; set; } = 0.0m;
    }
}

