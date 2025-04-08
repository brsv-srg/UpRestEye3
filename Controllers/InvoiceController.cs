using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.Integration;

namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Policy = "RequireAuthenticatedUser")]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IIntegrationInvoiceService _integrationService;

        public InvoicesController(IInvoiceService invoiceService, IIntegrationInvoiceService integrationService)
        {
            _invoiceService = invoiceService;
            _integrationService = integrationService;
        }

        // TODO сделать по ID of Consumer
        // todo сделать создание ActionResult в контроллере 
        [HttpGet("{consumerId}")]
        public async Task<ActionResult<IEnumerable<InvoiceDTO>>> GetInvoices(int? consumerId)
        {
            var result = await _invoiceService.GetInvoicesDTOAsync(consumerId);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpPost("save")]
        public async Task<ActionResult<InvoiceDTO>> SaveInvoice(InvoiceDTO invoice)
        {
            try
            { 
                // Валидация накладной после QR
                var validator = InvoiceValidatorBase.CreateValidator(invoice.Status);
                validator.Validate(invoice, invoice.Consumer.TaxNumber);

                var invoiceId = await _invoiceService.SaveInvoiceAsync(invoice);
                var updatedInvoice = await _invoiceService.GetInvoiceDTOByIdAsync((int)invoiceId);
                return Ok(updatedInvoice);
            }
            catch (Exception e)
            {
                invoice.Status = InvoiceStatusEnum.ProcessError;
                invoice.Comments += "; " + e.Message;
                await _invoiceService.SaveInvoiceAsync(invoice);
                Console.WriteLine(e);
                return BadRequest(invoice);
            }
        }


        [HttpPost("upload")]
        public async Task<ActionResult<InvoiceDTO>> UploadInvoice(InvoiceDTO invoiceToUpload)
        {
            try
            {
                var result = await _integrationService.PostInvoiceAsync(invoiceToUpload);
                if (result != null && result.Status != InvoiceStatusEnum.UploadError )
                {
                    await _invoiceService.SaveInvoiceAsync(invoiceToUpload);
                    return Ok(invoiceToUpload);
                }
                else
                    throw new Exception("Error uploading invoice to RMS");
            }
            catch (Exception e)
            {
                invoiceToUpload.Status = InvoiceStatusEnum.UploadError;
                invoiceToUpload.Comments += "; " + e.Message;
                await _invoiceService.SaveInvoiceAsync(invoiceToUpload);
                Console.WriteLine(e);
                return BadRequest(invoiceToUpload);
            }
            
        }
    }
}
