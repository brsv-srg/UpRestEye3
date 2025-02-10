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
                RMSProductExtGuid = dao.RMSProductExtGuid,
                Name = dao.Name,
                Description = dao.Description,
                Num = dao.Num,
                MainUnit = dao.MainUnit,
                Type = dao.Type,
                Containers = dao.Containers.Select(c => new RMSContainerDTO
                {
                    Id = c.Id,
                    RMSContainerExtGuid = c.RMSContainerExtGuid,
                    Num = c.Num,
                    Name = c.Name,
                    Count = c.Count,
                    ContainerWeight = c.ContainerWeight,
                    FullContainerWeight = c.FullContainerWeight
                }).ToList(),
                Status = dao.Status,
                Comments = dao.Comments
            };
        }

        public static RMSProductDAO ToDAO(RMSProductDTO dto)
        {
            return new RMSProductDAO
            {
                Id = dto.Id,
                ConsumerId = dto.ConsumerId,
                RMSProductExtGuid = dto.RMSProductExtGuid,
                Name = dto.Name,
                Description = dto.Description,
                Num = dto.Num,
                MainUnit = dto.MainUnit,
                Type = dto.Type,
                Containers = dto.Containers.Select(c => new ContainerDAO
                {
                    Id = c.Id,
                    RMSContainerExtGuid = c.RMSContainerExtGuid,
                    Num = c.Num,
                    Name = c.Name,
                    Count = c.Count,
                    ContainerWeight = c.ContainerWeight,
                    FullContainerWeight = c.FullContainerWeight
                }).ToList(),
                Status = dto.Status,
                Comments = dto.Comments
            };
        }

        public static RMSProductDTO ToDTO(ProductDTO dto)
        {
            return new RMSProductDTO
            {
                RMSProductExtGuid = dto.id,
                Name = dto.name,
                Description = dto.description,
                Num = dto.num,
                MainUnit = dto.mainUnit,
                Containers = dto.containers.Select(c => new RMSContainerDTO
                {
                    RMSContainerExtGuid = c.id,
                    Num = c.num,
                    Name = c.name,
                    Count = c.count,
                    ContainerWeight = c.containerWeight,
                    FullContainerWeight = c.fullContainerWeight
                }).ToList()
            };
        }

    }
}
