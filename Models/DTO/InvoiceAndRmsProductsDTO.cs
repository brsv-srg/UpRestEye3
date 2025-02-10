
namespace UpRestEye3.Models.DTO
{
    public class InvoiceAndRmsProductsDTO
    {
        public InvoiceDTO Invoice { get; set; } = new InvoiceDTO();
        public List<RMSProductDTO> RMSProducts { get; set; } = [];

    }
}

