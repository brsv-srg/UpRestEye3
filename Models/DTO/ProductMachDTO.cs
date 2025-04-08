using UpRestEye3.Models.BLO;

namespace UpRestEye3.Models.DTO
{

    public class InvoiceProductMappingDTO
    {
        public int? Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 0.0m;
        public string? Container { get; set; } 
        public decimal? Count { get; set; }

       
    }

    public class RMSProductMappingDTO
    {

        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Num { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public List<RMSContainerMappingDTO> Containers { get; set; } = new List<RMSContainerMappingDTO>();

    }

    public class RMSContainerMappingDTO
    {
        public int? Id { get; set; }
        public string Num { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Count { get; set; } = 0;
    }




    public class MatchedInvoiceProduct
    {
        public InvoiceProductMappingDTO InvoiceProduct { get; set; }
        public RMSProductMappingDTO RMSProduct { get; set; }
        public RMSContainerMappingDTO RMSContainer { get; set; }
        public bool NewRMSProduct { get; set; }
        public bool NewRMSContainer { get; set; }
        public string Storage { get; set; }
        public string Comments { get; set; }
    }

    public class MatchedInvoiceProducts : List<MatchedInvoiceProduct>
    {
    }


    

}
