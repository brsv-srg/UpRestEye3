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
        private readonly IInvoiceFileProcessor _imageProcessor;
        private readonly IHubContext<NotificationHub> _hubContext;

        public InvoicesFilesController(IInvoiceService invoiceService, IInvoiceFileProcessor imageProcessor, IHubContext<NotificationHub> hubContext)
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

            var uploadsFolder = "uploads";
            var fileName = file.FileName;
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Check if file exists and append a modifier if it does
            if (System.IO.File.Exists(filePath))
            {
                //if (IsFileLocked(filePath))
                //{
                    var fileExtension = Path.GetExtension(fileName);
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    fileName = $"{fileNameWithoutExtension}_{timestamp}{fileExtension}";
                    filePath = Path.Combine(uploadsFolder, fileName);
                //}
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var invoiceId = await _imageProcessor.SaveInitialInvoiceAsync(consumerId, filePath);
            if (invoiceId == 0)
                return BadRequest(new { message = "Failed to save invoice information." });

            _ = _imageProcessor.InvoiceFileProcessAsync((int)invoiceId, consumerId);

            return Ok(new { message = "File uploaded successfully, processing started." });
        }

        
        [HttpGet("{invoiceId}/scanned-image")]
        public async Task<IActionResult> GetScannedFile(int invoiceId)
        {
            // Получаем информацию о накладной
            var invoice = await _invoiceService.GetInvoiceDAOByIdAsync(invoiceId);
            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found." });
            }

            // Проверяем, указан ли путь к файлу
            if (string.IsNullOrEmpty(invoice.FilePath))
            {
                return NotFound(new { message = "Scanned file not found for this invoice." });
            }

            // Проверяем существование файла
            if (!System.IO.File.Exists(invoice.FilePath))
            {
                return NotFound(new { message = "Scanned file does not exist on the server." });
            }

            try
            {
                // Читаем файл и возвращаем его
                var fileBytes = await System.IO.File.ReadAllBytesAsync(invoice.FilePath);
                var fileName = Path.GetFileName(invoice.FilePath);
                var contentType = "application/octet-stream"; // Можно уточнить MIME-тип, если известно

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                // Логируем ошибку и возвращаем 500 Internal Server Error
                Console.WriteLine($"Error reading file: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while reading the file." });
            }
        }

        private bool IsFileLocked(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // If we can open the file with read/write access, it is not locked
                }
            }
            catch (IOException)
            {
                // The file is locked
                return true;
            }

            // The file is not locked
            return false;
        }




    }
}
