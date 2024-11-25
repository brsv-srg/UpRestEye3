using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models;
using UpRestEye3.Services;
using UpRestEye3.Components.Pages;

namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices()
        {
            return await _invoiceService.GetInvoicesAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Invoice>> SaveInvoice(Invoice invoice)
        {
            await _invoiceService.SaveInvoiceAsync(invoice);
            return CreatedAtAction(nameof(GetInvoices), new { id = invoice.Id }, invoice);
        }

        [HttpPost("batch")]
        public async Task<ActionResult<IEnumerable<Invoice>>> SaveInvoices(List<Invoice> invoices)
        {
            await _invoiceService.SaveInvoicesAsync(invoices);
            return CreatedAtAction(nameof(GetInvoices), invoices);
        }
    }
}
