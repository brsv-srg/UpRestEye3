using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.BusinessLogic;

namespace UpRestEye3.Services.DataLayer
{
    public interface ISupplierService
    {

        Task<SupplierDTO?> GetSupplierDTOByIdAsync(int id);

        Task<List<SupplierDTO>> GetSuppliersDTOAsync(int? consumerId);

        Task<int?> SaveSupplierAsync(SupplierDTO invoice);

    }

    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _context;

        public SupplierService(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<SupplierDTO?> GetSupplierDTOByIdAsync(int id)
        {
            var supplierDAO = await _context.Suppliers
                .AsNoTracking()
                .Include(s => s.Invoices)
                    .Include(i => i.Consumer)
                .FirstOrDefaultAsync(s => s.Id == id);
            return supplierDAO != null ? new SupplierDTO
            {
                Id = supplierDAO.Id,
                Name = supplierDAO.Name,
                TaxNumber = supplierDAO.TaxNumber,
                BankAccount = supplierDAO.BankAccount,
                RMSSupplierId = supplierDAO.RMSSupplierId,
                Status = supplierDAO.Status,
                ConsumerId = supplierDAO.ConsumerId,
                ConsumerTaxId = supplierDAO.Consumer.TaxNumber,
                HasInvoices = _context.Invoices.Any(i => i.SupplierId == id)

            } : null;
        }


        public async Task<List<SupplierDTO>> GetSuppliersDTOAsync(int? consumerId)
        {

            var suppliersDAO = await _context.Suppliers
                .AsNoTracking()
                .Include(i => i.Invoices)
                    .Include(i => i.Consumer)
                .Where(s => consumerId == null || consumerId != null && s.ConsumerId == consumerId)
                .ToListAsync();

            return new List<SupplierDTO>(
                    suppliersDAO.Select(s => new SupplierDTO
                    {
                        Id = s.Id,
                        Name = s.Name,
                        TaxNumber = s.TaxNumber,
                        BankAccount = s.BankAccount,
                        RMSSupplierId = s.RMSSupplierId,
                        Status = s.Status,
                        ConsumerId = s.ConsumerId,
                        ConsumerTaxId = s.Consumer.TaxNumber,
                        HasInvoices = _context.Invoices.Any(i => i.SupplierId == s.Id)

                    }));
        }

        public async Task<int?> SaveSupplierAsync1(SupplierDTO supplierDTO)
        {

            var supplierDAO = new SupplierDAO
            {
                Name = supplierDTO.Name,
                TaxNumber = supplierDTO.TaxNumber,
                BankAccount = supplierDTO.BankAccount,
                RMSSupplierId = supplierDTO.RMSSupplierId,
                Status = supplierDTO.Status
            };
            _context.Suppliers.Add(supplierDAO);
            await _context.SaveChangesAsync();
            return supplierDAO.Id;
        }
        public async Task<int?> SaveSupplierAsync(SupplierDTO supplierDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {

                var consumer = await _context.Consumers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => (supplierDTO.ConsumerId != null && c.Id == supplierDTO.ConsumerId) ||
                                                (supplierDTO.ConsumerId == null && c.TaxNumber == supplierDTO.ConsumerTaxId));

                if (consumer == null)
                {
                    throw new Exception("Consumer not found");
                }
                else
                {
                    supplierDTO.ConsumerId = (int)consumer.Id;
                }

                var newSupplierDAO = new SupplierDAO
                {
                    Name = supplierDTO.Name,
                    TaxNumber = supplierDTO.TaxNumber,
                    BankAccount = supplierDTO.BankAccount,
                    RMSSupplierId = supplierDTO.RMSSupplierId,
                    Status = supplierDTO.Status,
                    ConsumerId = (int)consumer.Id,
                    Consumer = consumer
                };
                _context.ChangeTracker.Clear();

                var existingSupplier = await _context.Suppliers 
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ConsumerId == newSupplierDAO.ConsumerId &&

                                            ((newSupplierDAO.Id != null && p.Id == newSupplierDAO.Id) ||

                                            (newSupplierDAO.Id == null &&
                                                newSupplierDAO.RMSSupplierId != null &&
                                                newSupplierDAO.RMSSupplierId != Guid.Empty &&
                                                p.RMSSupplierId == newSupplierDAO.RMSSupplierId) ||

                                            (newSupplierDAO.Id == null &&
                                                (newSupplierDAO.RMSSupplierId == null || newSupplierDAO.RMSSupplierId != Guid.Empty) &&
                                                !string.IsNullOrEmpty(newSupplierDAO.TaxNumber) &&
                                                p.TaxNumber == newSupplierDAO.TaxNumber)));

                if (existingSupplier == null)
                {

                    await _context.Suppliers.AddAsync(newSupplierDAO);
                    _context.Entry(newSupplierDAO.Consumer).State = EntityState.Unchanged;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return newSupplierDAO.Id;
                }
                else
                {

                    newSupplierDAO.Id = existingSupplier.Id;
                    if(existingSupplier.RMSSupplierId != null && existingSupplier.RMSSupplierId != Guid.Empty &&
                        (newSupplierDAO.RMSSupplierId == null || newSupplierDAO.RMSSupplierId == Guid.Empty))
                        newSupplierDAO.RMSSupplierId = existingSupplier.RMSSupplierId;

                    if (!string.IsNullOrEmpty(existingSupplier.BankAccount) && string.IsNullOrEmpty(newSupplierDAO.BankAccount))
                        newSupplierDAO.BankAccount = existingSupplier.BankAccount;

                    if (!string.IsNullOrEmpty(existingSupplier.Name) && string.IsNullOrEmpty(newSupplierDAO.Name))
                        newSupplierDAO.Name = existingSupplier.Name;

                    //newSupplierDAO.Status = SupplierStatus.Changed;
                    _context.Entry(newSupplierDAO).State = EntityState.Modified;


                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return newSupplierDAO.Id;
                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}
