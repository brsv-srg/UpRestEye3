namespace UpRestEye3.Models.DTO
{
    public class RAGFlatRecordDTO
    {
        public string SupplierName { get; set; } = string.Empty;
        public string InvoiceProductName { get; set; } = string.Empty;
        public string InvoiceUnit { get; set; } = string.Empty;
        public string InvoiceContainer { get; set; } = string.Empty;
        public RAGRMSProductDTO? MappedRmsProduct { get; set; }
        public RAGRMSContainerDTO? MappedRMSContainer { get; set; }

        public string Comment { get; set; } = string.Empty;
    }

    public class RAGRMSProductDTO
    {
        public string Name { get; set; } = string.Empty;
        public string MainUnit { get; set; } = string.Empty;
        public List<RAGRMSContainerDTO> Containers { get; set; } = [];
    }

    public class RAGRMSContainerDTO
    {
        public string Name { get; set; } = string.Empty;
        public decimal Count { get; set; }
    }

    public class RAGSupplierDTO
    {
        public SupplierDTO Supplier { get; set; }
        public List<InvoiceProductDTO> Products { get; set; } = [];
    }

    public class RAGFileDTO
    {
        public List<RAGSupplierDTO> Suppliers { get; set; } = [];
    }


}
