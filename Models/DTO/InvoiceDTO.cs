using UpRestEye3.Models.BLO;
using UpRestEye3.Services.BusinessLogic;

namespace UpRestEye3.Models.DTO
{
    public class InvoiceDTO
    {
        public int? Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public decimal TotalIVA { get; set; } = 0.0m;
        public decimal TotalAmount { get; set; } = 0.0m;
        public ConsumerDTO? Consumer { get; set; } 
        public SupplierDTO? Supplier { get; set; }

        public List<InvoiceProductDTO> Products { get; set; } = [];
        
        public List<TaxesDTO> TaxCategories { get; set; } = [];

        public string FilePath { get; set; } = string.Empty;

        public DateTime UploadTime { get; set; } = DateTime.Now;

        public string Comments { get; set; } = string.Empty;

        public bool IsSelected { get; set; } = false;

        public bool ProductsTaxIncluded { get; set; } = false;

        public InvoiceStageEnum Stage { get; set; } = InvoiceStageEnum.New;
        public InvoiceStatusEnum StageStatus { get; set; } = InvoiceStatusEnum.Ok;

        // Default constructor
        public InvoiceDTO()
        {
        }

    }    
    
    public class TaxesDTO
    {
        public int? Id { get; set; }
        public TaxCategoryEnum TaxCategory { get; set; } = TaxCategoryEnum.Intermediate;
        public decimal Base { get; set; } = 0.0m;
        public decimal IVA { get; set; } = 0.0m;
        public decimal Total { get; set; } = 0.0m;

        public decimal Percentage
        {
            get
            {
                return InvoiceHelper.GetTaxCategoryPercent(TaxCategory);
            }
        }
    }
  
    public class InvoiceProductDTO
    {
        public int? Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 0.0m;
        public string? Container { get; set; }
        public decimal? Count { get; set; } 
        public decimal ProductTotalValue { get; set; } = 0.0m;
        public TaxCategoryEnum TaxCategory { get; set; } = TaxCategoryEnum.Intermediate;
        public RMSProductDTO? RMSProduct { get; set; }
        public RMSContainerDTO? RMSContainer { get; set; }
        public RMSAccountDTO? RMSStorage { get; set; }
        public string Comments { get; set; } = string.Empty;
    }


    public class SetInvoicesStatusRequest
    {
        public List<int?> invoiceIds { get; set; } = [];
        public InvoiceStageEnum stage { get; set; }
        public InvoiceStatusEnum stageStatus { get; set; }
    }


}

