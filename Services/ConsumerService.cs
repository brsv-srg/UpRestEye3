using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models;

namespace UpRestEye3.Services
{
    public interface ICustomerService
    {
        Task<int?> GetOrCreateConsumerIdAsync(ConsumerInfo consumer);
    }
    // todo сделать создание объекта ConsumerInfo сразу с проверкой в БД, через билдер
    public class ConsumerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;

        public ConsumerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int?> GetOrCreateConsumerIdAsync(ConsumerInfo consumer)
        {
            if (consumer == null)
                return null;

            var existingConsumer = await _context.Consumers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.TaxNumber == consumer.TaxNumber);
            if (existingConsumer != null)
            {
                _context.Entry(existingConsumer).State = EntityState.Detached;
                
                // если поставщик с таким TaxNumber уже есть, но у него пустое наименование или банковский счет,
                // то перезаписываем старый в БД новыми значениями
                if (string.IsNullOrWhiteSpace(existingConsumer.Name) &&
                    !string.IsNullOrWhiteSpace(consumer.Name))
                {
                    consumer.Id = existingConsumer.Id;
                    _context.Entry(consumer).State = EntityState.Modified;
                    //await _context.SaveChangesAsync();
                    //_context.Entry(consumer).State = EntityState.Unchanged;
                }
                else
                // если новый потребитель с другим id (но тем же TaxNumber),
                // или то пустым id (что скорее), то присваиваем уже существующий в БД
                if (existingConsumer.Id != consumer.Id)
                {
                    _context.Entry(consumer).CurrentValues.SetValues(existingConsumer);
                    _context.Entry(consumer).State = EntityState.Unchanged;
                }
                return consumer.Id;
            }
            else
            {
                // если потребитель новый, то добавляем его в БД
                _context.Consumers.Add(consumer);
                //await _context.SaveChangesAsync();
                return consumer.Id;
            }
        }
    }
}
