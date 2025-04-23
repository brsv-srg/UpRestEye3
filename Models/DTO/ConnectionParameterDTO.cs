namespace UpRestEye3.Models.DTO
{
    public class ConnectionParameterDTO
    {
        public string ApiUrl { get; set; } = string.Empty;
        public string ApiLogin { get; set; } = string.Empty;
        public string ApiPassword { get; set; } = string.Empty;
        public int ConsumerId { get; set; } = -1;
        public string ConsumerTaxNumber { get; set; } = string.Empty;
        public int? DeliveryServiceId { get; set; }
        public string? DeliveryServiceName { get; set; }

    }
}
