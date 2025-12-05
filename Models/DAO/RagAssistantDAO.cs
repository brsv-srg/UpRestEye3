namespace UpRestEye3.Models.DAO
{
    /// <summary>
    /// Запись, привязанная к Consumer, с идентификаторами ассистента, vector store и файла.
    /// EF-сущность и одновременно модель для репозитория.
    /// </summary>
    public class RagAssistantDAO
    {
        public int Id { get; set; }
        public int? ConsumerId { get; set; }
        public ConsumerDAO? Consumer { get; set; }
        public string AssistantId { get; set; } = string.Empty;
        public string VectorStoreId { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

}
