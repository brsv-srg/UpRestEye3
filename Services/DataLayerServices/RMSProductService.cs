using Microsoft.EntityFrameworkCore;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Data;
using UpRestEye3.Components.Pages;

namespace UpRestEye3.Services.DataLayer
{
    public interface IRMSProductService
    {
        Task<List<RMSProductDTO>> GetProductsByConsumerIdAsync(int consumerId);
        Task<int?> SaveProductAsync(RMSProductDTO productDTO);
    }

    public class RMSProductService: IRMSProductService
    {
        private readonly ApplicationDbContext _context;

        public RMSProductService(ApplicationDbContext context) 
                                 
        {
            _context = context;
        }

        public async Task<List<RMSProductDTO>> GetProductsByConsumerIdAsync(int consumerId)
        {
            var products = await _context.RMSProducts
                .AsNoTracking()
                .Include(p => p.Containers)
                .Where(p => p.ConsumerId == consumerId)
                .ToListAsync();

            return products.Select(p => RMSProductMappingService.ToDTO(p)).ToList();
        }

        public async Task<int?> SaveProductAsync(RMSProductDTO productDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.ChangeTracker.Clear();

                var consumer = await _context.Consumers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == productDto.ConsumerId);

                if (consumer == null)
                {
                    throw new Exception("Consumer not found");
                }

                var existingProduct = await _context.RMSProducts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ConsumerId == productDto.ConsumerId && p.RMSProductId == productDto.RMSProductId);
                
                var newProduct = RMSProductMappingService.ToDAO(productDto);
                if (existingProduct == null)
                {
                    await _context.RMSProducts.AddAsync(newProduct);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return newProduct.Id;
                }
                else
                {
                    newProduct.Id = existingProduct.Id;
                    _context.Entry(newProduct).State = EntityState.Modified;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return existingProduct.Id;
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
