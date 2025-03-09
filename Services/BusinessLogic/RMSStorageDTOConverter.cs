using System;
using System.ComponentModel;
using System.Globalization;
using UpRestEye3.Components.Pages;
using UpRestEye3.Models.DTO;


namespace UpRestEye3.Services.BusinessLogic
{
    
    public class RMSAccountDTOConverter : TypeConverter
    {
        private RMSAccountDTO[]? _rmsStorages;
        public void SetSourceList(RMSAccountDTO[] rmsStorages)
        {
            _rmsStorages = rmsStorages;
        }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (string.IsNullOrEmpty(value.ToString()))
                return null;
            if (value is string idStr && int.TryParse(idStr, out int id) && _rmsStorages != null)
            {
                return _rmsStorages.FirstOrDefault(c => c.Id == id);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is RMSAccountDTO rmsStorages)
            {
                return $"{rmsStorages.Id}";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
