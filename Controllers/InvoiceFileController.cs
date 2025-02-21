using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.DataLayer;
using System.IO;
using System.Threading.Tasks;


namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Policy = "RequireAuthenticatedUser")]
    public class InvoicesFilesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IImageFileProcessor _imageProcessor;
        private readonly IHubContext<NotificationHub> _hubContext;

        public InvoicesFilesController(IInvoiceService invoiceService, IImageFileProcessor imageProcessor, IHubContext<NotificationHub> hubContext)
        {
            _invoiceService = invoiceService;
            _imageProcessor = imageProcessor;
            _hubContext = hubContext;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] int consumerId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var filePath = Path.Combine("uploads", file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _ = _imageProcessor.FileProcessAsync(filePath, consumerId); 

            return Ok(new { message = "File uploaded successfully, processing started." });
        }
    }
}
