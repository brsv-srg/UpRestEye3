using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.DAO;

namespace UpRestEye3.Services.DataLayer
{
    public interface IConnectionParameterService
    {
        Task<ConnectionParameterDTO?> GetConnectionParameterDTOByCustomerIdAsync(int customerId);
        Task<ConnectionParameterDTO?> SaveConnectionParameterAsync(ConnectionParameterDTO connectionParameter);

    }


    public class ConnectionParameterService : IConnectionParameterService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConsumerService _consumerService;

        public ConnectionParameterService(ApplicationDbContext context, IConsumerService consumerService)
        {
            _context = context;
            _consumerService = consumerService;
        }

        public async Task<ConnectionParameterDTO?> GetConnectionParameterDTOByCustomerIdAsync(int consumerId)
        {
            var connectionParameterDAO = await _context.ConnectionParameters
                .Include(p => p.Consumer)
                .Include(cp => cp.DeliveryService)
                .FirstOrDefaultAsync(cp => cp.ConsumerId == consumerId);
            return connectionParameterDAO != null ? new ConnectionParameterDTO
            {
                ApiUrl = connectionParameterDAO.ApiUrl,
                ApiLogin = connectionParameterDAO.ApiLogin,
                ApiPassword = connectionParameterDAO.ApiPassword,
                ConsumerTaxNumber = connectionParameterDAO.Consumer.TaxNumber,
                ConsumerId = (int)connectionParameterDAO.ConsumerId,
                DeliveryServiceId = connectionParameterDAO.DeliveryServiceId,
                DeliveryServiceName = connectionParameterDAO.DeliveryService?.Name
            } : null;
        }

        public async Task<ConnectionParameterDTO?> SaveConnectionParameterAsync(ConnectionParameterDTO connectionParameter)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Detach existing tracked entities to avoid conflicts
                _context.ChangeTracker.Clear();

                // Attach and set state for Consumer
                var consumerTaxNumber = connectionParameter.ConsumerTaxNumber;
                var consumerId = await _consumerService.GetConsumerIdAsync(consumerTaxNumber);
                if (consumerId == null)
                {
                    throw new Exception("Consumer not found");
                }
                // Attach and set state for DeliveryService
                var deliveryService = await _context.RMSProducts
                    .AsNoTracking()
                    .Where(p => p.ConsumerId == consumerId)
                    .FirstOrDefaultAsync(cp => cp.ConsumerId == consumerId &&
                                                    (connectionParameter.DeliveryServiceId == null && cp.Name == connectionParameter.DeliveryServiceName ||
                                                    connectionParameter.DeliveryServiceId != null && cp.Id == connectionParameter.DeliveryServiceId));

                var parameters = await _context.ConnectionParameters
                    .AsNoTracking()
                    .Include(cp => cp.Consumer)
                    .Include(cp => cp.DeliveryService)
                    .FirstOrDefaultAsync(cp => cp.ConsumerId == consumerId);
                if (parameters == null)
                {

                    parameters = new ConnectionParameterDAO
                    {
                        ConsumerId = consumerId,
                        ApiUrl = connectionParameter.ApiUrl,
                        ApiLogin = connectionParameter.ApiLogin,
                        ApiPassword = connectionParameter.ApiPassword,
                        DeliveryServiceId = deliveryService?.Id
                    };
                    _context.ConnectionParameters.Add(parameters);
                }
                else
                {
                    parameters.ApiUrl = connectionParameter.ApiUrl;
                    parameters.ApiLogin = connectionParameter.ApiLogin;
                    parameters.ApiPassword = connectionParameter.ApiPassword;
                    parameters.DeliveryServiceId = connectionParameter.DeliveryServiceId;
                    _context.Entry(parameters).State = EntityState.Modified;
                }



                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ConnectionParameterDTO
                {
                    ConsumerId = (int)consumerId,
                    ApiUrl = parameters.ApiUrl,
                    ApiLogin = parameters.ApiLogin,
                    ApiPassword = parameters.ApiPassword,
                    ConsumerTaxNumber = consumerTaxNumber,
                    DeliveryServiceId = parameters.DeliveryService?.Id,
                    DeliveryServiceName = parameters.DeliveryService?.Name

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
