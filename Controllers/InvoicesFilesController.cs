using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EyeRestWAs.Data;
using EyeRestWAs.Models;


namespace EyeRestWAs.Controllers
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

            // Process the file as needed
            // For example, parse the file and save data to the database

            return Ok(new { filePath });
        }
    }
}
