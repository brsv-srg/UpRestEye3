using System.Drawing;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.MLServices;
using UpRestEye3.Services.Recognition;



namespace UpRestEye3.Services.BusinessLogic
{



    public interface IInvoiceProcessor
    {
        Task<InvoiceAndRmsProductsDTO> MappingToRMSProductsAsync(InvoiceDTO currentInvoice, List<RMSProductDTO> rmsProducts);
    }

    // Класс обработки изображения
    public class InvoiceProcessor : IInvoiceProcessor
    {
        private readonly IGPTService _gptParser;

        public InvoiceProcessor(IGPTService gptParser)
        {
            _gptParser = gptParser;
        }

        public async Task<InvoiceAndRmsProductsDTO> MappingToRMSProductsAsync(InvoiceDTO currentInvoice, List<RMSProductDTO> rmsProducts)
        {
            return await _gptParser.ReceiptMappingByLLM(currentInvoice, rmsProducts);
        }

    }
}