namespace UpRestEye3.Models
{

    public class QRCodeData
    {
        public string A { get; set; } = string.Empty;
        public string SupplierTaxNumber { get => A; set => A = value; }
        //------------
        public string B { get; set; } = string.Empty;
        public string CustomerTaxNumber { get => B; set => B = value; }
        //------------
        public string C { get; set; } = string.Empty;
        //------------
        public string D { get; set; } = string.Empty;
        public string DocType { get => D; set => D = value; }
        //------------
        public string E { get; set; } = string.Empty;
        //------------
        public string F { get; set; } = string.Empty;
        public string DocDate { get => F; set => F = value; }
        //------------
        public string G { get; set; } = string.Empty;
        public string DocNumber { get => G; set => G = value; }
        //------------
        public string H { get; set; } = string.Empty;
        public string ATCUD { get => H; set => H = value; }
        //------------
        public string I1 { get; set; } = string.Empty;
        //------------
        public string I5 { get; set; } = string.Empty;
        public string NetAmount { get => I5; set => I5 = value; }
        //------------
        public string I6 { get; set; } = string.Empty;
        public string N { get; set; } = string.Empty;
        //------------
        public string O { get; set; } = string.Empty;
        public string TotalAmount { get => O; set => O = value; }
        //------------
        public string Q { get; set; } = string.Empty;
        //------------
        public string R { get; set; } = string.Empty;
        public string ATLicenceNumber { get => R; set => R = value; }

        // Default constructor
        public QRCodeData() { }

        // Constructor from string
        public QRCodeData(string data)
        {
            var parts = data.Split('*');
            foreach (var part in parts)
            {
                var keyValue = part.Split(':');
                if (keyValue.Length == 2)
                {
                    switch (keyValue[0])
                    {
                        case "A": A = keyValue[1]; break;
                        case "B": B = keyValue[1]; break;
                        case "C": C = keyValue[1]; break;
                        case "D": D = keyValue[1]; break;
                        case "E": E = keyValue[1]; break;
                        case "F": F = keyValue[1]; break;
                        case "G": G = keyValue[1]; break;
                        case "H": H = keyValue[1]; break;
                        case "I1": I1 = keyValue[1]; break;
                        case "I5": I5 = keyValue[1]; break;
                        case "I6": I6 = keyValue[1]; break;
                        case "N": N = keyValue[1]; break;
                        case "O": O = keyValue[1]; break;
                        case "Q": Q = keyValue[1]; break;
                        case "R": R = keyValue[1]; break;
                    }
                }
            }
        }
    }
}
