using System.Xml.Serialization;

namespace UpRestEye3.Models.RMSDTO
{
    public class EmployeeDTO
    {
        [XmlElement(ElementName= "id")]
        public string Id { get; set; } = string.Empty;
        [XmlElement(ElementName = "code")]
        public string Code { get; set; } = string.Empty;
        [XmlElement(ElementName = "name")]
        public string Name { get; set; } = string.Empty;
        [XmlElement(ElementName = "login")]
        public string? Login { get; set; }
        [XmlElement(ElementName = "password")]
        public string? Password { get; set; }
        [XmlElement(ElementName = "mainRoleCode")]
        public string? MainRoleCode { get; set; }
        [XmlElement(ElementName = "roleCodes")]
        public List<string>? RoleCodes { get; set; }
        [XmlElement(ElementName = "phone")]
        public string? Phone { get; set; }
        [XmlElement(ElementName = "cellPhone")]
        public string? CellPhone { get; set; }
        [XmlElement(ElementName = "firstName")]
        public string? FirstName { get; set; }
        [XmlElement(ElementName = "middleName")]
        public string? MiddleName { get; set; }
        [XmlElement(ElementName = "lastName")]
        public string? LastName { get; set; }
        [XmlElement(ElementName = "birthday")]
        public DateTime? Birthday { get; set; }
        [XmlElement(ElementName = "email")]
        public string? Email { get; set; }
        [XmlElement(ElementName = "address")]
        public string? Address { get; set; }
        [XmlElement(ElementName = "hireDate")]
        public string? HireDate { get; set; }
        [XmlElement(ElementName = "hireDocumentNumber")]
        public string? HireDocumentNumber { get; set; }
        [XmlElement(ElementName = "fireDate")]
        public DateTime? FireDate { get; set; }
        [XmlElement(ElementName = "note")]
        public string? Note { get; set; }
        [XmlElement(ElementName = "cardNumber")]
        public string? CardNumber { get; set; }
        [XmlElement(ElementName = "pinCode")]
        public string? PinCode { get; set; }
        [XmlElement(ElementName = "taxpayerIdNumber")]
        public string? TaxpayerIdNumber { get; set; }
        [XmlElement(ElementName = "snils")]
        public string? Snils { get; set; }
        [XmlElement(ElementName = "gln")]
        public string? Gln { get; set; }
        [XmlElement(ElementName = "activationDate")]
        public DateTime? ActivationDate { get; set; }
        [XmlElement(ElementName = "deactivationDate")]
        public DateTime? DeactivationDate { get; set; }
        [XmlElement(ElementName = "preferredDepartmentCode")]
        public string? PreferredDepartmentCode { get; set; }
        [XmlElement(ElementName = "departmentCodes")]
        public List<string>? DepartmentCodes { get; set; }
        [XmlElement(ElementName = "responsibilityDepartmentCodes")]
        public List<string>? ResponsibilityDepartmentCodes { get; set; }

        [XmlElement(ElementName = "deleted")]
        public string? DeletedFlag { get; set; }

        [XmlElement(ElementName = "supplier")]
        public string? SupplierFlag { get; set; }

        [XmlElement(ElementName = "employee")]
        public string? EmployeeFlag { get; set; }

        [XmlElement(ElementName = "client")]
        public string? ClientFlag { get; set; }
    }

    [XmlRoot("employees")]
    public class EmployeesDTO 
    {
        [XmlElement("employee")]
        public List<EmployeeDTO> EmployeeList { get; set; } = new List<EmployeeDTO>();
    }
}
