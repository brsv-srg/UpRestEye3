using System;
using System.Collections.Generic;
using System.ComponentModel;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.BusinessLogic;

namespace UpRestEye3.Models.DTO
{

    [TypeConverter(typeof(RMSProductDTOConverter))]
    public class RMSProductDTO
    {
        public int? Id { get; set; }
        public int? ConsumerId { get; set; }
        public string ConsumerTaxId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? RMSProductExtGuid { get; set; }
        public string? Num { get; set; } 
        public Guid MainUnit { get; set; } = Guid.Empty;
        public string Type { get; set; } = "GOODS";
        public List<RMSContainerDTO> Containers { get; set; } = new List<RMSContainerDTO>();
        public RMSProductStatusEnum Status { get; set; } = RMSProductStatusEnum.NewProduct;
    }


    [TypeConverter(typeof(RMSContainerDTOConverter))]
    public class RMSContainerDTO
    {
        public int? Id { get; set; }
        public string? Num { get; set; } 
        public string Name { get; set; } = string.Empty;
        public Guid? RMSContainerExtGuid { get; set; } 
        public decimal Count { get; set; } = 0;
        public decimal ContainerWeight { get; set; } = 0;
        public decimal FullContainerWeight { get; set; } = 0;
    }

}
