using System.Xml.Serialization;


namespace UpRestEye3.Models.RMSDTO
{
    [XmlRoot("document")]
    public class IncomingInvoiceDto
    {
        [XmlArray("items")]
        [XmlArrayItem("item")]
        public IncomingInvoiceItemDto[] Items { get; set; }

        [XmlElement("id")]
        public string Id { get; set; }

        [XmlElement("conception")]
        public string Conception { get; set; }

        [XmlElement("conceptionCode")]
        public string ConceptionCode { get; set; }

        [XmlElement("comment")]
        public string Comment { get; set; }

        [XmlElement("documentNumber")]
        public string DocumentNumber { get; set; }

        [XmlElement("dateIncoming")]
        public string DateIncoming { get; set; }

        [XmlElement("invoice")]
        public string Invoice { get; set; }

        [XmlElement("defaultStore")]
        public string DefaultStore { get; set; }

        [XmlElement("supplier")]
        public string Supplier { get; set; }

        [XmlElement("dueDate")]
        public string DueDate { get; set; }

        [XmlElement("incomingDate")]
        public string IncomingDate { get; set; }

        [XmlElement("useDefaultDocumentTime")]
        public bool UseDefaultDocumentTime { get; set; } = false;

        [XmlElement("status")]
        public DocumentStatus Status { get; set; }

        [XmlElement("incomingDocumentNumber")]
        public string IncomingDocumentNumber { get; set; }

        [XmlElement("employeePassToAccount")]
        public string EmployeePassToAccount { get; set; }

        [XmlElement("transportInvoiceNumber")]
        public string TransportInvoiceNumber { get; set; }

        [XmlElement("linkedOutgoingInvoiceId")]
        public string LinkedOutgoingInvoiceId { get; set; }

        [XmlElement("distributionAlgorithm")]
        public DistributionAlgorithmType DistributionAlgorithm { get; set; }
    }

    public class IncomingInvoiceItemDto
    {
        [XmlElement("isAdditionalExpense")]
        public bool IsAdditionalExpense { get; set; } = false;

        [XmlElement("amount")]
        public decimal Amount { get; set; }

        [XmlElement("supplierProduct")]
        public string SupplierProduct { get; set; }

        [XmlElement("supplierProductArticle")]
        public string SupplierProductArticle { get; set; }

        [XmlElement("product")]
        public string Product { get; set; }

        [XmlElement("productArticle")]
        public string ProductArticle { get; set; }

        [XmlElement("producer")]
        public string Producer { get; set; }

        [XmlElement("num")]
        public int Num { get; set; }

        [XmlElement("containerId")]
        public string ContainerId { get; set; }

        [XmlElement("amountUnit")]
        public string AmountUnit { get; set; }

        [XmlElement("actualUnitWeight")]
        public decimal ActualUnitWeight { get; set; }

        [XmlElement("sum")]
        public decimal Sum { get; set; }

        [XmlElement("discountSum")]
        public decimal DiscountSum { get; set; }

        [XmlElement("vatPercent")]
        public decimal VatPercent { get; set; }

        [XmlElement("vatSum")]
        public decimal VatSum { get; set; }

        [XmlElement("priceUnit")]
        public string PriceUnit { get; set; }

        [XmlElement("price")]
        public decimal Price { get; set; }

        [XmlElement("priceWithoutVat")]
        public decimal PriceWithoutVat { get; set; }

        [XmlElement("code")]
        public string Code { get; set; }

        [XmlElement("store")]
        public string Store { get; set; }

        [XmlElement("customsDeclarationNumber")]
        public string CustomsDeclarationNumber { get; set; }

        [XmlElement("actualAmount")]
        public decimal ActualAmount { get; set; }
    }

    public enum DocumentStatus
    {
        [XmlEnum("NEW")]
        New,

        [XmlEnum("PROCESSED")]
        Processed,

        [XmlEnum("DELETED")]
        Deleted
    }

    public enum DistributionAlgorithmType
    {
        [XmlEnum("DISTRIBUTION_BY_SUM")]
        DistributionBySum,

        [XmlEnum("DISTRIBUTION_BY_AMOUNT")]
        DistributionByAmount,

        [XmlEnum("DISTRIBUTION_NOT_SPECIFIED")]
        DistributionNotSpecified
    }
}