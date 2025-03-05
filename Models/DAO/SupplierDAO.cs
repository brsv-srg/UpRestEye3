using UpRestEye3.Models.BLO;
namespace UpRestEye3.Models.DAO
{
    public class SupplierDAO
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string? BankAccount { get; set; } = string.Empty;
        public SupplierStatus Status { get; set; } = SupplierStatus.New;
        public Guid? RMSSupplierId { get; set; }
        public List<InvoiceDAO> Invoices { get; set; }
        public int ConsumerId { get; set; }
        public ConsumerDAO Consumer { get; set; }

        public SupplierDAO()
        {
            Invoices = new List<InvoiceDAO>();
        }

    }

}
