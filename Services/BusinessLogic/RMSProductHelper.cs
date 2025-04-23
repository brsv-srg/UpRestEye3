using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.RMSDTO;



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
                Type = GetDTOItemType(rmsProductDAO.Type),
                Status = rmsProductDAO.Status,
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
                ConsumerId = (int)rmsProductDTO.ConsumerId,
                Consumer = new ConsumerDAO
                {
                    Id = rmsProductDTO.ConsumerId,
                    TaxNumber = rmsProductDTO.ConsumerTaxId
                },
                Name = rmsProductDTO.Name,
                Description = rmsProductDTO.Description ?? string.Empty,
                RMSProductExtGuid = rmsProductDTO.RMSProductExtGuid,
                Num = rmsProductDTO.Num,
                MainUnit = rmsProductDTO.MainUnit,
                Type = GetDAOItemType(rmsProductDTO.Type),
                Status = rmsProductDTO.Status,
                Containers = rmsProductDTO.Containers.Select(c => new RMSContainerDAO
                {
                    Id = c.Id,
                    RMSProductId = (int)rmsProductDTO.ConsumerId,
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

        public static RMSProductDTO? CopyRMSProductDTO(RMSProductDTO? rmsProductDTO)
        {
            if (rmsProductDTO == null)
                return null;

            var _rmsProductDAO = new RMSProductDTO
            {
                Id = rmsProductDTO.Id,
                ConsumerId = rmsProductDTO.ConsumerId,
                ConsumerTaxId = rmsProductDTO.ConsumerTaxId,
                Name = rmsProductDTO.Name,
                Description = rmsProductDTO.Description,
                RMSProductExtGuid = rmsProductDTO.RMSProductExtGuid,
                Num = rmsProductDTO.Num,
                MainUnit = rmsProductDTO.MainUnit,
                Type = rmsProductDTO.Type,
                Status = rmsProductDTO.Status,
                Containers = rmsProductDTO.Containers.Select(c => new RMSContainerDTO
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

        public static RMSProductDAO? CopyRMSProductDAO(RMSProductDAO? rmsProductDAO)
        {
            if (rmsProductDAO == null)
                return null;

            var _rmsProductDAO = new RMSProductDAO
            {
                Id = rmsProductDAO.Id,
                ConsumerId = rmsProductDAO.ConsumerId,
                Name = rmsProductDAO.Name,
                Description = rmsProductDAO.Description,
                RMSProductExtGuid = rmsProductDAO.RMSProductExtGuid,
                Num = rmsProductDAO.Num,
                MainUnit = rmsProductDAO.MainUnit,
                Type = rmsProductDAO.Type,
                Status = rmsProductDAO.Status,
                Containers = rmsProductDAO.Containers.Select(c => new RMSContainerDAO
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


        public static RMSContainerDAO? CopyRMSContainerDAO(RMSContainerDAO? rmsContainerDAO)
        {
            if (rmsContainerDAO == null)
                return null;

            var _rmsContainerDAO = new RMSContainerDAO
            {
                
                Id = rmsContainerDAO.Id,
                RMSProductId = rmsContainerDAO.RMSProductId,
                Num = rmsContainerDAO.Num,
                Name = rmsContainerDAO.Name,
                RMSContainerExtGuid = rmsContainerDAO.RMSContainerExtGuid,
                Count = rmsContainerDAO.Count,
                ContainerWeight = rmsContainerDAO.ContainerWeight,
                FullContainerWeight = rmsContainerDAO.FullContainerWeight
                
            };

            return _rmsContainerDAO;
        }

        public static RMSContainerDTO? CopyRMSContainerDTO(RMSContainerDTO? rmsContainerDTO)
        {
            if (rmsContainerDTO == null)
                return null;

            var _rmsContainerDTO = new RMSContainerDTO
            {

                Id = rmsContainerDTO.Id,
                Num = rmsContainerDTO.Num,
                Name = rmsContainerDTO.Name,
                RMSContainerExtGuid = rmsContainerDTO.RMSContainerExtGuid,
                Count = rmsContainerDTO.Count,
                ContainerWeight = rmsContainerDTO.ContainerWeight,
                FullContainerWeight = rmsContainerDTO.FullContainerWeight

            };

            return _rmsContainerDTO;
        }

        public static RMSAccountDTO? CopyRMSStorageDTO(RMSAccountDTO? rmsStorageDTO)
        {
            if (rmsStorageDTO == null)
                return null;

            var _rmsContainerDTO = new RMSAccountDTO
            {
                Id = rmsStorageDTO.Id,
                ConsumerId = rmsStorageDTO.ConsumerId,
                ConsumerTaxId = rmsStorageDTO.ConsumerTaxId,
                RootType = rmsStorageDTO.RootType,
                Code = rmsStorageDTO.Code,
                Name = rmsStorageDTO.Name,
                EntityExtGuid = rmsStorageDTO.EntityExtGuid,
                Description = rmsStorageDTO.Description,
                Status = rmsStorageDTO.Status
            };
            return _rmsContainerDTO;
        }
        public static RMSAccountDTO? CopyRMSStorageDTO(RMSAccountDAO? rmsStorageDAO)
        {
            if (rmsStorageDAO == null)
                return null;

            var _rmsContainerDTO = new RMSAccountDTO
            {
                Id = rmsStorageDAO.Id,
                ConsumerId = rmsStorageDAO.ConsumerId,
                ConsumerTaxId = rmsStorageDAO.Consumer?.TaxNumber,
                RootType = rmsStorageDAO.RootType,
                Code = rmsStorageDAO.Code,
                Name = rmsStorageDAO.Name,
                EntityExtGuid = rmsStorageDAO.EntityExtGuid,
                Description = rmsStorageDAO.Description,
                Status = rmsStorageDAO.Status
            };
            return _rmsContainerDTO;
        }
        public static RMSAccountDAO? CopyRMSStorageDAO(RMSAccountDTO? rmsStorageDTO)
        {
            if (rmsStorageDTO == null)
                return null;

            var _rmsContainerDAO = new RMSAccountDAO
            {
                Id = rmsStorageDTO.Id,
                ConsumerId = rmsStorageDTO.ConsumerId,
                RootType = rmsStorageDTO.RootType,
                Code = rmsStorageDTO.Code,
                Name = rmsStorageDTO.Name,
                EntityExtGuid = rmsStorageDTO.EntityExtGuid,
                Description = rmsStorageDTO.Description,
                Status = rmsStorageDTO.Status
            };
            return _rmsContainerDAO;
        }

        public static RMSAccountDAO? CopyRMSStorageDAO(RMSAccountDAO? rmsStorageDAO)
        {
            if (rmsStorageDAO == null)
                return null;

            var _rmsContainerDAO = new RMSAccountDAO
            {
                Id = rmsStorageDAO.Id,
                ConsumerId = rmsStorageDAO.ConsumerId,
                RootType = rmsStorageDAO.RootType,
                Code = rmsStorageDAO.Code,
                Name = rmsStorageDAO.Name,
                EntityExtGuid = rmsStorageDAO.EntityExtGuid,
                Description = rmsStorageDAO.Description,
                Status = rmsStorageDAO.Status
            };
            return _rmsContainerDAO;
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


        public static RMSProductDTO BuildRMSProductDTO(GetProductDTO dto)
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
                Type = dto.type,
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

        public static GetProductDTO BuildGetProductDTO(RMSProductDTO dto)
        {

            if (dto == null)
                return null;

            int counter = 1;
            var integrationProduct = new GetProductDTO
            {
                id = dto.RMSProductExtGuid,
                name = dto.Name,
                description = dto.Description,
                num = string.IsNullOrEmpty(dto.Num) ? null : dto.Num,
                mainUnit = dto.MainUnit,
                type = dto.Type,
                containers = dto.Containers.Select(c => new GetContainerDTO
                {
                    id = c.RMSContainerExtGuid,
                    num = string.IsNullOrEmpty(c.Num) ? counter++.ToString("D3") : c.Num,
                    name = c.Name,
                    count = c.Count,
                    containerWeight = c.ContainerWeight,
                    fullContainerWeight = c.FullContainerWeight
                }).ToList()
            };

            return integrationProduct;

        }
        

        public static SaveProductDTO BuildSaveProductDTO(RMSProductDTO dto)
        {

            if (dto == null)
                return null;
            int counter = 1;
            var integrationProduct = new SaveProductDTO
            {
                name = dto.Name,
                description = dto.Description,
                num = string.IsNullOrEmpty(dto.Num) ? null : dto.Num,
                mainUnit = dto.MainUnit,
                type = dto.Type,
                containers = dto.Containers.Select(c => new SaveContainerDTO
                {
                    num = string.IsNullOrEmpty(c.Num) ? counter++.ToString("D3") : c.Num,
                    name = c.Name,
                    count = c.Count,
                    containerWeight = c.ContainerWeight,
                    fullContainerWeight = c.FullContainerWeight
                }).ToList()
            };

            return integrationProduct;

        }

        public static string GetDTOItemType(ItemTypeEnum type)
        {
            switch (type)
            {

                case ItemTypeEnum.GOODS:
                    return "GOODS";
                case ItemTypeEnum.SERVICE:
                    return "SERVICE";
                default:
                    return string.Empty;
            }

        }

        public static ItemTypeEnum GetDAOItemType(string type)
        {
            switch (type)
            {

                case "GOODS":
                    return ItemTypeEnum.GOODS;

                case "SERVICE":
                    return ItemTypeEnum.SERVICE;
                default:
                    return ItemTypeEnum.GOODS;
            }

        }
    }

}
