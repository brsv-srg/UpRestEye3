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
        private readonly IImageProcessor _imageProcessor;



        public InvoicesFilesController(IInvoiceService invoiceService, IImageProcessor imageProcessor)
        {
            _invoiceService = invoiceService;
            _imageProcessor = imageProcessor;
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

            Invoice invoice = await _imageProcessor.ExtProcessImageAsync(filePath);

            if (invoice == null)
            {
                return BadRequest("Failed to recognize invoice image.");
            }

            invoice.FilePath = filePath;

            await _invoiceService.SaveInvoiceAsync(invoice);

            return Ok(new { filePath });
        }
    }
}
