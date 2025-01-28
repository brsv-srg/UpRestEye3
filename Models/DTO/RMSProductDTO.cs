using System;
using System.Collections.Generic;
using UpRestEye3.Models.BLO;

namespace UpRestEye3.Models.DTO
{
    public class RMSProductDTO
    {
        public int? Id { get; set; }
        public int ConsumerId { get; set; }
        public string ConsumerTaxId { get; set; }
        public Guid RMSProductId { get; set; }
        public bool Deleted { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Num { get; set; }
        public Guid? Parent { get; set; }
        public Guid TaxCategory { get; set; }
        public Guid Category { get; set; }
        public Guid AccountingCategory { get; set; }
        public Guid MainUnit { get; set; }
        public ItemType Type { get; set; }
        public decimal UnitWeight { get; set; }
        public decimal UnitCapacity { get; set; }
        public bool NotInStoreMovement { get; set; }
        public List<ContainerDTO> Containers { get; set; }
    }

    public class ContainerDTO
    {
        public int? Id { get; set; }
        public Guid RMSContainerId { get; set; }
        public string Num { get; set; }
        public string Name { get; set; }
        public decimal Count { get; set; }
        public decimal MinContainerWeight { get; set; }
        public decimal MaxContainerWeight { get; set; }
        public decimal ContainerWeight { get; set; }
        public decimal FullContainerWeight { get; set; }
        public bool BackwardRecalculation { get; set; }
        public bool UseInFront { get; set; }
        public bool Deleted { get; set; }
    }
}
