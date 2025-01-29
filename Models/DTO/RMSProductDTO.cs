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
        public Guid RMSProductId { get; set; } = Guid.NewGuid();
        public bool Deleted { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Num { get; set; } = string.Empty;
        public Guid? Parent { get; set; } = Guid.Empty;
        public Guid TaxCategory { get; set; } = Guid.Empty;
        public Guid Category { get; set; } = Guid.Empty;
        public Guid AccountingCategory { get; set; } = Guid.Empty;
        public Guid MainUnit { get; set; } = Guid.Empty;
        public ItemType Type { get; set; } = ItemType.GOODS;
        public decimal UnitWeight { get; set; } = 0;
        public decimal UnitCapacity { get; set; } = 0;
        public bool NotInStoreMovement { get; set; } = false;
        public List<RMSContainerDTO> Containers { get; set; } = new List<RMSContainerDTO>();
    }

    public class RMSContainerDTO
    {
        public int? Id { get; set; }
        public Guid RMSContainerId { get; set; } = Guid.NewGuid();
        public string Num { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Count { get; set; } = 0;
        public decimal MinContainerWeight { get; set; } = 0;
        public decimal MaxContainerWeight { get; set; } = 0;
        public decimal ContainerWeight { get; set; } = 0;
        public decimal FullContainerWeight { get; set; } = 0;
        public bool BackwardRecalculation { get; set; } = false;
        public bool UseInFront { get; set; } = false;
        public bool Deleted { get; set; } = false;
    }
}
