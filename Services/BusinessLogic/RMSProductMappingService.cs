using UpRestEye3.Models.DAO;
using UpRestEye3.Models.DTO;

namespace UpRestEye3.Services.BusinessLogic
{
    public class RMSProductMappingService
    {
        public static RMSProductDTO ToDTO(RMSProductDAO dao)
        {
            return new RMSProductDTO
            {
                Id = dao.Id,
                ConsumerId = dao.ConsumerId,
                RMSProductId = dao.RMSProductId,
                Deleted = dao.Deleted,
                Name = dao.Name,
                Description = dao.Description,
                Num = dao.Num,
                Parent = dao.Parent,
                TaxCategory = dao.TaxCategory,
                Category = dao.Category,
                AccountingCategory = dao.AccountingCategory,
                MainUnit = dao.MainUnit,
                //Type = dao.Type,
                UnitWeight = dao.UnitWeight,
                UnitCapacity = dao.UnitCapacity,
                NotInStoreMovement = dao.NotInStoreMovement,
                Containers = dao.Containers.Select(c => new RMSContainerDTO
                {
                    Id = c.Id,
                    RMSContainerId = c.RMSContainerId,
                    Num = c.Num,
                    Name = c.Name,
                    Count = c.Count,
                    MinContainerWeight = c.MinContainerWeight,
                    MaxContainerWeight = c.MaxContainerWeight,
                    ContainerWeight = c.ContainerWeight,
                    FullContainerWeight = c.FullContainerWeight
                }).ToList()
            };
        }

        public static RMSProductDAO ToDAO(RMSProductDTO dto)
        {
            return new RMSProductDAO
            {
                Id = dto.Id,
                ConsumerId = dto.ConsumerId,
                RMSProductId = dto.RMSProductId,
                Deleted = dto.Deleted,
                Name = dto.Name,
                Description = dto.Description,
                Num = dto.Num,
                Parent = dto.Parent,
                TaxCategory = dto.TaxCategory,
                Category = dto.Category,
                AccountingCategory = dto.AccountingCategory,
                MainUnit = dto.MainUnit,
                //Type = dto.Type,
                UnitWeight = dto.UnitWeight,
                UnitCapacity = dto.UnitCapacity,
                NotInStoreMovement = dto.NotInStoreMovement,
                Containers = dto.Containers.Select(c => new ContainerDAO
                {
                    Id = c.Id,
                    RMSContainerId = c.RMSContainerId,
                    Num = c.Num,
                    Name = c.Name,
                    Count = c.Count,
                    MinContainerWeight = c.MinContainerWeight,
                    MaxContainerWeight = c.MaxContainerWeight,
                    ContainerWeight = c.ContainerWeight,
                    FullContainerWeight = c.FullContainerWeight
                }).ToList()
            };
        }

        public static RMSProductDTO ToDTO(ProductDTO dto)
        {
            return new RMSProductDTO
            {
                RMSProductId = dto.id,
                Deleted = dto.deleted,
                Name = dto.name,
                Description = dto.description,
                Num = dto.num,
                Parent = dto.parent,
                MainUnit = dto.mainUnit,
                UnitWeight = dto.unitWeight,
                UnitCapacity = dto.unitCapacity,
                NotInStoreMovement = dto.notInStoreMovement,
                Containers = dto.containers.Select(c => new RMSContainerDTO
                {
                    RMSContainerId = c.id,
                    Num = c.num,
                    Name = c.name,
                    Count = c.count,
                    MinContainerWeight = c.minContainerWeight,
                    MaxContainerWeight = c.maxContainerWeight,
                    ContainerWeight = c.containerWeight,
                    FullContainerWeight = c.fullContainerWeight
                }).ToList()
            };
        }

    }
}
