using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.DAO;
using UpRestEye3.Services.DataLayer;


namespace UpRestEye3.Services.DataLayer
{
    public interface IConnectionParameterService
    {
        Task<ConnectionParameterDTO> GetConnectionParameterDTOByCustomerIdAsync(int customerId);
        Task<ConnectionParameterDAO> GetConnectionParameterDAOByCustomerIdAsync(int customerId);
        Task<int?> SaveConnectionParameterAsync(ConnectionParameterDTO connectionParameter);
        Task<int?> SaveConnectionParameterAsync(ConnectionParameterDAO connectionParameter);

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
                .FirstOrDefaultAsync(cp => cp.ConsumerId == consumerId);
            return connectionParameterDAO != null ? new ConnectionParameterDTO
            {
                ApiUrl = connectionParameterDAO.ApiUrl,
                ApiLogin = connectionParameterDAO.ApiLogin,
                ApiPassword = connectionParameterDAO.ApiPassword,
                ConsumerTaxNumber = connectionParameterDAO.Consumer.TaxNumber,
                ConsumerId = (int)connectionParameterDAO.ConsumerId
            } : null;
        }

        public async Task<ConnectionParameterDAO?> GetConnectionParameterDAOByCustomerIdAsync(int consumerId)
        {
            return await _context.ConnectionParameters
                .FirstOrDefaultAsync(cp => cp.ConsumerId == consumerId);
        }

        public async Task<int?> SaveConnectionParameterAsync(ConnectionParameterDTO connectionParameter)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Detach existing tracked entities to avoid conflicts
                _context.ChangeTracker.Clear();

                // Attach and set state for Consumer

                var consumerId = await _consumerService.GetConsumerIdAsync(connectionParameter.ConsumerTaxNumber);
                if (consumerId == null)
                {
                    throw new Exception("Consumer not found");
                }

                var existingParameter = await _context.ConnectionParameters
                    .FirstOrDefaultAsync(cp => cp.ConsumerId == consumerId);
                if (existingParameter == null) 
                {
                    var connectionParameterDAO = new ConnectionParameterDAO
                    {
                        ConsumerId = consumerId.Value,
                        ApiUrl = connectionParameter.ApiUrl,
                        ApiLogin = connectionParameter.ApiLogin,
                        ApiPassword = connectionParameter.ApiPassword
                    };
                    _context.ConnectionParameters.Add(connectionParameterDAO);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return connectionParameterDAO.ConsumerId;
                }
                else
                {
                    existingParameter.ApiUrl = connectionParameter.ApiUrl;
                    existingParameter.ApiLogin = connectionParameter.ApiLogin;
                    existingParameter.ApiPassword = connectionParameter.ApiPassword;
                    _context.Entry(existingParameter).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return existingParameter.ConsumerId;
                }
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return null;
            }
        }

        public async Task<int?> SaveConnectionParameterAsync(ConnectionParameterDAO connectionParameter)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Detach existing tracked entities to avoid conflicts
                _context.ChangeTracker.Clear();

                // Attach and set state for Consumer

                var consumerId = await _consumerService.GetConsumerIdAsync(connectionParameter.Consumer.TaxNumber);
                if (consumerId == null)
                {
                    throw new Exception("Consumer not found");
                }

                var existingParameter = await _context.ConnectionParameters
                    .FirstOrDefaultAsync(cp => cp.ConsumerId == consumerId);
                if (existingParameter == null)
                {
                    var connectionParameterDAO = new ConnectionParameterDAO
                    {
                        ConsumerId = consumerId.Value,
                        ApiUrl = connectionParameter.ApiUrl,
                        ApiLogin = connectionParameter.ApiLogin,
                        ApiPassword = connectionParameter.ApiPassword
                    };
                    _context.ConnectionParameters.Add(connectionParameterDAO);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return connectionParameterDAO.ConsumerId;
                }
                else
                {
                    existingParameter.ApiUrl = connectionParameter.ApiUrl;
                    existingParameter.ApiLogin = connectionParameter.ApiLogin;
                    existingParameter.ApiPassword = connectionParameter.ApiPassword;
                    _context.Entry(existingParameter).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return existingParameter.ConsumerId;
                }
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                return null;
            }
        }

    }
}
