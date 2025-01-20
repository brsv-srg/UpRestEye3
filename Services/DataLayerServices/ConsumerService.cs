using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.DTO;

namespace UpRestEye3.Services.DataLayer
{
    public interface IConsumerService
    {

        Task<ConsumerDAO?> GetConsumerDAOByIdAsync(int id);
        Task<ConsumerDTO?> GetConsumerDTOByIdAsync(int id);

        Task<int?> GetConsumerIdAsync(string taxNumber);

        Task<ActionResult<IEnumerable<ConsumerDAO>>> GetConsumerDAOAsync();
        Task<ActionResult<IEnumerable<ConsumerDTO>>> GetConsumerDTOAsync();

        //Task<int?> SaveConsumerAsync(ConsumerDTO consumer);
        //Task<int?> SaveConsumerAsync(ConsumerDAO consumer);

    }

    public class ConsumerService : IConsumerService
    {
        private readonly ApplicationDbContext _context;

        public ConsumerService(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<ConsumerDAO?> GetConsumerDAOByIdAsync(int id)
        {
            return await _context.Consumers
                .Include(s => s.Invoices)
                .Include(s => s.Suppliers)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<ConsumerDTO?> GetConsumerDTOByIdAsync(int id)
        {
            var consumerDAO = await _context.Consumers
                .Include(s => s.Invoices)
                .FirstOrDefaultAsync(s => s.Id == id);
            return consumerDAO != null ? new ConsumerDTO
            {
                Name = consumerDAO.Name,
                TaxNumber = consumerDAO.TaxNumber
            } : null;
        }

        public async Task<int?> GetConsumerIdAsync(string taxNumber)
        {
            var consumerDAO = await _context.Consumers
                .FirstOrDefaultAsync(c => c.TaxNumber == taxNumber);
            return consumerDAO?.Id;
        }

        public async Task<ActionResult<IEnumerable<ConsumerDAO>>> GetConsumerDAOAsync()
        {

            return await _context.Consumers
                .Include(i => i.Invoices)
                .Include(i => i.Suppliers)
                .ToListAsync();
        }

        public async Task<ActionResult<IEnumerable<ConsumerDTO>>> GetConsumerDTOAsync()
        {

            var consumersDAO = await GetConsumerDAOAsync();
            return new ActionResult<IEnumerable<ConsumerDTO>>(
                    consumersDAO.Value.Select(s => new ConsumerDTO
                    {
                        Name = s.Name,
                        TaxNumber = s.TaxNumber
                    }));
        }

    }
}
