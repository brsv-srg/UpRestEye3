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

        Task DeleteInvoicesAsync(List<int> invoiceIds);

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
                        .ThenInclude(p => p.Consumer) // Include Consumer through RMSProducts

                .Include(i => i.Products)
                    .ThenInclude(p => p.RMSProduct) // Include RMSContainers through Products
                        .ThenInclude(p => p.Containers) // Include Containers through RMSProducts

                .Include(i => i.Products)
                    .ThenInclude(p => p.RMSContainer) // Include RMSContainers through Products

                .Include(i => i.Products)
                    .ThenInclude(p => p.RMSStorage) // Include RMSStorage through Products

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
                    .ThenInclude(p => p.RMSProduct) // Include RMSProducts through Products
                        .ThenInclude(p => p.Consumer) // Include Consumer through RMSProducts

                .Include(i => i.Products)
                    .ThenInclude(p => p.RMSProduct) // Include RMSProducts through Products
                        .ThenInclude(p => p.Containers) // Include Containers through RMSProducts

                .Include(i => i.Products)
                    .ThenInclude(p => p.RMSContainer) // Include RMSContainers through Products

                .Include(i => i.Products)
                    .ThenInclude(p => p.RMSStorage) // Include RMSStorage through Products

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

                        if (string.IsNullOrWhiteSpace(existingSupplier.Name) && 
                                !string.IsNullOrWhiteSpace(invoice.Supplier.Name)
                            || string.IsNullOrWhiteSpace(existingSupplier.BankAccount) && 
                                !string.IsNullOrWhiteSpace(invoice.Supplier.Name) && 
                                !string.IsNullOrWhiteSpace(invoice.Supplier.BankAccount))
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


                // Check of the doubles
                var doubleInvoice = await _context.Invoices
               .AsNoTracking()
               .FirstOrDefaultAsync(i => i.ConsumerId == invoice.ConsumerId &&
                                            i.SupplierId == invoice.SupplierId &&
                                            i.InvoiceNumber == invoice.InvoiceNumber &&
                                            (invoice.Id == null || invoice.Id != null && i.Id != invoice.Id));
                if (doubleInvoice != null)
                {
                    throw new InvalidOperationException("Invoice with the same number already exists.");
                }


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
                            .FirstOrDefault(c => c.TaxCategory == newCategory.TaxCategory);

                        if (existingCategory == null)
                        {
                            _context.Entry(newCategory).State = EntityState.Added;
                        }
                        else
                        {
                            newCategory.Id = existingCategory.Id;
                            newCategory.InvoiceId = existingCategory.InvoiceId;
                            _context.Entry(newCategory).State = EntityState.Modified;
                        }
                    }

                    // Remove TaxCategories that are not in the new list
                    foreach (var existingCategory in existingTaxCategories)
                    {
                        if (!newTaxCategories.Any(c => c.TaxCategory == existingCategory.TaxCategory))
                        {
                            //_context.Remove(existingCategory);
                            newTaxCategories.Add(existingCategory);
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
                            .FirstOrDefault(c =>
                                newProduct.Id != null && 
                                    c.Id == newProduct.Id ||
                                    
                                newProduct.Id == null && 
                                    c.ProductName == newProduct.ProductName && 
                                    c.ProductCode == newProduct.ProductCode && 
                                    c.ProductTotalValue == newProduct.ProductTotalValue);


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
                            //_context.Remove(existingProduct);
                            newInvoiceProducts.Add(existingProduct);
                            _context.Entry(existingProduct).State = EntityState.Deleted;

                        }
                    }

                    // Update invoice
                    _context.Entry(existingInvoice).State = EntityState.Detached;
                    _context.Entry(invoice).State = EntityState.Modified;
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return existingInvoice.Id;
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Error saving invoice: {ex.Message}");
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

        public async Task DeleteInvoicesAsync(List<int> invoiceIds)
        {
            var invoicesToDelete = _context.Invoices.Where(i => invoiceIds.Contains((int)i.Id)).ToList();

            if (invoicesToDelete.Any())
            {
                _context.Invoices.RemoveRange(invoicesToDelete);
                await _context.SaveChangesAsync();
            }
        }
    }
}
