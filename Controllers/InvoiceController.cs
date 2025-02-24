using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;

namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Policy = "RequireAuthenticatedUser")]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
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
            var invoiceId = await _invoiceService.SaveInvoiceAsync(invoice);
            var updatedInvoice = await _invoiceService.GetInvoiceDTOByIdAsync((int)invoiceId);
            return Ok(updatedInvoice);
        }
    }
}
