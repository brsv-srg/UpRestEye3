using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.ComponentModel.DataAnnotations;


namespace UpRestEye3.Models
{
    public class InvoiceHelper
    {

        public InvoiceHelper()
        {
        }


        // TODO Перенести в сервис
        public static void UpdateInvoiceFromQRCode(Invoice invoice, QRCodeData qrCode, string? filePath)
        {
            if (qrCode != null)
            {
                if (invoice.Consumer == null)
                {
                    invoice.Consumer = new ConsumerInfo
                    {
                        TaxNumber = qrCode.CustomerTaxNumber
                    };
                }
                else
                if (invoice.Consumer.TaxNumber != qrCode.CustomerTaxNumber)
                {
                    invoice.Consumer.TaxNumber = qrCode.CustomerTaxNumber;
                    invoice.Consumer.Id = null;// todo ???
                }

                if (invoice.Supplier == null)
                {
                    invoice.Supplier = new SupplierInfo
                    {
                        TaxNumber = qrCode.SupplierTaxNumber
                    };
                }
                else
                if (invoice.Supplier.TaxNumber != qrCode.SupplierTaxNumber)
                {
                    invoice.Supplier.TaxNumber = qrCode.SupplierTaxNumber;
                    invoice.Supplier.Id = null;// todo ???
                }

                invoice.Info.InvoiceNumber = qrCode.DocNumber;
                invoice.Info.InvoiceDate = qrCode.DocDate;
                invoice.Info.TotalIVA = qrCode.TotalIVA;
                invoice.Info.TotalAmount = qrCode.TotalAmount;

                if (invoice.TaxCategories == null)
                    invoice.TaxCategories = [];
                else
                    invoice.TaxCategories.Clear();

                if (qrCode.Base0 > 0)
                    invoice.TaxCategories.Add(new Taxes { Category = GetTaxCategory("0%"), Base = qrCode.Base0, IVA = qrCode.Base0, Total = qrCode.Base0 });
                if (qrCode.Base6 > 0)
                    invoice.TaxCategories.Add(new Taxes { Category = GetTaxCategory("6%"), Base = qrCode.Base6, IVA = qrCode.IVA6, Total = qrCode.Base6 + qrCode.IVA6 });
                if (qrCode.Base13 > 0)
                    invoice.TaxCategories.Add(new Taxes { Category = GetTaxCategory("13%"), Base = qrCode.Base13, IVA = qrCode.IVA13, Total = qrCode.Base13 + qrCode.IVA13 });
                if (qrCode.Base23 > 0)
                    invoice.TaxCategories.Add(new Taxes { Category = GetTaxCategory("23%"), Base = qrCode.Base23, IVA = qrCode.IVA23, Total = qrCode.Base23 + qrCode.IVA23 });

                invoice.Status = InvoiceStatus.QRCodeProcessed;
                invoice.FilePath = filePath;
            }
            else
            if (filePath != null)
            {
                invoice.Status = InvoiceStatus.RawFile;
                invoice.FilePath = filePath;
            }
            else
            {
                invoice.Status = InvoiceStatus.Error;
                throw new ArgumentException("Both QRCodeData and filePath are null");
            }
        }

        // TODO Перенести в сервис
        public static void CheckAndUpdateInvoice(Invoice invoiceTarget, Invoice invoiceSource)
        {
            if (invoiceTarget.Supplier == null)
            {
                invoiceTarget.Supplier = new SupplierInfo();
            }
            if (invoiceSource.Supplier != null)
            {
                invoiceTarget.Supplier.Name = invoiceSource.Supplier.Name;
                invoiceTarget.Supplier.TaxNumber = invoiceSource.Supplier.TaxNumber;
                invoiceTarget.Supplier.BankAccount = invoiceSource.Supplier.BankAccount;
            }

            if (invoiceTarget.Consumer == null)
            {
                invoiceTarget.Consumer = new ConsumerInfo();
            }
            if (invoiceSource.Consumer != null)
            {
                invoiceTarget.Consumer.Name = invoiceSource.Consumer.Name;
                invoiceTarget.Consumer.TaxNumber = invoiceSource.Consumer.TaxNumber;
            }

            invoiceTarget.Info.InvoiceNumber = invoiceSource.Info.InvoiceNumber;
            invoiceTarget.Info.InvoiceDate = invoiceSource.Info.InvoiceDate;
            invoiceTarget.Info.TotalIVA = invoiceSource.Info.TotalIVA;
            invoiceTarget.Info.TotalAmount = invoiceSource.Info.TotalAmount;

            invoiceTarget.Products = invoiceSource.Products;
            invoiceTarget.TaxCategories = invoiceSource.TaxCategories;


            if (invoiceSource.Status != null)
            {
                invoiceTarget.Status = invoiceSource.Status;
            }

            if (!string.IsNullOrWhiteSpace(invoiceSource.FilePath))
            {
                invoiceTarget.FilePath = invoiceSource.FilePath;
            }

        }

