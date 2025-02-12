using System;
using System.Collections.Generic;
using UpRestEye3.Models.BLO;

namespace UpRestEye3.Models.DTO
{
    public class RMSProductDTO
    {
        public int? Id { get; set; }
        public int ConsumerId { get; set; }
        public string ConsumerTaxId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid RMSProductExtGuid { get; set; } = Guid.NewGuid();
        public string Num { get; set; } = string.Empty;
        public Guid MainUnit { get; set; } = Guid.Empty;
        public List<RMSContainerDTO> Containers { get; set; } = new List<RMSContainerDTO>();
        public RMSProductStatusEnum Status { get; set; } = RMSProductStatusEnum.NewProduct;
        public string Comments { get; set; } = string.Empty;
    }



    public class RMSContainerDTO
    {
        public int? Id { get; set; }
        public string Num { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid RMSContainerExtGuid { get; set; } = Guid.NewGuid();
        public decimal Count { get; set; } = 0;
        public decimal ContainerWeight { get; set; } = 0;
        public decimal FullContainerWeight { get; set; } = 0;
    }

}
