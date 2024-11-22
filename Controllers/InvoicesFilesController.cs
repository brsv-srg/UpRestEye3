using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models;
using Microsoft.Extensions.DependencyInjection;


namespace UpRestEye3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesFilesController : ControllerBase
    {
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
            var invoice = await fileProcessor.ProcessFileAsync(filePath);
            
            if (invoice == null)
            {
                return BadRequest("Failed to recognize QR code or invalid data.");
            }

            // Save the invoice to the database or perform other actions as needed
            using (var context = new ApplicationDbContext(new DbContextOptions<ApplicationDbContext>()))
            {
                context.Invoices.Add(invoice);
                await context.SaveChangesAsync();
            }

            return Ok(new { filePath });
        }
    }
}
