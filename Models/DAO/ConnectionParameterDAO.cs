namespace UpRestEye3.Models.DAO
{
    public class ConnectionParameterDAO
    {
        public int Id { get; set; }
        public string ApiUrl { get; set; } = string.Empty;
        public string ApiLogin { get; set; } = string.Empty;
        public string ApiPassword { get; set; } = string.Empty;
        public int? ConsumerId { get; set; }
        public ConsumerDAO? Consumer { get; set; }
    }
}
