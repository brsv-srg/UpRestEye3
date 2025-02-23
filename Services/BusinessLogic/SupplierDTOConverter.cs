using System;
using System.ComponentModel;
using System.Globalization;
using UpRestEye3.Models.DTO;

namespace UpRestEye3.Services.BusinessLogic
{
    public class SupplierDTOConverter : TypeConverter
    {
        private SupplierDTO[]? _suppliers;
        public void SetSourceList(SupplierDTO[] suppliers)
        {
            _suppliers = suppliers;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string taxId && _suppliers != null)
            {
                return _suppliers.FirstOrDefault(c => c.TaxNumber == taxId);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is SupplierDTO supplier)
            {
                return $"{supplier.TaxNumber}";
            }
            return base.ConvertTo(context, culture, value, destinationType);

        }

    }
}
