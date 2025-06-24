using System.ComponentModel;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Models.BLO;

namespace UpRestEye3.Models.DTO
{
    [TypeConverter(typeof(SupplierDTOConverter))]
    public class SupplierDTO
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string? BankAccount { get; set; } = string.Empty;
        public SupplierStatus Status { get; set; } = SupplierStatus.New;
        public Guid? RMSSupplierId { get; set; }
        public int? ConsumerId { get; set; }
        public string? ConsumerTaxId { get; set; } = string.Empty;
        public bool HasInvoices { get; set; } = false;
    }
}
