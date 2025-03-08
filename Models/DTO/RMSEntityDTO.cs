using UpRestEye3.Models.BLO;
namespace UpRestEye3.Models.DTO
{
    public class RMSEntityDTO
    {
        public int? Id { get; set; }
        public int? ConsumerId { get; set; }
        public string ConsumerTaxId { get; set; } = string.Empty;
        public Guid EntityExtGuid { get; set; } = Guid.Empty;
        public string RootType { get; set; } = string.Empty;
        public string? Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public EntityStatus Status { get; set; } = EntityStatus.New;
    }

    public class RMSMeasureUnitDTO : RMSEntityDTO { }

    public class RMSAccountDTO : RMSEntityDTO { }
}
