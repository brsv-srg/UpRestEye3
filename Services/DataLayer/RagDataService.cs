using Microsoft.EntityFrameworkCore;
using OpenAI.Assistants;
using OpenAI.VectorStores;
using UpRestEye3.Data;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.DTO;

namespace UpRestEye3.Services.DataLayer
{
    public interface IRagDataService
    {
        Task<RagManagementDTO?> GetRagDTOByCustomerIdAsync(int customerId);
        Task<RagManagementDTO?> GetRagDTOByCustomerIdAsync(string consumerTaxNumber);
        Task<RagManagementDTO?> SaveRagAsync(RagManagementDTO assistant);

    }


    public class RagDataService : IRagDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConsumerService _consumerService;

        public RagDataService(ApplicationDbContext context, IConsumerService consumerService)
        {
            _context = context;
            _consumerService = consumerService;
        }

        public async Task<RagManagementDTO?> GetRagDTOByCustomerIdAsync(int consumerId)
        {
            var raDao = await _context.RagData
                .Include(ra => ra.Consumer)
                .FirstOrDefaultAsync(ra => ra.ConsumerId == consumerId);

            return raDao != null ? new RagManagementDTO
            {
                Id = raDao.Id,
                MappingVectorStoreId = raDao.MappingVectorStoreId,

                ProductsVectorStoreId = raDao.ProductsVectorStoreId,
                ConsumerTaxNumber = raDao.Consumer.TaxNumber,
                UpdatedAt = raDao.UpdatedAt
            } : null;
        }

        public async Task<RagManagementDTO?> GetRagDTOByCustomerIdAsync(string consumerTaxNumber)
        {
            var consumerId = await _consumerService.GetConsumerIdAsync(consumerTaxNumber);
            if (consumerId == null)
            {
                return null;
            }
            return await GetRagDTOByCustomerIdAsync((int)consumerId);
        }

        public async Task<RagManagementDTO?> SaveRagAsync(RagManagementDTO assistant)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Detach existing tracked entities to avoid conflicts
                _context.ChangeTracker.Clear();

                // Attach and set state for Consumer
                var consumerTaxNumber = assistant.ConsumerTaxNumber;
                var consumerId = await _consumerService.GetConsumerIdAsync(consumerTaxNumber);
                if (consumerId == null)
                {
                    throw new Exception("Consumer not found");
                }

                var ragData = await _context.RagData
                    .AsNoTracking()
                    .Include(ra => ra.Consumer)
                    .FirstOrDefaultAsync(ra => ra.ConsumerId == consumerId);
                if (ragData == null)
                {
                    ragData = new RagDataDAO
                    {
                        ConsumerId = consumerId,
                        MappingVectorStoreId = assistant.MappingVectorStoreId,
                        ProductsVectorStoreId = assistant.ProductsVectorStoreId,
                    };
                    _context.RagData.Add(ragData);
                }
                else
                {
                    ragData.MappingVectorStoreId = assistant.MappingVectorStoreId;
                    ragData.ProductsVectorStoreId = assistant.ProductsVectorStoreId;
                    ragData.UpdatedAt = DateTime.UtcNow;
                    _context.Entry(ragData).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new RagManagementDTO
                {
                    ConsumerTaxNumber = consumerTaxNumber,
                    MappingVectorStoreId = ragData.MappingVectorStoreId,
                    ProductsVectorStoreId = ragData.ProductsVectorStoreId,
                    UpdatedAt = ragData.UpdatedAt
                };
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return null;
            }
        }
    }
}
