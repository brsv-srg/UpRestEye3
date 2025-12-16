namespace UpRestEye3.Models.DTO
{
    public class RAGMappingRecordDTO
    {
        public string EmbeddingText { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierTaxNumber { get; set; } = string.Empty;
        public int? InvoiceProductId { get; set; }
        public string InvoiceProductName { get; set; } = string.Empty;
        public string InvoiceUnit { get; set; } = string.Empty;
        public string InvoiceContainer { get; set; } = string.Empty;
        public RAGRMSProductDTO? MappedRmsProduct { get; set; }
        public RAGRMSContainerDTO? MappedRMSContainer { get; set; }

    }

    public class RAGProductsRecordDTO: RAGRMSProductDTO
    {
        public string EmbeddingText { get; set; } = string.Empty;
    }


    public class RAGRMSProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MainUnit { get; set; } = string.Empty;
        public List<RAGRMSContainerDTO> Containers { get; set; } = [];
    }

    public class RAGRMSContainerDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Count { get; set; }
    }



}
