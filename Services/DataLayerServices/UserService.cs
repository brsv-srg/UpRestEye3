using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.DTO;


namespace UpRestEye3.Services.DataLayer
{
    public interface IUserService
    {
        Task<UserDTO?> AuthenticateAsync(string login, string password);

        Task<IEnumerable<UserDTO>> GetUsersByCustomerIdAsync(int customerId);

        Task<UserDTO?> GetUserDTOByIdAsync(int userId);
        Task<UserDAO?> GetUserDAOByIdAsync(int userId);

        Task<int?> SaveUserAsync(UserDTO user);
        Task<int?> SaveUserAsync(UserDAO user);

        Task DeleteUserAsync(int userId);
    }

    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConsumerService _consumerService;

        public UserService(ApplicationDbContext context, IConsumerService consumerService)
        {
            _context = context;
            _consumerService = consumerService;
        }

        public async Task<UserDTO?> AuthenticateAsync(string login, string password)
        {
            var userDAO = await _context.Users
                .Include(u => u.Consumer)
                .FirstOrDefaultAsync(u => u.Login == login && u.Password == password);
            return userDAO != null ? new UserDTO
            {
                Login = userDAO.Login,
                Password = userDAO.Password,
                ConsumerId = userDAO.ConsumerId,
                ConsumerTaxNumber = userDAO.Consumer.TaxNumber
            } : null;
        }

        public async Task<IEnumerable<UserDTO>> GetUsersByCustomerIdAsync(int customerId)
        {
            return await _context.Users
                .Where(u => u.ConsumerId == customerId)
                .Select(u => new UserDTO
                {
                    Login = u.Login,
                    Password = u.Password,
                    ConsumerId = u.ConsumerId,
                    ConsumerTaxNumber = u.Consumer.TaxNumber
                })
                .ToListAsync();
        }

        public async Task<UserDTO?> GetUserDTOByIdAsync(int userId)
        {
            var userDAO = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            return userDAO != null ? new UserDTO
            {
                Login = userDAO.Login,
                Password = userDAO.Password,
                ConsumerId = userDAO.ConsumerId,
                ConsumerTaxNumber = userDAO.Consumer.TaxNumber
            } : null;
        }

        public async Task<UserDAO?> GetUserDAOByIdAsync(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<int?> SaveUserAsync(UserDTO user)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Detach existing tracked entities to avoid conflicts
                _context.ChangeTracker.Clear();

                // Attach and set state for Consumer
                
                var consumerId = await _consumerService.GetConsumerIdAsync(user.ConsumerTaxNumber);
                if (consumerId == null)
                {
                    throw new Exception("Consumer not found");
                }


                var existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Login == user.Login);

                if (existingUser == null)
                {
                    var newUser = new UserDAO
                    {
                        Login = user.Login,
                        Password = user.Password,
                        ConsumerId = (int)consumerId
                    };
                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return newUser.Id;
                }
                else
                {
                    existingUser.Password = user.Password;
                    _context.Entry(existingUser).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return existingUser.Id;
                }

            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return null;
            }
        }

        public async Task<int?> SaveUserAsync(UserDAO user)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Detach existing tracked entities to avoid conflicts
                _context.ChangeTracker.Clear();

                // Attach and set state for Consumer

                var consumerId = await _consumerService.GetConsumerIdAsync(user.Consumer.TaxNumber);
                if (consumerId == null)
                {
                    throw new Exception("Consumer not found");
                }


                var existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Login == user.Login);

                if (existingUser == null)
                {
                    var newUser = new UserDAO
                    {
                        Login = user.Login,
                        Password = user.Password,
                        ConsumerId = (int)consumerId
                    };
                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return newUser.Id;
                }
                else
                {
                    existingUser.Password = user.Password;
                    _context.Entry(existingUser).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return existingUser.Id;
                }

               
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return null;
            }

        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}
