using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using UpRestEye3.Components.Pages;
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
        private readonly IInvoiceFileProcessor _invoiceFileService;
        private readonly IIntegrationInvoiceService _integrationService;

        public InvoicesController(IInvoiceService invoiceService, IInvoiceFileProcessor invoiceFileService, IIntegrationInvoiceService integrationService)
        {
            _invoiceService = invoiceService;
            _invoiceFileService = invoiceFileService;
            _integrationService = integrationService;
        }

        // TODO сделать по ID of Consumer
        // todo сделать создание ActionResult в контроллере 
        [HttpGet("{consumerId}")]
        public async Task<ActionResult<IEnumerable<InvoiceDTO>>> GetInvoices(int? consumerId, [FromQuery] int rmsProductId )
        {
            var result = await _invoiceService.GetInvoicesDTOAsync(consumerId, rmsProductId);
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
                CheckQR(invoice);

                // Валидация накладной после QR
                var validator = InvoiceValidatorBase.CreateValidator(invoice.Stage);
                validator.Validate(invoice, invoice.Consumer.TaxNumber, true);

                var invoiceId = await _invoiceService.SaveInvoiceAsync(invoice);
                var updatedInvoice = await _invoiceService.GetInvoiceDTOByIdAsync((int)invoiceId);
                return Ok(updatedInvoice);
            }
            catch (Exception e)
            {
                invoice.StageStatus = InvoiceStatusEnum.Manual;
                if(!string.IsNullOrEmpty(invoice.Comments))
                    invoice.Comments += "; " ;
                invoice.Comments += e.Message;

                await _invoiceService.SaveInvoiceAsync(invoice);
                Console.WriteLine(e);
                return BadRequest(invoice);
            }
        }


        //todo переделать на общий процессный движок

        [HttpPost("upload")]
        public async Task<ActionResult<InvoiceDTO>> UploadInvoice(InvoiceDTO invoiceToUpload)
        {
            var result = await _integrationService.PostInvoiceAsync(invoiceToUpload);
            await _invoiceService.SaveInvoiceAsync(invoiceToUpload);
            if (result != null && result.Stage == InvoiceStageEnum.SavingToSystem && result.StageStatus != InvoiceStatusEnum.Error)
            {
                return Ok(invoiceToUpload);
            }
            else
            {
                Console.WriteLine(invoiceToUpload.Comments);
                return BadRequest(invoiceToUpload);
            }
        }

        

        [HttpPost("process/{consumerId}")]
        public async Task<IActionResult> ProcessSelectedInvoices(int consumerId, [FromBody] List<int> invoiceIds)
        {
            if (invoiceIds == null || !invoiceIds.Any())
            {
                return BadRequest("No invoice IDs provided.");
            }

            foreach (var id in invoiceIds)
            {
                _ = Task.Run(() => _invoiceFileService.InvoiceFileProcessAsync(id, (int)consumerId));
            }
            return Ok("Invoices are being processed asynchronously.");

        }


        [HttpPost("delete")]
        public async Task<IActionResult> DeleteInvoices([FromBody] List<int> invoiceIds)
        {
            if (invoiceIds == null || !invoiceIds.Any())
            {
                return BadRequest("No invoice IDs provided.");
            }

            try
            {
                await _invoiceService.DeleteInvoicesAsync(invoiceIds);
                return Ok("Invoices deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting invoices: {ex.Message}");
            }
        }

        private void CheckQR(InvoiceDTO invoice)
        {
         if (invoice.Stage == InvoiceStageEnum.QRCodeRecognition && invoice.StageStatus != InvoiceStatusEnum.Ok)
         {
            if (!string.IsNullOrEmpty(invoice.Comments) && QRCodeData.IsMatchingATQRCode(invoice.Comments))
            {
                var basicQRCode = new QRCodeData(invoice.Comments);
                InvoiceHelper.UpdateInvoiceByQR(invoice, basicQRCode, invoice.FilePath);
            }
         }
        }
    }
}
