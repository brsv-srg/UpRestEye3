using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using System.IO;
using System.Threading.Tasks;
using UpRestEye3.Components.Pages;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.DataLayer;


namespace UpRestEye3.Services.Controllers
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

            var invoiceId = await _imageProcessor.SaveInitialInvoiceAsync(consumerId, fileName, filePath);
            if (invoiceId == 0)
                return BadRequest(new { message = "Failed to save invoice information." });

//            _ = _imageProcessor.InvoiceFileProcessAsync((int)invoiceId, consumerId);

            return Ok(new { message = "File uploaded successfully, processing started.", invoiceId });
        }

        
        [HttpPost("multiupload")]
        public async Task<IActionResult> MultiUpload([FromForm] IFormFile file)
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

            return Ok(new { message = "File uploaded successfully, processing started.", filePath });

        }

        [HttpPost("createMultiInvoice")]
        public async Task<IActionResult> CreateMultiInvoice([FromBody] CreateMultiInvoiceRequest request)
        {
            if (request.files == null || request.files.Count == 0)
                return BadRequest("No files uploaded.");

            var uploadsFolder = "uploads";
            bool isAnyFileSkipped = false;


            // MULTIPAGE: Сохраняем файлы на диск/в хранилище
            foreach (var file in request.files)
            {
                if (!System.IO.File.Exists(file))
                {
                    isAnyFileSkipped = true;
                }
            }
            if (isAnyFileSkipped)
            {
                return BadRequest(new { message = "Some files do not exist on the server." });
            }

            // Save all file paths as a single string separated by semicolons
            var filePathsString = string.Join("; ", request.files);
            var firstFileName = Path.GetFileName(request.files.First());

            // Если мультистраничный, сохраняем все файлы как один invoice
            var invoiceId = await _imageProcessor.SaveInitialInvoiceAsync(request.consumerId, firstFileName, filePathsString);
            if (invoiceId == 0)
                return BadRequest(new { message = "Failed to save invoice information for files: " + filePathsString });

            return Ok(new { message = "Files uploaded successfully, processing started.", invoiceId });
        }

        public class CreateMultiInvoiceRequest
        {
            public List<string> files { get; set; }
            public int consumerId { get; set; }
            
        }


        [HttpGet("{invoiceId}/scanned-image")]
        public async Task<IActionResult> GetScannedFile(int invoiceId, [FromQuery] string? fileName)
        {
            // Получаем информацию о накладной
            var invoice = await _invoiceService.GetInvoiceDAOByIdAsync(invoiceId);
            if (invoice == null)
                return NotFound(new { message = "Invoice not found." });

            // Проверяем, указан ли путь к файлу
            if (string.IsNullOrEmpty(invoice.FilePath))
                return NotFound(new { message = "Scanned file not found for this invoice." });


            // Разбиваем FilePath на массив файлов
            var files = invoice.FilePath.Split("; ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            string targetFile = files.FirstOrDefault();
            if (!string.IsNullOrEmpty(fileName))
            {
                // Ищем файл по имени
                targetFile = files.Where(f => f.Equals(fileName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (targetFile == null)
                    return NotFound(new { message = "Requested file not found in invoice." });
            }


            try
            {
                // Проверка, конвертация и загрузка изображения
                var imageLoader = new ImageLoader();
                var image = imageLoader.LoadImage(targetFile);

                // Читаем файл и возвращаем его
                var fileBytes = imageLoader.ConvertBitmapToByteArray(image[0].Item1);
                var contentType = "application/octet-stream";
                return File(fileBytes, contentType, Path.GetFileName(targetFile));
            }
            catch (Exception ex)
            {
                // Логируем ошибку и возвращаем 500 Internal Server Error
                Console.WriteLine($"Error reading file: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while reading the file." });
            }
        }

        [HttpPost("set-status")]
        public async Task<IActionResult> SetInvoicesStatus([FromBody] SetInvoicesStatusRequest request)
        {
            if (request == null || request.invoiceIds == null || !request.invoiceIds.Any())
                return BadRequest("No invoice ids provided.");

            var result = await _invoiceService.SetInvoicesStatusAsync(request.invoiceIds, request.stage, request.stageStatus);

            if (result)
                return Ok(new { message = "Statuses updated successfully." });
            else
                return StatusCode(500, new { message = "Failed to update statuses." });
        }

        public class SetInvoicesStatusRequest
        {
            public List<int> invoiceIds { get; set; }
            public InvoiceStageEnum stage { get; set; }
            public InvoiceStatusEnum stageStatus { get; set; }
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
