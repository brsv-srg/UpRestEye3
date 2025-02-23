using System.ComponentModel;
using UpRestEye3.Services.BusinessLogic;

namespace UpRestEye3.Models.DTO
{
    [TypeConverter(typeof(SupplierDTOConverter))]
    public class SupplierDTO
    {
        public string Name { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;

    }

}
