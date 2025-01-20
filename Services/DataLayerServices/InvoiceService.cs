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

        Task<ActionResult<IEnumerable<InvoiceDAO>>> GetInvoicesDAOAsync();
        Task<ActionResult<IEnumerable<InvoiceDTO>>> GetInvoicesDTOAsync();

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

        public async Task<ActionResult<IEnumerable<InvoiceDAO>>> GetInvoicesDAOAsync()
        {
            var invoices = await _context.Invoices
                .Include(i => i.TaxCategories)
                .Include(i => i.Products)
                .Include(i => i.Supplier)
                .Include(i => i.Consumer)
                .ToListAsync();
            return invoices;
        }

        public async Task<ActionResult<IEnumerable<InvoiceDTO>>> GetInvoicesDTOAsync()
        {
            var invoices = await GetInvoicesDAOAsync();
            if (invoices.Value == null)
            {
                return new ActionResult<IEnumerable<InvoiceDTO>>(new List<InvoiceDTO>());
            }
            return new ActionResult<IEnumerable<InvoiceDTO>>(invoices.Value.Select(InvoiceHelper.BuildInvoiceDTO)
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
                }
                if (invoice.ConsumerId == null)
                {
                    throw new Exception("Consumer not found");
                }


                // Attach and set state for Supplier
                if (invoice.Supplier != null)
                {
                    var existingSupplier = await _context.Suppliers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => invoice.SupplierId != null && s.Id == invoice.SupplierId ||
                                                    invoice.SupplierId == null && s.TaxNumber == invoice.Supplier.TaxNumber);

                    if (existingSupplier == null)
                    {
                        _context.Suppliers.Add(invoice.Supplier);
                    }
                    else
                    {
                        existingSupplier.Name = invoice.Supplier.Name;
                        existingSupplier.BankAccount = invoice.Supplier.BankAccount;
                        _context.Entry(existingSupplier).State = EntityState.Modified;
                        invoice.SupplierId = existingSupplier.Id;
                    }
                }
                

                // Attach and set state for Products
                foreach (var product in invoice.Products)
                {
                    var existingProduct = await _context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == product.Id);
                    if (existingProduct == null)
                    {
                        _context.Products.Add(product);
                    }
                    else
                    {
                        _context.Entry(product).State = EntityState.Modified;
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
                    existingInvoice.InvoiceNumber = invoice.InvoiceNumber;
                    existingInvoice.InvoiceDate = invoice.InvoiceDate;
                    existingInvoice.TotalIVA = invoice.TotalIVA;
                    existingInvoice.ConsumerId = invoice.ConsumerId;
                    existingInvoice.SupplierId = invoice.SupplierId;
                    existingInvoice.FilePath = invoice.FilePath;
                    existingInvoice.UploadTime = invoice.UploadTime;
                    existingInvoice.Comments = invoice.Comments;
                    existingInvoice.Status = invoice.Status;

                    // Update related entities if necessary
                    existingInvoice.Products = new List<ProductDAO>(invoice.Products);
                    existingInvoice.TaxCategories = new List<TaxesDAO>(invoice.TaxCategories);

                    _context.Entry(existingInvoice).State = EntityState.Modified;

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
