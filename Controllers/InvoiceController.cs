using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;

namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceDTO>>> GetInvoices()
        {
            return await _invoiceService.GetInvoicesDTOAsync();
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceDTO>> SaveInvoice(InvoiceDTO invoice)
        {
            var invoiceId = await _invoiceService.SaveInvoiceAsync(invoice);
            return CreatedAtAction(nameof(GetInvoices), new { id = invoiceId }, invoice);
        }
    }
}
