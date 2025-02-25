using UpRestEye3.Data;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.DAO;
using Microsoft.EntityFrameworkCore;
using UpRestEye3.Services.BusinessLogic;

namespace UpRestEye3.Services.DataLayer
{
    public interface IRMSMeasureUnitService
    {
        Task<List<RMSMeasureUnitDTO>> GetUnitsByConsumerIdAsync(int consumerId);
        Task<int?> SaveMeasureUnitAsync(RMSMeasureUnitDTO measureUnitDTO);
    }

    public class RMSMeasureUnitService : IRMSMeasureUnitService
    {
        private readonly ApplicationDbContext _context;

        public RMSMeasureUnitService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RMSMeasureUnitDTO>> GetUnitsByConsumerIdAsync(int consumerId)
        {
            var products = await _context.MeasureUnits
                .AsNoTracking()
                .Include(p => p.Consumer)
                .Where(p => p.ConsumerId == consumerId)
                .ToListAsync();

            return products.Select(p => new RMSMeasureUnitDTO()
            {
                Id = p.Id,
                ConsumerId = p.ConsumerId,
                ConsumerTaxId = p.Consumer.TaxNumber,
                MeasureUnitExtGuid = p.MeasureUnitExtGuid,
                RootType = p.RootType,
                Deleted = p.Deleted,
                Code = p.Code,
                Name = p.Name
            }).ToList();
        }
        public async Task<RMSMeasureUnitDTO> GetUnitByIdAsync(int unitId)
        {
            var measureUnit = await _context.MeasureUnits
                .AsNoTracking()
                .Include(p => p.Consumer)
                .Where(p => p.Id == unitId)
                .FirstOrDefaultAsync();

            return new RMSMeasureUnitDTO()
            {
                Id = measureUnit.Id,
                ConsumerId = measureUnit.ConsumerId,
                ConsumerTaxId = measureUnit.Consumer.TaxNumber,
                MeasureUnitExtGuid = measureUnit.MeasureUnitExtGuid,
                RootType = measureUnit.RootType,
                Deleted = measureUnit.Deleted,
                Code = measureUnit.Code,
                Name = measureUnit.Name
            };
        }


        public async Task<int?> SaveMeasureUnitAsync(RMSMeasureUnitDTO measureUnitDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {

                var consumer = await _context.Consumers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => (measureUnitDTO.ConsumerId != null && c.Id == measureUnitDTO.ConsumerId) ||
                                                (measureUnitDTO.ConsumerId == null && c.TaxNumber == measureUnitDTO.ConsumerTaxId));

                if (consumer == null)
                {
                    throw new Exception("Consumer not found");
                }
                else
                {
                    measureUnitDTO.ConsumerId = (int)consumer.Id;
                }

                var newUnit = new RMSMeasureUnitDAO
                {
                    Id = measureUnitDTO.Id,
                    ConsumerId = consumer.Id,
                    Consumer = consumer,
                    MeasureUnitExtGuid = measureUnitDTO.MeasureUnitExtGuid,
                    RootType = measureUnitDTO.RootType,
                    Deleted = measureUnitDTO.Deleted,
                    Code = measureUnitDTO.Code,
                    Name = measureUnitDTO.Name,
                };
                _context.ChangeTracker.Clear();

                var existingUnit = await _context.MeasureUnits
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ConsumerId == newUnit.ConsumerId &&

                                            ((newUnit.MeasureUnitExtGuid != null &&
                                                newUnit.MeasureUnitExtGuid != Guid.Empty &&
                                                p.MeasureUnitExtGuid == newUnit.MeasureUnitExtGuid) ||

                                            (newUnit.MeasureUnitExtGuid == null ||
                                                newUnit.MeasureUnitExtGuid == Guid.Empty) &&
                                                p.Name == newUnit.Name));

                if (existingUnit == null)
                {
                    await _context.MeasureUnits.AddAsync(newUnit);
                    _context.Entry(newUnit.Consumer).State = EntityState.Unchanged;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return newUnit.Id;
                }
                else
                {
                    newUnit.Id = existingUnit.Id;
                    _context.Entry(newUnit).State = EntityState.Modified;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return newUnit.Id;
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

