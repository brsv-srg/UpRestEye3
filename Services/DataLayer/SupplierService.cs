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

        Task<List<SupplierDAO>> GetSuppliersDAOAsync(int? consumerId);
        Task<List<SupplierDTO>> GetSuppliersDTOAsync(int? consumerId);

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

        public async Task<List<SupplierDAO>> GetSuppliersDAOAsync(int? consumerId)
        {

            return await _context.Suppliers
                .AsNoTracking()
                .Include(i => i.Invoices)
                    .Include(i => i.Consumer)
                .Where(s => consumerId == null || consumerId != null && s.ConsumerId == consumerId)
                .ToListAsync();
        }

        public async Task<List<SupplierDTO>> GetSuppliersDTOAsync(int? consumerId)
        {

            var suppliersDAO = await GetSuppliersDAOAsync(consumerId);
            return new List<SupplierDTO>(
                    suppliersDAO.Select(s => new SupplierDTO
                    {
                        Name = s.Name,
                        TaxNumber = s.TaxNumber,
                        BankAccount = s.BankAccount
                    }));
        }
    }
}
