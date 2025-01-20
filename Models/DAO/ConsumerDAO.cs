using System.Text.Json.Serialization;

namespace UpRestEye3.Models.DAO
{
    public class ConsumerDAO
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;

        public List<SupplierDAO> Suppliers { get; set; }
        public List<InvoiceDAO> Invoices { get; set; }
        public List<UserDAO> Users { get; set; }
        public ConnectionParameterDAO? ConnectionParameter { get; set; }

        public ConsumerDAO()
        {
            Users = new List<UserDAO>();
            Invoices = new List<InvoiceDAO>();
            Suppliers = new List<SupplierDAO>();
        }

    }
}
