namespace UpRestEye3.Models.DAO
{
    public class RMSMeasureUnitDAO
    {
        public int? Id { get; set; }
        public int? ConsumerId { get; set; }
        public ConsumerDAO Consumer { get; set; } = new ConsumerDAO();
        public Guid MeasureUnitExtGuid { get; set; } = Guid.Empty;
        public string RootType { get; set; } = string.Empty;
        public bool Deleted { get; set; } = false;
        public string? Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
