using UpRestEye3.Data;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.DAO;
using Microsoft.EntityFrameworkCore;
using UpRestEye3.Services.BusinessLogic;
using System.Security.Principal;

namespace UpRestEye3.Services.DataLayer
{
    public interface IRMSAccountsService
    {
        Task<List<RMSAccountDTO>> GetAccountsByConsumerIdAsync(int consumerId);
        Task<RMSAccountDTO> GetAccountByIdAsync(int accountId);
        Task<int?> SaveAccountAsync(RMSAccountDTO accountDTO);
    }

    public class RMSAccountsService : IRMSAccountsService
    {
        private readonly ApplicationDbContext _context;

        public RMSAccountsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RMSAccountDTO>> GetAccountsByConsumerIdAsync(int consumerId)
        {
            var products = await _context.Accounts
                .AsNoTracking()
                .Include(p => p.Consumer)
                .Where(p => p.ConsumerId == consumerId)
                .ToListAsync();

            return products.Select(p => new RMSAccountDTO()
            {
                Id = p.Id,
                ConsumerId = p.ConsumerId,
                ConsumerTaxId = p.Consumer.TaxNumber,
                EntityExtGuid = p.EntityExtGuid,
                RootType = p.RootType,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status
            }).ToList();
        }
        
        public async Task<RMSAccountDTO> GetAccountByIdAsync(int accountId)
        {
            var _account = await _context.Accounts
                .AsNoTracking()
                .Include(p => p.Consumer)
                .Where(p => p.Id == accountId)
                .FirstOrDefaultAsync();

            return new RMSAccountDTO()
            {
                Id = _account.Id,
                ConsumerId = _account.ConsumerId,
                ConsumerTaxId = _account.Consumer.TaxNumber,
                EntityExtGuid = _account.EntityExtGuid,
                RootType = _account.RootType,
                Code = _account.Code,
                Name = _account.Name,
                Description = _account.Description,
                Status = _account.Status
            };
        }

        public async Task<int?> SaveAccountAsync(RMSAccountDTO accountDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {

                var consumer = await _context.Consumers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => (accountDTO.ConsumerId != null && c.Id == accountDTO.ConsumerId) ||
                                                (accountDTO.ConsumerId == null && c.TaxNumber == accountDTO.ConsumerTaxId));

                if (consumer == null)
                {
                    throw new Exception("Consumer not found");
                }
                else
                {
                    accountDTO.ConsumerId = (int)consumer.Id;
                }

                var newAccount = new RMSAccountDAO
                {
                    Id = accountDTO.Id,
                    ConsumerId = consumer.Id,
                    Consumer = consumer,
                    EntityExtGuid = accountDTO.EntityExtGuid,
                    RootType = accountDTO.RootType,
                    Code = accountDTO.Code,
                    Name = accountDTO.Name,
                    Description = accountDTO.Description,
                    Status = accountDTO.Status
                };
                _context.ChangeTracker.Clear();

                var existingAccount = await _context.Accounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ConsumerId == newAccount.ConsumerId &&

                                            ((newAccount.EntityExtGuid != null &&
                                                newAccount.EntityExtGuid != Guid.Empty &&
                                                p.EntityExtGuid == newAccount.EntityExtGuid) ||

                                            (newAccount.EntityExtGuid == null ||
                                                newAccount.EntityExtGuid == Guid.Empty) &&
                                                p.Name == newAccount.Name));

                if (existingAccount == null)
                {
                    await _context.Accounts.AddAsync(newAccount);
                    _context.Entry(newAccount.Consumer).State = EntityState.Unchanged;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return newAccount.Id;
                }
                else
                {
                    newAccount.Id = existingAccount.Id;
                    if (string.IsNullOrEmpty(newAccount.Description) && !string.IsNullOrEmpty(existingAccount.Description))
                        newAccount.Description = existingAccount.Description;

                    _context.Entry(newAccount).State = EntityState.Modified;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return newAccount.Id;
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

