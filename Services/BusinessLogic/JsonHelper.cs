using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.Recognition;
using Google.Api;



namespace UpRestEye3.Services.BusinessLogic
{
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
        "M/d/yyyy",          // 9/13/2024
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
                return DateTime.Now;
                //throw new JsonException($"Unable to parse DateTime from string: {dateString}");
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

    public class TaxCategoryEnumJsonConverter : JsonConverter<TaxCategoryEnum>
    {
        public override TaxCategoryEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                return InvoiceHelper.GetTaxCategory(stringValue);
            }
            else
            if (reader.TokenType == JsonTokenType.Number)
            {
                var stringValue = reader.GetDecimal().ToString();
                return InvoiceHelper.GetTaxCategory(stringValue);
            }
            throw new JsonException("Invalid token type for TaxCategory.");
        }

        public override void Write(Utf8JsonWriter writer, TaxCategoryEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class RMSProductStatusEnumJsonConverter : JsonConverter<RMSProductStatusEnum>
    {
        public override RMSProductStatusEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();

                if (stringValue.Contains("NewProduct"))
                {
                    return RMSProductStatusEnum.NewProduct;
                }
                else if (stringValue.Contains("NewContainer"))
                {
                    return RMSProductStatusEnum.NewContainer;
                }
                else if (stringValue.Contains("RMS"))
                {
                    return RMSProductStatusEnum.FromRMS;
                }
            }
            throw new JsonException("Invalid token type for TaxCategory.");
        }

        public override void Write(Utf8JsonWriter writer, RMSProductStatusEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class InvoiceStatusEnumJsonConverter : JsonConverter<InvoiceStatusEnum>
    {
        public override InvoiceStatusEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                return stringValue switch
                {
                    var s when s.Contains("New") => InvoiceStatusEnum.New,
                    var s when s.Contains("RawFile") => InvoiceStatusEnum.RawFile,
                    var s when s.Contains("QRCodeProcessed") => InvoiceStatusEnum.QRCodeProcessed,
                    var s when s.Contains("TextProcessed") => InvoiceStatusEnum.TextProcessed,
                    var s when s.Contains("ProductsMapped") => InvoiceStatusEnum.ProductsMapped,
                    var s when s.Contains("SavedToSystem") => InvoiceStatusEnum.SavedToSystem,
                    var s when s.Contains("TextRecognitionError") => InvoiceStatusEnum.TextRecognitionError,
                    var s when s.Contains("QRError") => InvoiceStatusEnum.QRError,
                    var s when s.Contains("MappingError") => InvoiceStatusEnum.MappingError,
                    var s when s.Contains("UploadError") => InvoiceStatusEnum.UploadError,
                    var s when s.Contains("ProcessError") => InvoiceStatusEnum.ProcessError,
                    _ => throw new JsonException("Invalid token type for TaxCategory.")
                };
            }
            throw new JsonException("Invalid token type for TaxCategory.");
        }
        
        public override void Write(Utf8JsonWriter writer, InvoiceStatusEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class ItemTypeEnumJsonConverter : JsonConverter<ItemTypeEnum>
    {
        public override ItemTypeEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                return stringValue switch
                {
                    var s when s.Contains("GOODS") => ItemTypeEnum.GOODS,
                    var s when s.Contains("DISH") => ItemTypeEnum.DISH,
                    var s when s.Contains("PREPARED") => ItemTypeEnum.PREPARED,
                    var s when s.Contains("SERVICE") => ItemTypeEnum.SERVICE,
                    var s when s.Contains("MODIFIER") => ItemTypeEnum.MODIFIER,
                    var s when s.Contains("OUTER") => ItemTypeEnum.OUTER,
                    var s when s.Contains("RATE") => ItemTypeEnum.RATE,
                    _ => throw new JsonException("Invalid token type for TaxCategory.")

                };
            }
            throw new JsonException("Invalid token type for TaxCategory.");
        }
        
        public override void Write(Utf8JsonWriter writer, ItemTypeEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public static class JsonHelper
    {

        public static string GetInvoiceSchema()
        {
            return _invoiceSchema;
        }

        public static string GetMappedInvoiceSchema()
        {
            return _mappedInvoiceSchema;
        }
        
        public static string GetMappedProductSchema()
        {
            return _mappedProductsSchema;
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
            catch (Exception ex)
            {
                throw new Exception("Schema validation failed: " + ex.Message);
            }
        }

        public static void ValidateInvoiceObject()
        {
            try
            {

                InvoiceDTO invoice = new InvoiceDTO();
                invoice.Status = InvoiceStatusEnum.ProductsMapped;

                var options = JsonHelper.GetSerializerOptions();
                string invoiceJsonString = JsonSerializer.Serialize(invoice, options);
                var invoiceJsonDoc = JsonDocument.Parse(invoiceJsonString);


                var invoiceStr = invoiceJsonDoc.Deserialize<InvoiceDTO>(options);
                var context = new ValidationContext(invoice, serviceProvider: null, items: null);
                Validator.ValidateObject(invoice, context, validateAllProperties: true);
                Console.WriteLine("Object is valid against the schema.");
            }
            catch (Exception ex)
            {
                throw new Exception("Object validation against schema failed: " + ex.Message);
            }
        }





        public static void ValidateMappedInvoiceSchema()
        {
            // Validate schema version
            if (!_mappedInvoiceSchema.Contains(@"""$schema"": ""http://json-schema.org/draft-07/schema#"""))
            {
                throw new Exception("Schema is not compatible with Draft-07.");
            }

            // Validate schema structure
            try
            {
                var schema = JsonDocument.Parse(_mappedInvoiceSchema);
                Console.WriteLine("Schema is valid and compatible with Draft-07.");
            }
            catch (Exception ex)
            {
                throw new Exception("Schema validation failed: " + ex.Message);
            }
        }

        public static void ValidateMappedInvoiceObject()
        {
            try
            {
                var invoice = new InvoiceDTO();
                invoice.Products = new List<InvoiceProductDTO>();
                invoice.Products.Add(new InvoiceProductDTO());

                var options = JsonHelper.GetSerializerOptions();

                string rmsProductJsonString = JsonSerializer.Serialize(invoice, options);
                var rmsProductsJsonDoc = JsonDocument.Parse(rmsProductJsonString);

                var invoice2 = rmsProductsJsonDoc.Deserialize<InvoiceProductDTO>(options);
                var context = new ValidationContext(invoice2, serviceProvider: null, items: null);
                Validator.ValidateObject(invoice2, context, validateAllProperties: true);
                Console.WriteLine("Object is valid against the schema.");
            }
            catch (Exception ex)
            {
                throw new Exception("Object validation against schema failed: " + ex.Message);
            }
        }


        public static JsonSerializerOptions GetSerializerOptions()
        {
            return new JsonSerializerOptions
            {
            Converters = { new DateTimeJsonConverter(),
                                            new DecimalJsonConverter(),
                                            new IntegerJsonConverter(),
                                            new TaxCategoryEnumJsonConverter(),
                                            new InvoiceStatusEnumJsonConverter(),
                                            new RMSProductStatusEnumJsonConverter(),
                                            new ItemTypeEnumJsonConverter()},
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
            };
        }

        private const string _invoiceSchema = $@"
            {{
                ""$schema"": ""http://json-schema.org/draft-07/schema#"",
                ""type"": ""object"",
                ""properties"": {{
                    ""Id"": {{ ""type"": [""integer"", ""null""] }},
                    ""InvoiceNumber"": {{ ""type"": ""string"" }},
                    ""InvoiceDate"": {{ ""type"": ""string"", ""format"": ""date-time"" }},
                    ""TotalIVA"": {{ ""type"": ""number"" }},
                    ""TotalAmount"": {{ ""type"": ""number"" }},
                    ""Consumer"": {{
                        ""type"": ""object"",
                        ""properties"": {{
                            ""Name"": {{ ""type"": ""string"" }},
                            ""TaxNumber"": {{ ""type"": ""string"" }}
                        }}
                    }},
                    ""Supplier"": {{
                        ""type"": ""object"",
                        ""properties"": {{
                            ""Name"": {{ ""type"": ""string"" }},
                            ""TaxNumber"": {{ ""type"": ""string"" }},
                            ""BankAccount"": {{ ""type"": ""string"" }}
                        }}
                    }},
                    ""Products"": {{
                        ""type"": ""array"",
                        ""items"": {{
                            ""type"": ""object"",
                            ""properties"": {{
                                ""Id"": {{ ""type"": [""integer"", ""null""] }},
                                ""ProductCode"": {{ ""type"": ""string"" }},
                                ""ProductName"": {{ ""type"": ""string"" }},
                                ""Unit"": {{ ""type"": ""string"" }},
                                ""Quantity"": {{ ""type"": ""number"" }},
                                ""Price"": {{ ""type"": ""number"" }},
                                ""TaxCategory"": {{ ""type"": ""string"", ""enum"": [""Normal"", ""Intermediate"", ""Reduced"", ""Zero""] }}
                            }}
                        }}
                    }},
                    ""TaxCategories"": {{
                        ""type"": ""array"",
                        ""items"": {{
                            ""type"": ""object"",
                            ""properties"": {{
                                ""TaxCategory"": {{ ""type"": ""string"", ""enum"": [""Normal"", ""Intermediate"", ""Reduced"", ""Zero""] }},
                                ""Base"": {{ ""type"": ""number"" }},
                                ""IVA"": {{ ""type"": ""number"" }},
                                ""Total"": {{ ""type"": ""number"" }}
                            }}
                        }}
                    }},
                    ""FilePath"": {{ ""type"": ""string"" }},
                    ""UploadTime"": {{ ""type"": ""string"", ""format"": ""date-time"" }},
                    ""Comments"": {{ ""type"": ""string"" }},
                    ""Status"": {{ ""type"": ""string"", ""enum"": [""New"", ""RawFile"", ""QRCodeProcessed"", ""TextProcessed"", ""ProductsMapped"", ""AttentionRequired"", ""SavedToSystem"", ""Error""] }}
                }}
            }}";
        
        private const string _mappedInvoiceSchema = $@"
            {{
                ""$schema"": ""http://json-schema.org/draft-07/schema#"",
                ""type"": ""object"",
                ""properties"": {{
                    ""Id"": {{ ""type"": [""integer"", ""null""] }},
                    ""InvoiceNumber"": {{ ""type"": ""string"" }},
                    ""InvoiceDate"": {{ ""type"": ""string"", ""format"": ""date-time"" }},
                    ""TotalIVA"": {{ ""type"": ""number"" }},
                    ""TotalAmount"": {{ ""type"": ""number"" }},
                    ""Consumer"": {{
                        ""type"": ""object"",
                        ""properties"": {{
                            ""Name"": {{ ""type"": ""string"" }},
                            ""TaxNumber"": {{ ""type"": ""string"" }}
                        }}
                    }},
                    ""Supplier"": {{
                        ""type"": ""object"",
                        ""properties"": {{
                            ""Name"": {{ ""type"": ""string"" }},
                            ""TaxNumber"": {{ ""type"": ""string"" }},
                            ""BankAccount"": {{ ""type"": ""string"" }}
                        }}
                    }},
                    ""Products"": {{
                        ""type"": ""array"",
                        ""items"": {{
                            ""type"": ""object"",
                            ""properties"": {{
                                ""Id"": {{ ""type"": [""integer"", ""null""] }},
                                ""ProductCode"": {{ ""type"": ""string"" }},
                                ""ProductName"": {{ ""type"": ""string"" }},
                                ""Unit"": {{ ""type"": ""string"" }},
                                ""Quantity"": {{ ""type"": ""number"" }},
                                ""Price"": {{ ""type"": ""number"" }},
                                ""TaxCategory"": {{ ""type"": ""string"", ""enum"": [""Normal"", ""Intermediate"", ""Reduced"", ""Zero""] }},
                                ""RMSProduct"": {{
                                    ""type"": ""object"",
                                    ""properties"": {{
                                        ""Id"": {{ ""type"": [""integer"", ""null""] }},
                                        ""ConsumerId"": {{ ""type"": ""integer"" }},
                                        ""ConsumerTaxId"": {{ ""type"": ""string"" }},
                                        ""Name"": {{ ""type"": ""string"" }},
                                        ""Description"": {{ ""type"": ""string"" }},
                                        ""RMSProductExtGuid"": {{ ""type"": ""string"", ""format"": ""uuid"" }},
                                        ""Num"": {{ ""type"": ""string"" }},
                                        ""MainUnit"": {{ ""type"": ""string"", ""format"": ""uuid"" }},
                                        ""Containers"": {{
                                            ""type"": ""array"",
                                            ""items"": {{
                                                ""type"": ""object"",
                                                ""properties"": {{
                                                    ""Id"": {{ ""type"": [""integer"", ""null""] }},
                                                    ""Num"": {{ ""type"": ""string"" }},
                                                    ""Name"": {{ ""type"": ""string"" }},
                                                    ""RMSContainerExtGuid"": {{ ""type"": ""string"", ""format"": ""uuid"" }},
                                                    ""Count"": {{ ""type"": ""number"" }},
                                                    ""ContainerWeight"": {{ ""type"": ""number"" }},
                                                    ""FullContainerWeight"": {{ ""type"": ""number"" }}
                                                }}
                                            }}
                                        }},
                                        ""Status"": {{ ""type"": ""string"", ""enum"": [""FromRMS"", ""NewProduct"", ""NewContainer""] }}
                                    }}
                                }},
                                ""RMSContainer"": {{
                                    ""type"": ""object"",
                                    ""properties"": {{
                                        ""Id"": {{ ""type"": [""integer"", ""null""] }},
                                        ""Num"": {{ ""type"": ""string"" }},
                                        ""Name"": {{ ""type"": ""string"" }},
                                        ""RMSContainerExtGuid"": {{ ""type"": ""string"", ""format"": ""uuid"" }},
                                        ""Count"": {{ ""type"": ""number"" }},
                                        ""ContainerWeight"": {{ ""type"": ""number"" }},
                                        ""FullContainerWeight"": {{ ""type"": ""number"" }}
                                    }}
                                }},
                                ""Comments"": {{ ""type"": ""string"" }}
                            }}
                        }}
                    }},
                    ""TaxCategories"": {{
                        ""type"": ""array"",
                        ""items"": {{
                            ""type"": ""object"",
                            ""properties"": {{
                                ""TaxCategory"": {{ ""type"": ""string"", ""enum"": [""Normal"", ""Intermediate"", ""Reduced"", ""Zero""] }},
                                ""Base"": {{ ""type"": ""number"" }},
                                ""IVA"": {{ ""type"": ""number"" }},
                                ""Total"": {{ ""type"": ""number"" }}
                            }}
                        }}
                    }},
                    ""FilePath"": {{ ""type"": ""string"" }},
                    ""UploadTime"": {{ ""type"": ""string"", ""format"": ""date-time"" }},
                    ""Comments"": {{ ""type"": ""string"" }},
                    ""Status"": {{ ""type"": ""string"", ""enum"": [""New"", ""RawFile"", ""QRCodeProcessed"", ""TextProcessed"", ""ProductsMapped"", ""AttentionRequired"", ""SavedToSystem"", ""Error""] }}
                }}
            }}";


        private const string _mappedProductsSchema = $@"
          {{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object"",
            ""properties"": 
            {{
                ""MatchedInvoiceProducts"": 
                {{
                    ""type"": ""array"",
                    ""items"": {{
                        ""type"": ""object"",
                        ""properties"": {{
                            ""InvoiceProduct"": {{
                                ""type"": ""object"",
                                ""description"": ""Invoice product"",
                                ""properties"": {{
                                    ""Id"": {{ ""type"": ""integer"", ""description"": ""ID of the invoice product"" }},
                                    ""ProductCode"": {{ ""type"": ""string"", ""description"": ""Product code from the supplier"" }},
                                    ""ProductName"": {{ ""type"": ""string"", ""description"": ""Product name from the supplier"" }},
                                    ""Unit"": {{ ""type"": ""string"", ""description"": ""Unit of measurement"" }}
                                }}
                            }},
                            ""RMSProduct"": {{
                                ""type"": ""object"",
                                ""description"": ""A matched (if found) or new product from the restaurant system"",
                                ""properties"": {{
                                    ""Id"": {{ ""type"": [""integer"",""null""], ""description"": ""ID of the RMS product"" }},
                                    ""Name"": {{ ""type"": ""string"", ""description"": ""The name of the product in the restaurant system"" }},
                                    ""Description"": {{ ""type"": ""string"", ""description"": ""The description of the product in the restaurant system"" }},
                                    ""Num"": {{ ""type"": [""string"",""null""], ""description"": ""Product item in the restaurant system"" }},
                                    ""Unit"": {{ ""type"": ""string"", ""description"": ""Unit of measurement"" }},
                                    ""Containers"": {{
                                        ""type"": ""array"",
                                        ""description"": ""List of containers for the RMS product"",
                                        ""items"": {{
                                            ""type"": ""object"",
                                            ""properties"": {{
                                                ""Id"": {{ ""type"": [""integer"",""null""], ""description"": ""Container ID"" }},
                                                ""Num"": {{ ""type"": [""string"",""null""], ""description"": ""Container item in the restaurant system"" }},
                                                ""Name"": {{ ""type"": ""string"", ""description"": ""Container name in the restaurant system"" }},
                                                ""Count"": {{ ""type"": ""number"", ""description"": ""Quantity, volume"" }}
                                            }}
                                        }}
                                    }}
                                }}
                            }},
                            ""RMSContainer"": {{
                                ""type"": ""object"",
                                ""description"": ""Mapping of the RMS container"",
                                ""properties"": {{
                                    ""Id"": {{ ""type"": [""integer"",""null""], ""description"": ""Container ID"" }},
                                    ""Num"": {{ ""type"": [""string"",""null""], ""description"": ""Container item in the restaurant system"" }},
                                    ""Name"": {{ ""type"": ""string"", ""description"": ""Container name in the restaurant system"" }},
                                    ""Count"": {{ ""type"": ""number"", ""description"": ""Quantity, volume"" }}
                                }}
                            }},
                            ""NewRMSProduct"": {{ ""type"": ""boolean"", ""description"": ""Whether a new product has been created"" }},
                            ""NewRMSContainer"": {{ ""type"": ""boolean"", ""description"": ""Whether a new container has been created"" }},
                            ""Storage"": {{ ""type"": ""string"", ""description"": ""The warehouse where the product should go"" }},

                            ""Comments"": {{ ""type"": ""string"", ""description"": ""Comments about the mapping"" }}
                        }},
                        ""required"": [""InvoiceProduct"", ""RMSProduct"", ""RMSContainer"", ""NewRMSProduct"", ""NewRMSContainer"", ""Storage"", ""Comments""],
                        ""additionalProperties"": false
                    }}
                }}
            }}
        }}";

    }
}
