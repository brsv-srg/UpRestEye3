using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using UpRestEye3.Controllers;
using UpRestEye3.Components.Pages;
using UpRestEye3.Services;


namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesFilesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;


        public InvoicesFilesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var filePath = Path.Combine("uploads", file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Process the file to recognize QR code and fill Invoice
            var fileProcessor = new FileProcessor();
            QRCodeData //qrInvoice = await fileProcessor.ProcessFileAsync(filePath);
            //if (qrInvoice == null)
            qrInvoice = await fileProcessor.AutoProcessAndDecodeQRCode5(filePath);
            //if (qrInvoice == null)
            //qrInvoice = await fileProcessor.AutoProcessAndDecodeQRCode4(filePath);




            //if (qrInvoice == null)
            //qrInvoice = await fileProcessor.AutoProcessAndDecodeQRCode2(filePath);
            //if (qrInvoice == null)
            //qrInvoice = await fileProcessor.AutoProcessAndDecodeQRCode3(filePath);



            if (qrInvoice == null)
            {
                return BadRequest("Failed to recognize QR code or invalid data.");
            }

            var invoice = new Invoice(qrInvoice);
            invoice.FilePath = filePath;

            await _invoiceService.SaveInvoiceAsync(invoice);

            return Ok(new { filePath });
        }
    }
}
