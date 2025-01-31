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
                    .ThenInclude(p => p.RMSProduct) // Include RMSProducts through Products
                .Include(i => i.Products)
                    .ThenInclude(p => p.RMSContainer) // Include RMSContainers through Products
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
                    .Include(i => i.Products)
                    .Include(i => i.TaxCategories)  
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

                    // Update TaxCategories
                    var existingTaxCategories = existingInvoice.TaxCategories.ToList();
                    var newTaxCategories = invoice.TaxCategories;

                    // Add or update TaxCategories

                    foreach (var newCategory in newTaxCategories)
                    {
                        var existingCategory = existingTaxCategories
                            .FirstOrDefault(c => c.Category == newCategory.Category);

                        if (existingCategory == null)
                        {
                            _context.Entry(newCategory).State = EntityState.Added;
                        }
                        else
                        {
                            newCategory.Id = existingCategory.Id;
                            _context.Entry(newCategory).State = EntityState.Modified;
                        }
                    }

                    // Remove TaxCategories that are not in the new list
                    foreach (var existingCategory in existingTaxCategories)
                    {
                        if (!newTaxCategories.Any(c => c.Category == existingCategory.Category))
                        {
                            _context.Remove(existingCategory);
                            _context.Entry(existingCategory).State = EntityState.Deleted;

                        }
                    }


                    // Update Invoice Products
                    var existingInvoiceProducts = existingInvoice.Products.ToList();
                    var newInvoiceProducts = invoice.Products;

                    // Add or update Invoice Products
                    foreach (var newProduct in newInvoiceProducts)
                    {
                        var existingProduct = existingInvoiceProducts
                            .FirstOrDefault(c => c.ProductName == newProduct.ProductName && c.ProductCode == newProduct.ProductCode);

                        if (existingProduct == null)
                        {
                            _context.Entry(newProduct).State = EntityState.Added;
                        }
                        else
                        {
                            newProduct.Id = existingProduct.Id;
                            _context.Entry(newProduct).State = EntityState.Modified;
                        }
                    }

                    // Remove Invoice Products that are not in the new list
                    foreach (var existingProduct in existingInvoiceProducts)
                    {
                        if (!newInvoiceProducts.Any(c => c.ProductName == existingProduct.ProductName && c.ProductCode == existingProduct.ProductCode))
                        {
                            _context.Remove(existingProduct);
                            _context.Entry(existingProduct).State = EntityState.Deleted;

                        }
                    }

                    //



                    // Update invoice
                    invoice.Id = existingInvoice.Id;
                    _context.Entry(invoice).State = EntityState.Modified;

                    
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
