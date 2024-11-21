using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EyeRestWAs.Data;
using EyeRestWAs.Models;

namespace EyeRestWAs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InvoicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices()
        {
            return await _context.Invoices
                .Include(i => i.TaxCategories)
                .Include(i => i.Products)
                .ToListAsync();
        }
    }
}
