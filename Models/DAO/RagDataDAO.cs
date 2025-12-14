namespace UpRestEye3.Models.DAO
{
    /// <summary>
    /// Запись, привязанная к Consumer, с идентификаторами ассистента, vector store и файла.
    /// EF-сущность и одновременно модель для репозитория.
    /// </summary>
    public class RagDataDAO
    {
        public int Id { get; set; }
        public int? ConsumerId { get; set; }
        public ConsumerDAO? Consumer { get; set; }
        public string MappingVectorStoreId { get; set; } = string.Empty;
        public string MappingFileId { get; set; } = string.Empty;
        
        public string ProductsVectorStoreId { get; set; } = string.Empty;
        public string ProductsFileId { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

}
