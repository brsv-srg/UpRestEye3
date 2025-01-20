using System.Globalization;
using System.Text.RegularExpressions;

namespace UpRestEye3.Models.BLO
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
        public DateTime DocDate { get => ParseDate(F); }
        //------------
        public string G { get; set; } = string.Empty;
        public string DocNumber { get => G; set => G = value; }
        //------------
        public string H { get; set; } = string.Empty;
        public string ATCUD { get => H; set => H = value; }
        //------------
        public string I1 { get; set; } = string.Empty;
        //------------
        public string I2 { get; set; } = string.Empty;
        public decimal Base0 { get => ParseDecimal(I2); }
        //------------
        public string I3 { get; set; } = string.Empty;
        public decimal Base6 { get => ParseDecimal(I3); }
        //------------
        public string I4 { get; set; } = string.Empty;
        public decimal IVA6 { get => ParseDecimal(I4); }
        //------------
        public string I5 { get; set; } = string.Empty;
        public decimal Base13 { get => ParseDecimal(I5); }
        //------------
        public string I6 { get; set; } = string.Empty;
        public decimal IVA13 { get => ParseDecimal(I6); }
        //------------
        public string I7 { get; set; } = string.Empty;
        public decimal Base23 { get => ParseDecimal(I7); }
        //------------
        public string I8 { get; set; } = string.Empty;
        public decimal IVA23 { get => ParseDecimal(I8); }
        //------------
        public string N { get; set; } = string.Empty;
        public decimal TotalIVA { get => ParseDecimal(N); }
        //------------
        public string O { get; set; } = string.Empty;
        public decimal TotalAmount { get => ParseDecimal(O); }
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
                        case "I2": I2 = keyValue[1]; break;
                        case "I3": I3 = keyValue[1]; break;
                        case "I4": I4 = keyValue[1]; break;
                        case "I5": I5 = keyValue[1]; break;
                        case "I6": I6 = keyValue[1]; break;
                        case "I7": I7 = keyValue[1]; break;
                        case "I8": I8 = keyValue[1]; break;
                        case "N": N = keyValue[1]; break;
                        case "O": O = keyValue[1]; break;
                        case "Q": Q = keyValue[1]; break;
                        case "R": R = keyValue[1]; break;
                    }
                }
            }
        }

        private static decimal ParseDecimal(string value)
        {
            try
            {
                if (decimal.TryParse(value, out decimal result))
                {
                    return result;
                }
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static DateTime ParseDate(string value)
        {
            return StringToDate.Convert(value);
        }

        public static bool IsMatchingATQRCode(string input)
        {
            string pattern = @"^A:.*\*B:.*\*C:.*\*D:.*\*E:.*\*F:.*\*G:.*\*H:.*$";
            return Regex.IsMatch(input, pattern);
        }

    }

    public static class StringToDate
    {
        // Список распространённых форматов даты и времени
        private static readonly string[] DateFormats = new[]
        {
        "MM-dd-yyyy",        // 12-31-2024
        "dd/MM/yyyy",        // 31/12/2024
        "yyyy-MM-dd",        // 2024-12-31
        "yyyy/MM/dd",        // 2024/12/31
        "yyyy.MM.dd",        // 2024.12.31
        "dd-MM-yyyy",        // 31-12-2024
        "MM/dd/yyyy",        // 12/31/2024
        "yyyyMMdd",          // 20241231
        "MM-dd-yyyy HH:mm",  // 12-31-2024 23:59
        "yyyy-MM-ddTHH:mm:ss", // 2024-12-31T23:59:59
        "yyyy-MM-ddTHH:mm:ssZ", // 2024-12-31T23:59:59Z (UTC)
        "MM/dd/yyyy h:mm tt", // 12/31/2024 11:59 PM
        "dd.MM.yy",          // 31.12.24
        "dd.MM.yyyy"         // 31.12.2024
        };

        public static DateTime Convert(string stringDateTime)
        {
            try
            {
                // Пробуем преобразовать строку в DateTime с помощью разных форматов
                foreach (var format in DateFormats)
                {
                    if (DateTime.TryParseExact(
                            stringDateTime,
                            format,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var date))
                    {
                        return date;
                    }
                }
                return DateTime.Now;
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }

    }
}


/*


MUSE
FIGUEIREDO & ANDRADE, UNIPESSOAL LIMITADA                   
 Sede: Avenida 1º de Maio, 36G, Costa da                    
       Caparica, 2825-393, Portugal                         
              NIF: 515409723


    Avenida 1º de Maio, 36G, Costa da                       
       Caparica, 2825-393, Portugal                         
        2825-393 Costa da Caparica                          
                                                            
Consultas de Mesa                                           
CM 01_01.24G/667                                            
Original - IVA incluído                                     
Consumidor final                                            
------------------------------------------                  
Qt. Descrição            Preço   T   Valor                  
1.000 Americano          1.50    B    1.50                  
1.000 Americano 0% tax   3.50    M    3.50    M99 Não sujeito; não tributado(ou similar)
1.000 Americano 23% tax  3.50    A    3.50                  
1.000 Americano 6% tax   1.50    C    1.50                  
------------------------------------------                  
T O T A L                            10.00                  
------------------------------------------                  
    Base    IVA    Taxa          T   Valor                  
    2.85    23%    0.65          A    3.50                  
    1.33    13%    0.17          B    1.50                  
    1.42     6%    0.08          C    1.50                  
    3.50     0%    0.00          M    3.50                  
------------------------------------------                  
            ATCUD:JJSGNPYG - 667

QR: 
A: 515409723 * 
B:999999990 * 
C:Desconhecido* 
D:CM* 
E:N* 
F:20241229 * 
G:CM 01_01.24G/667*
H:JJSGNPYG - 667 * 
I1:PT* 
I2:3.50 * Base 0%
I3:1.42 * Base 6%
I4:0.08 * Taxa 6%
I5:1.33 * Base 13%
I6:0.17 * Taxa 13%
I7:2.85 * Base 23%
I8:0.65 * Taxa 23%

N:0.90  * Taxa Total   
O:10.00 * Total

Q:bfXe* 

R:3005



bfXe - Processado por programa certificado nº 3005/AT         
Data:     2024 - 12 - 29 23:37:57
Operador: SYSTEM
Mesa:     Order 5099                                        
Este documento não serve de fatura                          
         Obrigado e volte sempre 



*/