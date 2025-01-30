using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.DTO;

namespace UpRestEye3.Services.DataLayer
{
    public interface ISupplierService
    {

        Task<SupplierDAO?> GetSupplierDAOByIdAsync(int id);
        Task<SupplierDTO?> GetSupplierDTOByIdAsync(int id);

        Task<ActionResult<IEnumerable<SupplierDAO>>> GetSupplierDAOAsync(ConsumerDAO? consumer);
        Task<ActionResult<IEnumerable<SupplierDTO>>> GetSupplierDTOAsync(ConsumerDAO? consumer);

        //Task<int?> SaveSupplierAsync(SupplierDTO invoice);
        //Task<int?> SaveSupplierAsync(SupplierDAO invoice);

    }

    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _context;

        public SupplierService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SupplierDAO?> GetSupplierDAOByIdAsync(int id)
        {
            return await _context.Suppliers
                .AsNoTracking()
                .Include(s => s.Invoices)
                .Include(s => s.Consumer)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<SupplierDTO?> GetSupplierDTOByIdAsync(int id)
        {
            var supplierDAO = await _context.Suppliers
                .AsNoTracking()
                .Include(s => s.Invoices)
                .FirstOrDefaultAsync(s => s.Id == id);
            return supplierDAO != null ? new SupplierDTO
            {
                Name = supplierDAO.Name,
                TaxNumber = supplierDAO.TaxNumber,
                BankAccount = supplierDAO.BankAccount
            } : null;
        }

        public async Task<ActionResult<IEnumerable<SupplierDAO>>> GetSupplierDAOAsync(ConsumerDAO? consumer)
        {

            return await _context.Suppliers
                .AsNoTracking()
                .Include(i => i.Invoices)
                .Include(i => i.Consumer)
                .Where(s => consumer != null &&
                            (consumer.Id != null && s.ConsumerId == consumer.Id ||
                                 consumer.Id == null && s.TaxNumber == consumer.TaxNumber))

                .ToListAsync();
        }

        public async Task<ActionResult<IEnumerable<SupplierDTO>>> GetSupplierDTOAsync(ConsumerDAO? consumer)
        {

            var suppliersDAO = await GetSupplierDAOAsync(consumer);
            return new ActionResult<IEnumerable<SupplierDTO>>(
                    suppliersDAO.Value.Select(s => new SupplierDTO
                    {
                        Name = s.Name,
                        TaxNumber = s.TaxNumber,
                        BankAccount = s.BankAccount
                    }));
        }
    }
}
