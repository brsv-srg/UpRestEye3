using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.DataLayer;

namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Policy = "RequireAuthenticatedUser")]
    public class InvoicesFilesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IInvoiceFileService _imageProcessor;



        public InvoicesFilesController(IInvoiceService invoiceService, IInvoiceFileService imageProcessor)
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

            var result = await _imageProcessor.FileProcessAsync(filePath); // ExtProcessImageAsync(filePath);

            if (!result)
            {
                return BadRequest("Failed to recognize invoice image.");
            }
            else
            {
                return Ok("Invoice image recognized successfully.");
            }

        }
    }
}
