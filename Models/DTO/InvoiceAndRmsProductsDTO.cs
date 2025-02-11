
namespace UpRestEye3.Models.DTO
{
    public class mappedProductsDTO
    {
        public BaseInvoiceProductDTO InvoiceProduct { get; set; } = new BaseInvoiceProductDTO();
        public RMSProductDTO RMSProduct { get; set; } = new RMSProductDTO();

    }


    public class InvoiceAndRmsProductsDTO
    {
        public List<mappedProductsDTO> mappedProductListDTO { get; set; } = [];
        public string Comments { get; set; } = string.Empty;

    }

}