        public static TaxCategory GetTaxCategory(string stringCategory)
        {

            stringCategory = stringCategory.ToLower();
            if (stringCategory.Contains("23") || stringCategory.ToLower().Contains("nor"))
            {
                return TaxCategory.Normal;
            }
            else if (stringCategory.Contains("13") || stringCategory.ToLower().Contains("int"))
            {
                return TaxCategory.Intermediate;
            }
            else if (stringCategory.Contains("6") || stringCategory.ToLower().Contains("red"))
            {
                return TaxCategory.Reduced;
            }
            else if (stringCategory.Contains("0") || stringCategory.ToLower().Contains("na") || stringCategory.ToLower().Contains("nã"))
            {
                return TaxCategory.Zero;
            }
            else
            {
                throw new ArgumentException("Invalid category string");
            }
        }
    }

    public class DecimalJsonConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // Если значение строка, пытаемся преобразовать её в decimal
                if (decimal.TryParse(reader.GetString(), out var result))
                {
                    return result;
                }
                throw new JsonException("Invalid format for decimal in JSON string.");
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                // Если значение уже число, конвертируем его в decimal
                return reader.GetDecimal();
            }
            else if (reader.TokenType == JsonTokenType.Null)
            {
                return 0;
            }

            throw new JsonException("Invalid token type for decimal.");
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            // Сериализуем как число
            writer.WriteNumberValue(value);
        }
    }

    public class IntegerJsonConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // Если значение строка, пытаемся преобразовать её в int
                if (int.TryParse(reader.GetString(), out var result))
                {
                    return result;
                }
                throw new JsonException("Invalid format for integer in JSON string.");
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                // Если значение уже число, конвертируем его в int
                int res = reader.GetInt32();
                return res;
            }
            else if (reader.TokenType == JsonTokenType.Null)
            {
                return 0;
            }

            throw new JsonException("Invalid token type for integer.");
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            // Сериализуем как число
            writer.WriteNumberValue(value);
        }
    }

    public class DateTimeJsonConverter : JsonConverter<DateTime>
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

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();

                // Пробуем преобразовать строку в DateTime с помощью разных форматов
                foreach (var format in DateFormats)
                {
                    if (DateTime.TryParseExact(
                            dateString,
                            format,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var date))
                    {
                        return date;
                    }
                }

                throw new JsonException($"Unable to parse DateTime from string: {dateString}");
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                // Если значение — это числовое представление даты (например, Unix timestamp)
                var timestamp = reader.GetInt64();
                return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
            }
            else if (reader.TokenType == JsonTokenType.Null)
            {
                return DateTime.Now;
            }

            throw new JsonException($"Unexpected token type: {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Преобразуем дату в стандартный ISO 8601 формат
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
        }
    }

    public class TaxCategoryJsonConverter : JsonConverter<TaxCategory>
    {
        public override TaxCategory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                return InvoiceHelper.GetTaxCategory(stringValue);
            }
            throw new JsonException("Invalid token type for TaxCategory.");
        }

        public override void Write(Utf8JsonWriter writer, TaxCategory value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public static class InvoiceJsonHelper
    {

        public static string GetSchema()
        {
            return _invoiceSchema;
        }

        public static void ValidateInvoiceSchema()
        {
            // Validate schema version
            if (!_invoiceSchema.Contains(@"""$schema"": ""http://json-schema.org/draft-07/schema#"""))
            {
                throw new Exception("Schema is not compatible with Draft-07.");
            }

            // Validate schema structure
            try
            {
                var schema = JsonDocument.Parse(_invoiceSchema);
                Console.WriteLine("Schema is valid and compatible with Draft-07.");
            }
            catch (JsonException ex)
            {
                throw new Exception("Schema validation failed: " + ex.Message);
            }
        }

        public static void ValidateInvoiceObject()
        {
            try
            {

                Invoice invoice = new Invoice();

                var options = new JsonSerializerOptions
                {
                    Converters = { new DecimalJsonConverter(), 
                                    new IntegerJsonConverter(), 
                                    new DateTimeJsonConverter(), 
                                    new TaxCategoryJsonConverter() },
                    WriteIndented = true
                };
                string invoiceJsonString = JsonSerializer.Serialize(invoice, options);
                var invoiceJsonDoc = JsonDocument.Parse(invoiceJsonString);

        
                var invoiceStr = JsonSerializer.Deserialize<Invoice>(invoiceJsonDoc);
                var context = new ValidationContext(invoice, serviceProvider: null, items: null);
                Validator.ValidateObject(invoice, context, validateAllProperties: true);
                Console.WriteLine("Object is valid against the schema.");
            }
            catch (ValidationException ex)
            {
                throw new Exception("Object validation against schema failed: " + ex.Message);
            }
        }
    
        /*

    // Unit Test Example
    public class Program
    {
        public static void Main()
        {
            // Validate schema
            InvoiceJsonHelper.ValidateInvoiceSchema();

            // Create an invoice object
            var invoice = new Invoice
            {
                Id = "123",
                Customer = "John Doe",
                Amount = 100.0m,
                Date = DateTime.Parse("2023-10-01T00:00:00Z")
            };

            // Serialize the invoice object to JSON
            string invoiceJson = JsonSerializer.Serialize(invoice);

            // Validate object
            InvoiceJsonHelper.ValidateInvoiceObject(invoiceJson);
        }
}*/





        private const string _invoiceSchema = $@"
{{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""title"": ""Invoice"",
  ""type"": ""object"",
  ""properties"": {{
    ""Supplier"": {{
      ""type"": ""object"",
      ""properties"": {{
        ""Name"": {{
          ""type"": ""string""
        }},
        ""TaxNumber"": {{
          ""type"": ""string""
        }},
        ""BankAccount"": {{
          ""type"": [""string"", ""null""]
        }}
      }}
    }},
    ""Consumer"": {{
      ""type"": ""object"",
      ""properties"": {{
        ""Name"": {{
          ""type"": ""string""
        }},
        ""TaxNumber"": {{
          ""type"": ""string""
        }}
      }}
    }},
    ""Info"": {{
      ""type"": ""object"",
      ""properties"": {{
        ""InvoiceNumber"": {{
          ""type"": ""string""
        }},
        ""InvoiceDate"": {{
          ""type"": ""string"",
          ""format"": ""date-time""
        }},
        ""TotalIVA"": {{
          ""type"": ""number""
        }},
        ""TotalAmount"": {{
          ""type"": ""number""
        }}
      }}
    }},
    ""Products"": {{
      ""type"": ""array"",
      ""items"": {{
        ""type"": ""object"",
        ""properties"": {{
          ""ProductCode"": {{
            ""type"": ""string""
          }},
          ""ProductName"": {{
            ""type"": ""string""
          }},
          ""Unit"": {{
            ""type"": ""string""
          }},
          ""Quantity"": {{
            ""type"": ""number""
          }},
          ""Price"": {{
            ""type"": ""number""
          }}
        }}
      }}
    }},
    ""TaxCategories"": {{
      ""type"": ""array"",
      ""items"": {{
        ""type"": ""object"",
        ""properties"": {{
          ""Category"": {{
            ""type"": ""string"",
            ""format"": ""tax-category""

          }},
            ""Base"": {{
                ""type"": ""number""
            }},
            ""IVA"": {{
                ""type"": ""number""
            }},
          ""Total"": {{
            ""type"": ""number""
          }}

        }}
      }}
    }},
    ""Comments"": {{
      ""type"": ""string""
    }}
  }}
}}
";

    }

}
