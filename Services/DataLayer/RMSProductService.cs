using Microsoft.EntityFrameworkCore;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Data;
using UpRestEye3.Components.Pages;
using UpRestEye3.Services.BusinessLogic;

namespace UpRestEye3.Services.DataLayer
{
    public interface IRMSProductService
    {
        Task<List<RMSProductDTO>> GetProductsByConsumerIdAsync(int consumerId);
        Task<RMSProductDTO> GetProductByIdAsync(int productId);
        Task<int?> SaveProductAsync(RMSProductDTO productDTO);
    }

    public class RMSProductService : IRMSProductService
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
                .Include(p => p.Consumer)   
                .Where(p => p.ConsumerId == consumerId)
                .ToListAsync();

            return products.Select(p => RMSProductHelper.BuildRMSProductDTO(p)).ToList();
        }
        public async Task<RMSProductDTO> GetProductByIdAsync(int productId)
        {
            var product = await _context.RMSProducts
                .AsNoTracking()
                .Include(p => p.Containers)
                .Include(p => p.Consumer)   
                .Where(p => p.Id == productId)
                .FirstOrDefaultAsync();

            return RMSProductHelper.BuildRMSProductDTO(product);
        }




        public async Task<int?> SaveProductAsync(RMSProductDTO productDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {

                var consumer = await _context.Consumers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => (productDto.ConsumerId != null && c.Id == productDto.ConsumerId) ||
                                                (productDto.ConsumerId == null && c.TaxNumber == productDto.ConsumerTaxId));

                if (consumer == null)
                {
                    throw new Exception("Consumer not found");
                }
                else
                {
                    productDto.ConsumerId = (int)consumer.Id;
                }

                var newProductDao = RMSProductHelper.BuildRMSProductDAO(productDto);
                _context.ChangeTracker.Clear();

                var existingProduct = await _context.RMSProducts
                    .AsNoTracking()
                    .Include(p => p.Containers)
                    .FirstOrDefaultAsync(p => p.ConsumerId == newProductDao.ConsumerId && 
                                            
                                            ((newProductDao.Id != null && p.Id == newProductDao.Id) ||

                                            (newProductDao.Id == null && 
                                                newProductDao.RMSProductExtGuid != null && 
                                                newProductDao.RMSProductExtGuid != Guid.Empty && 
                                                p.RMSProductExtGuid == newProductDao.RMSProductExtGuid) ||

                                            (newProductDao.Id == null &&
                                                (newProductDao.RMSProductExtGuid == null || newProductDao.RMSProductExtGuid != Guid.Empty) &&
                                                !string.IsNullOrEmpty(newProductDao.Num) &&
                                                p.Num == newProductDao.Num) ||

                                            (newProductDao.Id == null &&
                                                (newProductDao.RMSProductExtGuid == null || newProductDao.RMSProductExtGuid != Guid.Empty) &&
                                                string.IsNullOrEmpty(newProductDao.Num) &&
                                                p.Name == newProductDao.Name)));

                if (existingProduct == null)
                {
                    
                    await _context.RMSProducts.AddAsync(newProductDao);
                    _context.Entry(newProductDao.Consumer).State = EntityState.Unchanged;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return newProductDao.Id;
                }
                else
                {


                    // Update Containers
                    var existingContainers = existingProduct.Containers.ToList();
                    var newContainers = newProductDao.Containers;

                    // Add or update containers
                    foreach (var newContainer in newContainers)
                    {
                        var existingContainer = existingContainers
                            .FirstOrDefault(c => (newContainer.Id != null && c.Id == newContainer.Id) ||
                                                    
                                                    (newContainer.Id == null && 
                                                        newContainer.RMSContainerExtGuid != null && newContainer.RMSContainerExtGuid != Guid.Empty &&
                                                        c.RMSContainerExtGuid != null && c.RMSContainerExtGuid != Guid.Empty &&
                                                        c.RMSContainerExtGuid == newContainer.RMSContainerExtGuid) ||

                                                    (newContainer.Id == null &&
                                                        (newContainer.RMSContainerExtGuid == null || newContainer.RMSContainerExtGuid != Guid.Empty ||
                                                        c.RMSContainerExtGuid == null || c.RMSContainerExtGuid == Guid.Empty) &&
                                                        !string.IsNullOrEmpty(newContainer.Name) && !string.IsNullOrEmpty(c.Name) &&
                                                        c.Name == newContainer.Name));

                        if (existingContainer == null)
                        {
                            //existingProduct.Containers.Add(newContainer);
                            _context.Entry(newContainer).State = EntityState.Added;
                        }
                        else
                        {
                            newContainer.Id = existingContainer.Id;
                            _context.Entry(newContainer).State = EntityState.Modified;
                        }
                    }

                    // Remove containers that are not in the new list
                    foreach (var existingContainer in existingContainers)
                    {
                        if (!newContainers.Any(c => (c.Id != null && c.Id == existingContainer.Id) ||
                                                    
                                                    (c.Id == null &&
                                                        c.RMSContainerExtGuid != null && c.RMSContainerExtGuid != Guid.Empty &&
                                                        existingContainer.RMSContainerExtGuid != null && existingContainer.RMSContainerExtGuid != Guid.Empty &&
                                                        c.RMSContainerExtGuid == existingContainer.RMSContainerExtGuid) ||
                                                    
                                                    (c.Id == null &&
                                                        (c.RMSContainerExtGuid == null || c.RMSContainerExtGuid != Guid.Empty ||
                                                        existingContainer.RMSContainerExtGuid == null || existingContainer.RMSContainerExtGuid == Guid.Empty) &&
                                                        !string.IsNullOrEmpty(c.Name) && !string.IsNullOrEmpty(existingContainer.Name) &&
                                                        c.Name == existingContainer.Name)))
                        {
                            _context.Remove(existingContainer);
                            _context.Entry(existingContainer).State = EntityState.Deleted;
                        }
                    }

                    newProductDao.Id = existingProduct.Id;
                    _context.Entry(newProductDao).State = EntityState.Modified;


                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return newProductDao.Id;
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
