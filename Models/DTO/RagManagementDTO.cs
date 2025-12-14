namespace UpRestEye3.Models.DTO
{

    /// <summary>
    /// Запись, привязанная к Consumer, с идентификаторами ассистента, vector store и файла.
    /// EF-сущность и одновременно модель для репозитория.
    /// </summary>
    public class RagManagementDTO

    {
        public int? Id { get; set; }

        public string ConsumerTaxNumber { get; set; } = string.Empty;

        public string MappingVectorStoreId { get; set; } = string.Empty;

        public string MappingFileId { get; set; } = string.Empty;

        public string ProductsVectorStoreId { get; set; } = string.Empty;

        public string ProductsFileId { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

}
