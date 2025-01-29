using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.BusinessLogic;


namespace UpRestEye3.Services.DataLayer
{

    // Интерфейс сервиса
    public interface IInvoiceService
    {
        Task<InvoiceDAO?> GetInvoiceDAOByIdAsync(int id);
        Task<InvoiceDTO?> GetInvoiceDTOByIdAsync(int id);

        Task<IEnumerable<InvoiceDAO>> GetInvoicesDAOAsync(int? consumerId);
        Task<IEnumerable<InvoiceDTO>> GetInvoicesDTOAsync(int? consumerId);

        Task<int?> SaveInvoiceAsync(InvoiceDTO invoice);
        Task<int?> SaveInvoiceAsync(InvoiceDAO invoice);
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConsumerService _consumerService;
        private readonly ISupplierService _supplierService;

        public InvoiceService(ApplicationDbContext context, IConsumerService consumerService, ISupplierService supplierService)
        {
            _context = context;
            _consumerService = consumerService;
            _supplierService = supplierService;
        }

        public async Task<InvoiceDAO?> GetInvoiceDAOByIdAsync(int id)
        {
            return await _context.Invoices
                .AsNoTracking()
                .Include(i => i.TaxCategories)
                .Include(i => i.Products)
                .Include(i => i.Supplier)
                .Include(i => i.Consumer)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<InvoiceDTO?> GetInvoiceDTOByIdAsync(int id)
        {
            return InvoiceHelper.BuildInvoiceDTO(await GetInvoiceDAOByIdAsync(id));
        }

        public async Task<IEnumerable<InvoiceDAO>> GetInvoicesDAOAsync(int? consumerId)
        {
            var invoices = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.TaxCategories)
                .Include(i => i.Products)
                .Include(i => i.Supplier)
                .Include(i => i.Consumer)
                .Where(i => i.ConsumerId == consumerId)
                .ToListAsync();
            return invoices;
        }

        public async Task<IEnumerable<InvoiceDTO>> GetInvoicesDTOAsync(int? consumerId)
        {
            var invoices = await GetInvoicesDAOAsync(consumerId);
            if (invoices == null)
            {
                return new List<InvoiceDTO>();
            }
            return new List<InvoiceDTO>(invoices.Select(InvoiceHelper.BuildInvoiceDTO)
                                                                .Where(dto => dto != null)
                                                                .Cast<InvoiceDTO>()
                                                                .ToList());
        }

        public async Task<int?> SaveInvoiceAsync(InvoiceDAO invoice)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Detach existing tracked entities to avoid conflicts
                _context.ChangeTracker.Clear();

                // Attach and set state for Consumer
                if (invoice.ConsumerId == null && invoice.Consumer != null)
                {
                    invoice.ConsumerId = await _consumerService.GetConsumerIdAsync(invoice.Consumer.TaxNumber);
                    invoice.Consumer.Id = invoice.ConsumerId;
                }
                if (invoice.ConsumerId == null)
                    throw new Exception("Consumer not found");
                else
                    _context.Entry(invoice.Consumer).State = EntityState.Unchanged;



                // Attach and set state for Supplier
                if (invoice.Supplier != null)
                {
                    invoice.Supplier.ConsumerId = (int)invoice.ConsumerId;
                    var existingSupplier = await _context.Suppliers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => invoice.SupplierId != null && s.Id == invoice.SupplierId && s.ConsumerId == invoice.ConsumerId ||
                                                    invoice.SupplierId == null && s.TaxNumber == invoice.Supplier.TaxNumber && s.ConsumerId == invoice.ConsumerId);

                    if (existingSupplier == null)
                    {
                        _context.Suppliers.Add(invoice.Supplier);
                    }
                    else
                    {
                        invoice.SupplierId = existingSupplier.Id;
                        invoice.Supplier.Id = invoice.SupplierId;

                        if (string.IsNullOrWhiteSpace(existingSupplier.Name) || string.IsNullOrWhiteSpace(existingSupplier.BankAccount))
                            _context.Entry(invoice.Supplier).State = EntityState.Modified;
                        else
                            _context.Entry(invoice.Supplier).State = EntityState.Unchanged;
                    }
                }
                

                // Attach and set state for Invoice
                var existingInvoice = await _context.Invoices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == invoice.Id);
                if (existingInvoice == null)
                {
                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return invoice.Id;
                }
                else
                {
                    // Update specific fields
                    _context.Entry(existingInvoice).State = EntityState.Modified;
                    _context.Entry(existingInvoice).CurrentValues.SetValues(invoice);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return existingInvoice.Id;
                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return null;
            }
        }

        private Exception InvalidOperationException(string v)
        {
            throw new NotImplementedException();
        }

        public async Task<int?> SaveInvoiceAsync(InvoiceDTO invoiceDTO)
        {
            return await SaveInvoiceAsync(InvoiceHelper.BuildInvoiceDAO(invoiceDTO));
        }

        public async Task DeleteInvoiceAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice != null)
            {

                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
        }
    }
}
