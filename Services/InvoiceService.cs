using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models;
using static UpRestEye3.Models.Invoice;

namespace UpRestEye3.Services
{

    // Интерфейс сервиса
    public interface IInvoiceService
    {
        Task<Invoice> GetInvoiceByIdAsync(int id);
        Task<ActionResult<IEnumerable<Invoice>>> GetInvoicesAsync();
        Task SaveInvoiceAsync(Invoice invoice);
        Task SaveInvoicesAsync(List<Invoice> invoices);
        Task<int?> GetOrCreateSupplierIdAsync(SupplierInfo supplier);
        Task<int?> GetOrCreateConsumerIdAsync(ConsumerInfo consumer);

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
            var invoices = await _context.Invoices
                .Include(i => i.TaxCategories)
                .Include(i => i.Products)
                .Include(i => i.Supplier)
                .Include(i => i.Consumer)
                .ToListAsync();
            return invoices;
        }

        public async Task<int?> GetOrCreateSupplierIdAsync(SupplierInfo supplier)
        {
            // Проверяем существование поставщика
            var existingSupplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.TaxNumber == supplier.TaxNumber);

            if (existingSupplier != null)
            {
                // Если поставщик найден, возвращаем его
                supplier.Id = existingSupplier.Id;
                supplier.Name = existingSupplier.Name;
                supplier.BankAccount = existingSupplier.BankAccount;
                
                _context.Entry(existingSupplier).State = EntityState.Detached;
                _context.Attach(supplier);

                return supplier.Id;
            }

            // Если поставщика нет, создаём нового
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return supplier.Id;
        }

        public async Task<int?> GetOrCreateConsumerIdAsync(ConsumerInfo consumer)
        {
            // Проверяем существование потребителя
            var existingConsumer = await _context.Consumers
                .FirstOrDefaultAsync(c => c.TaxNumber == consumer.TaxNumber);
            if (existingConsumer != null)
            {
                // Если потребитель найден, то возвращаем его
                consumer.Id = existingConsumer.Id;
                consumer.Name = existingConsumer.Name;
                
                _context.Entry(existingConsumer).State = EntityState.Detached;
                _context.Attach(consumer);

                return consumer.Id;
            }
            // Если потребителя нет, создаём нового
            _context.Consumers.Add(consumer);
            await _context.SaveChangesAsync();
            return consumer.Id;
        }

        public async Task SaveInvoiceAsync(Invoice invoice)
        {
            invoice.SupplierId = await GetOrCreateSupplierIdAsync(invoice.Supplier);
            invoice.ConsumerId = await GetOrCreateConsumerIdAsync(invoice.Consumer);
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
