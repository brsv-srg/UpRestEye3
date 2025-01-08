using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models;

namespace UpRestEye3.Services
{
    public interface ISupplierService
    {
        Task<int?> GetOrCreateSupplierIdAsync(SupplierInfo supplier);
    }

    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _context;

        public SupplierService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int?> GetOrCreateSupplierIdAsync(SupplierInfo supplier)
        {
            if (supplier == null)
                return null;

            var existingSupplier = await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TaxNumber == supplier.TaxNumber);
            if (existingSupplier != null)
            {
                _context.Entry(existingSupplier).State = EntityState.Detached;
                
                // если поставщик с таким TaxNumber уже есть, но у него пустое наименование или банковский счет,
                // то перезаписываем старый в БД новыми значениями
                if ((string.IsNullOrWhiteSpace(existingSupplier.Name) && !string.IsNullOrWhiteSpace(supplier.Name) ||
                    string.IsNullOrWhiteSpace(existingSupplier.BankAccount) && !string.IsNullOrWhiteSpace(supplier.BankAccount)))
                {
                    supplier.Id = existingSupplier.Id;
                    _context.Entry(supplier).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    _context.Entry(supplier).State = EntityState.Unchanged;

                }
                else
                // если новый поставщик с другим id(но тем же TaxNumber),
                // или то пустым id (что скорее), то присваиваем все атрибуты от уже существующего в БД
                if (existingSupplier.Id != supplier.Id)
                {
                    _context.Entry(supplier).CurrentValues.SetValues(existingSupplier);
                    _context.Entry(supplier).State = EntityState.Unchanged;
                }
                return supplier.Id;
            }
            else
            {
                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();
                return supplier.Id;
            }
        }
    }
}
