using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;



namespace UpRestEye3.Services.BusinessLogic
{
    public static class RMSProductHelper
    {

        public static RMSProductDTO? BuildRMSProductDTO(RMSProductDAO? rmsProductDAO)
        {
            if (rmsProductDAO == null)
                return null;

            var rmsProductDTO = new RMSProductDTO
            {
                Id = rmsProductDAO.Id,
                ConsumerId = rmsProductDAO.ConsumerId,
                ConsumerTaxId = rmsProductDAO.Consumer.TaxNumber,
                Name = rmsProductDAO.Name,
                Description = rmsProductDAO.Description,
                RMSProductExtGuid = rmsProductDAO.RMSProductExtGuid,
                Num = rmsProductDAO.Num,
                MainUnit = rmsProductDAO.MainUnit,
                Status = rmsProductDAO.Status,
                Comments = rmsProductDAO.Comments,
                Containers = rmsProductDAO.Containers.Select(c => new RMSContainerDTO
                {
                    Id = c.Id,
                    Num = c.Num,
                    Name = c.Name,
                    RMSContainerExtGuid = c.RMSContainerExtGuid,
                    Count = c.Count,
                    ContainerWeight = c.ContainerWeight,
                    FullContainerWeight = c.FullContainerWeight

                }).ToList()
            };

            return rmsProductDTO;
        }

        public static RMSProductDAO? BuildRMSProductDAO(RMSProductDTO? rmsProductDTO)
        {
            if (rmsProductDTO == null)
                return null;

            var rmsProductDAO = new RMSProductDAO
            {
                Id = rmsProductDTO.Id,
                ConsumerId = rmsProductDTO.ConsumerId,
                Consumer = new ConsumerDAO
                {
                    Id = rmsProductDTO.ConsumerId,
                    TaxNumber = rmsProductDTO.ConsumerTaxId
                },
                Name = rmsProductDTO.Name,
                Description = rmsProductDTO.Description,
                RMSProductExtGuid = rmsProductDTO.RMSProductExtGuid,
                Num = rmsProductDTO.Num,
                MainUnit = rmsProductDTO.MainUnit,
                Status = rmsProductDTO.Status,
                Comments = rmsProductDTO.Comments,
                Containers = rmsProductDTO.Containers.Select(c => new RMSContainerDAO
                {
                    Id = c.Id,
                    Num = c.Num,
                    Name = c.Name,
                    RMSContainerExtGuid = c.RMSContainerExtGuid,
                    Count = c.Count,
                    ContainerWeight = c.ContainerWeight,
                    FullContainerWeight = c.FullContainerWeight
                }).ToList()
            };

            return rmsProductDAO;
        }        
        
        
        public static RMSContainerDTO? BuildRMSContainerDTO(RMSContainerDAO? rmsContainerDAO)
        {
            if (rmsContainerDAO == null)
                return null;

            var rmsContainerDTO = new RMSContainerDTO
            {
                    Id = rmsContainerDAO.Id,
                    Num = rmsContainerDAO.Num,
                    Name = rmsContainerDAO.Name,
                    RMSContainerExtGuid = rmsContainerDAO.RMSContainerExtGuid,
                    Count = rmsContainerDAO.Count,
                    ContainerWeight = rmsContainerDAO.ContainerWeight,
                    FullContainerWeight = rmsContainerDAO.FullContainerWeight

            };

            return rmsContainerDTO;
        }

        public static RMSContainerDAO? BuildRMSContainerDAO(RMSContainerDTO? rmsContainerDTO)
        {
            if (rmsContainerDTO == null)
                return null;

            var rmsContainerDAO = new RMSContainerDAO
            {
                    Id = rmsContainerDTO.Id,
                    Num = rmsContainerDTO.Num,
                    Name = rmsContainerDTO.Name,
                    RMSContainerExtGuid = rmsContainerDTO.RMSContainerExtGuid,
                    Count = rmsContainerDTO.Count,
                    ContainerWeight = rmsContainerDTO.ContainerWeight,
                    FullContainerWeight = rmsContainerDTO.FullContainerWeight
            };

            return rmsContainerDAO;
        }


        public static RMSProductDTO BuildRMSProductDTO(ProductDTO dto)
        {

            if (dto == null)
                return null;

            var rmsProductDTO = new RMSProductDTO
            {
                RMSProductExtGuid = dto.id,
                Name = dto.name,
                Description = dto.description,
                Num = dto.num,
                MainUnit = dto.mainUnit,
                Status = RMSProductStatusEnum.FromRMS,
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

            return rmsProductDTO;

        }


    }

}
