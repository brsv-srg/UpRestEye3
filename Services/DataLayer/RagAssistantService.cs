using Microsoft.EntityFrameworkCore;
using OpenAI.Assistants;
using OpenAI.VectorStores;
using UpRestEye3.Data;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.DTO;

namespace UpRestEye3.Services.DataLayer
{
    public interface IRagAssistantDataService
    {
        Task<RagAssistantDTO?> GetRagAssistantDTOByCustomerIdAsync(int customerId);
        Task<RagAssistantDTO?> SaveRagAssistantAsync(RagAssistantDTO assistant);

    }


    public class RagAssistantDataService : IRagAssistantDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConsumerService _consumerService;

        public RagAssistantDataService(ApplicationDbContext context, IConsumerService consumerService)
        {
            _context = context;
            _consumerService = consumerService;
        }

        public async Task<RagAssistantDTO?> GetRagAssistantDTOByCustomerIdAsync(int consumerId)
        {
            var raDao = await _context.RagAssistant
                .Include(ra => ra.Consumer)
                .FirstOrDefaultAsync(ra => ra.ConsumerId == consumerId);

            return raDao != null ? new RagAssistantDTO
            {
                AssistantId = raDao.AssistantId,
                VectorStoreId = raDao.VectorStoreId,
                FileId = raDao.FileId,
                ConsumerTaxNumber = raDao.Consumer.TaxNumber,
                UpdatedAt = raDao.UpdatedAt
            } : null;
        }

        public async Task<RagAssistantDTO?> SaveRagAssistantAsync(RagAssistantDTO assistant)
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

                var ragAssistant = await _context.RagAssistant
                    .AsNoTracking()
                    .Include(ra => ra.Consumer)
                    .FirstOrDefaultAsync(ra => ra.ConsumerId == consumerId);
                if (ragAssistant == null)
                {
                    ragAssistant = new RagAssistantDAO
                    {
                        ConsumerId = consumerId,
                        AssistantId = assistant.AssistantId,
                        VectorStoreId = assistant.VectorStoreId,
                        FileId = assistant.FileId
                    };
                    _context.RagAssistant.Add(ragAssistant);
                }
                else
                {
                    ragAssistant.AssistantId = assistant.AssistantId;
                    ragAssistant.VectorStoreId = assistant.VectorStoreId;
                    ragAssistant.FileId = assistant.FileId;
                    ragAssistant.UpdatedAt = DateTime.UtcNow;
                    _context.Entry(ragAssistant).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new RagAssistantDTO
                {
                    ConsumerTaxNumber = consumerTaxNumber,
                    AssistantId = ragAssistant.AssistantId,
                    VectorStoreId = ragAssistant.VectorStoreId,
                    FileId = ragAssistant.FileId,
                    UpdatedAt = ragAssistant.UpdatedAt
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
