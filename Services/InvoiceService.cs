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
        Task SaveInvoiceAsync(Invoice invoice, bool isList = false);
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
            if (supplier == null)
                return null;
            // Проверяем существование поставщика
            var existingSupplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.TaxNumber == supplier.TaxNumber);
            // TODO сделать апдейт имени существующего поставщика (и потребителя)
            if (existingSupplier != null)
            {
                if (existingSupplier.Id == supplier.Id &&
                    (string.IsNullOrWhiteSpace(existingSupplier.Name) &&  !string.IsNullOrWhiteSpace(supplier.Name) ||
                    string.IsNullOrWhiteSpace(existingSupplier.BankAccount) &&  !string.IsNullOrWhiteSpace(supplier.BankAccount)))
                {
                    // Если потребитель найден, и его имя или счет отличаются от имени в базе, то обновляем
                    _context.Suppliers.Update(supplier);
                    _context.Entry(existingSupplier).State = EntityState.Detached;
                }
                else
                if (existingSupplier.Id != supplier.Id)
                {
                    // Если потребитель найден, то берем его
                    _context.Entry(supplier).State = EntityState.Detached;
                    supplier = existingSupplier;
                }
                return supplier.Id;
            }
            else
            {
                // Если поставщика еще нет, создаём нового
                _context.Suppliers.Add(supplier);
                return supplier.Id;
            }
        }

        public async Task<int?> GetOrCreateConsumerIdAsync(ConsumerInfo consumer)
        {
            if (consumer == null)
                return null;
            // Проверяем существование потребителя
            var existingConsumer = await _context.Consumers
                .FirstOrDefaultAsync(c => c.TaxNumber == consumer.TaxNumber);
            if (existingConsumer != null)
            {
                if (existingConsumer.Id == consumer.Id &&
                    string.IsNullOrWhiteSpace(existingConsumer.Name) && 
                    !string.IsNullOrWhiteSpace(consumer.Name))
                {
                    // Если потребитель найден, и его имя отличается от имени в базе, то обновляем имя
                    _context.Consumers.Update(consumer);
                    _context.Entry(existingConsumer).State = EntityState.Detached;
                }
                else
                if (existingConsumer.Id != consumer.Id)
                {
                    // Если потребитель найден, то берем его
                    _context.Entry(consumer).State = EntityState.Detached;
                    consumer = existingConsumer;   
                }
                return consumer.Id;
            }
            else
            {
                // Если потребителя еще нет, создаём нового
                _context.Consumers.Add(consumer);
                return consumer.Id;
            }
        }

        public async Task SaveInvoiceAsync(Invoice invoice, bool isList = false )
        {
            invoice.SupplierId = 
                await GetOrCreateSupplierIdAsync(invoice.Supplier);
            invoice.ConsumerId = 
                await GetOrCreateConsumerIdAsync(invoice.Consumer);
            
            if (invoice.Id == null)
                _context.Invoices.Add(invoice);
            else
                _context.Invoices.Update(invoice);

            if(!isList)
                await _context.SaveChangesAsync();
        }

        public async Task SaveInvoicesAsync(List<Invoice> invoices)
        {
            foreach (var invoice in invoices)
                await SaveInvoiceAsync(invoice, true);

            //_context.Invoices.AddRange(invoices);
            await _context.SaveChangesAsync();
        }
    }
}
