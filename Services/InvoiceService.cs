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
       

    }

    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICustomerService _customerService;
        private readonly ISupplierService _supplierService;

        public InvoiceService(ApplicationDbContext context, ICustomerService customerService, ISupplierService supplierService)
        {
            _context = context;
            _customerService = customerService;
            _supplierService = supplierService;
        }

        public async Task<Invoice> GetInvoiceByIdAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.TaxCategories)
                .Include(i => i.Products)
                .Include(i => i.Supplier)
                .Include(i => i.Consumer)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoicesAsync()
        {
            var invoices = await _context.Invoices
                .Include(i => i.TaxCategories)
                .Include(i => i.Products)
                .Include(i => i.Supplier)
                .Include(i => i.Consumer)
                .ToListAsync();
            return invoices;
        }

        

        public async Task SaveInvoiceAsync(Invoice invoice)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Save or update Supplier
                if (invoice.Supplier != null)
                {
                    invoice.SupplierId = await _supplierService.GetOrCreateSupplierIdAsync(invoice.Supplier);
                    _context.Entry(invoice.Supplier).State = EntityState.Unchanged;
                }

                // Save or update Consumer
                if (invoice.Consumer != null)
                {
                    invoice.ConsumerId = await _customerService.GetOrCreateConsumerIdAsync(invoice.Consumer);
                    _context.Entry(invoice.Consumer).State = EntityState.Unchanged;
                }

                // Save or update Invoice
                var existingInvoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.Id == invoice.Id);
                if (existingInvoice == null)
                {
                    _context.Invoices.Add(invoice);
                }
                else
                {
                    _context.Entry(existingInvoice).CurrentValues.SetValues(invoice);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();


            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
