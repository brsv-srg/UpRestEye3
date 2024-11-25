using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models;

namespace UpRestEye3.Services
{

    // Интерфейс сервиса
    public interface IInvoiceService
    {
        Task<Invoice> GetInvoiceByIdAsync(int id);
        Task<ActionResult<IEnumerable<Invoice>>> GetInvoicesAsync();
        Task SaveInvoiceAsync(Invoice invoice);
        Task SaveInvoicesAsync(List<Invoice> invoices);
        
    }
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;

        public InvoiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice> GetInvoiceByIdAsync(int id)
        {
            return await _context.Invoices.FindAsync(id);
        }

        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.TaxCategories)
                .Include(i => i.Products)
                .ToListAsync();
        }

        public async Task SaveInvoiceAsync(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task SaveInvoicesAsync(List<Invoice> invoices)
        {
            _context.Invoices.AddRange(invoices);
            await _context.SaveChangesAsync();
        }
    }
}
