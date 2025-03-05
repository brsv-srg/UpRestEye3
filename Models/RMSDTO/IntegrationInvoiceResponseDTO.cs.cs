using System;
using System.Xml.Serialization;

namespace UpRestEye3.Models.RMSDTO
{
    [XmlRoot("documentValidationResult")]
    public class DocumentValidationResult
    {
        [XmlElement("valid")]
        public bool Valid { get; set; }

        [XmlElement("warning")]
        public bool Warning { get; set; }

        [XmlElement("documentNumber")]
        public string DocumentNumber { get; set; }

        [XmlElement("otherSuggestedNumber")]
        public string OtherSuggestedNumber { get; set; }

        [XmlElement("errorMessage")]
        public string ErrorMessage { get; set; }

        [XmlElement("additionalInfo")]
        public string AdditionalInfo { get; set; }
    }
}
