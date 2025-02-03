using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Models.DTO;



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

    public class RMSProductStateJsonConverter : JsonConverter<RMSProductStatus>
    {
        public override RMSProductStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();

                if (stringValue.Contains("New"))
                {
                    return RMSProductStatus.New;
                }
                else if (stringValue.Contains("RMS"))
                {
                    return RMSProductStatus.FromRMS;
                }
            }
            throw new JsonException("Invalid token type for TaxCategory.");
        }

        public override void Write(Utf8JsonWriter writer, RMSProductStatus value, JsonSerializerOptions options)
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

        public static string GetRMSProductsSchema()
        {
            return _rmsProductsSchema;
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

                InvoiceDTO invoice = new InvoiceDTO();

                var options = new JsonSerializerOptions
                {
                    Converters = { new DecimalJsonConverter(),
                                    new IntegerJsonConverter(),
                                    new DateTimeJsonConverter(),
                                    new TaxCategoryJsonConverter(),
                                    new RMSProductStateJsonConverter()},
                    WriteIndented = true
                };
                string invoiceJsonString = JsonSerializer.Serialize(invoice, options);
                var invoiceJsonDoc = JsonDocument.Parse(invoiceJsonString);


                var invoiceStr = invoiceJsonDoc.Deserialize<InvoiceDTO>();
                var context = new ValidationContext(invoice, serviceProvider: null, items: null);
                Validator.ValidateObject(invoice, context, validateAllProperties: true);
                Console.WriteLine("Object is valid against the schema.");
            }
            catch (ValidationException ex)
            {
                throw new Exception("Object validation against schema failed: " + ex.Message);
            }
        }



        public static void ValidateRMSProductsSchema()
        {
            // Validate schema version
            if (!_rmsProductsSchema.Contains(@"""$schema"": ""http://json-schema.org/draft-07/schema#"""))
            {
                throw new Exception("Schema is not compatible with Draft-07.");
            }

            // Validate schema structure
            try
            {
                var schema = JsonDocument.Parse(_rmsProductsSchema);
                Console.WriteLine("Schema is valid and compatible with Draft-07.");
            }
            catch (JsonException ex)
            {
                throw new Exception("Schema validation failed: " + ex.Message);
            }
        }

        public static void ValidateRMSProductsObject()
        {
            try
            {
                List<RMSProductDTO> rmsProducts = new List<RMSProductDTO>();
                rmsProducts.Add(new RMSProductDTO());

                var options = new JsonSerializerOptions
                {
                    Converters = { new DecimalJsonConverter(),
                                    new IntegerJsonConverter(),
                                    new DateTimeJsonConverter(),
                                    new TaxCategoryJsonConverter(),
                                    new RMSProductStateJsonConverter()},
                    WriteIndented = true
                };
                string rmsProductJsonString = JsonSerializer.Serialize(rmsProducts, options);
                var rmsProductsJsonDoc = JsonDocument.Parse(rmsProductJsonString);

                var rmsProductsStr = rmsProductsJsonDoc.Deserialize<List<RMSProductDTO>>(options);
                var context = new ValidationContext(rmsProducts, serviceProvider: null, items: null);
                Validator.ValidateObject(rmsProducts, context, validateAllProperties: true);
                Console.WriteLine("Object is valid against the schema.");
            }
            catch (ValidationException ex)
            {
                throw new Exception("Object validation against schema failed: " + ex.Message);
            }
        }





        private const string _invoiceSchema = $@"
{{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
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
          ""type"": ""string""
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
          }},
          ""Category"": {{
            ""type"": ""string"",
            ""enum"": [""Normal"", ""Intermediate"", ""Reduced"", ""Zero""]
          }},
          ""RMSProductId"": {{
            ""type"": [""integer"", ""null""]
          }},
          ""RMSProductName"": {{
            ""type"": [""string"", ""null""]
          }},
          ""RMSContainerId"": {{
            ""type"": [""integer"", ""null""]
          }},
          ""RMSContainerName"": {{
            ""type"": [""string"", ""null""]
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
            ""enum"": [""Normal"", ""Intermediate"", ""Reduced"", ""Zero""]
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
    ""FilePath"": {{
      ""type"": ""string""
    }},
    ""UploadTime"": {{
      ""type"": ""string"",
      ""format"": ""date-time""
    }},
    ""Comments"": {{
      ""type"": ""string""
    }},
    ""Status"": {{
      ""type"": ""string"",
      ""enum"": [""New"", ""RawFile"", ""QRCodeProcessed"", ""TextProcessed"", ""ProductsMapped"", ""SavedToSystem"", ""Error""]
    }}
  }}
}}
";





        private const string _rmsProductsSchema = $@"
{{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""type"": ""array"",
  ""items"": {{
    ""type"": ""object"",
    ""properties"": {{
      ""Id"": {{
        ""type"": [""integer"", ""null""]
      }},
      ""ConsumerId"": {{
        ""type"": ""integer""
      }},
      ""ConsumerTaxId"": {{
        ""type"": ""string""
      }},
      ""RMSProductId"": {{
        ""type"": ""string"",
        ""format"": ""uuid""
      }},
      ""Deleted"": {{
        ""type"": ""boolean""
      }},
      ""Name"": {{
        ""type"": ""string""
      }},
      ""Description"": {{
        ""type"": ""string""
      }},
      ""Num"": {{
        ""type"": ""string""
      }},
      ""Parent"": {{
        ""type"": [""string"", ""null""],
        ""format"": ""uuid""
      }},
      ""TaxCategory"": {{
        ""type"": ""string"",
        ""format"": ""uuid""
      }},
      ""Category"": {{
        ""type"": ""string"",
        ""format"": ""uuid""
      }},
      ""AccountingCategory"": {{
        ""type"": ""string"",
        ""format"": ""uuid""
      }},
      ""MainUnit"": {{
        ""type"": ""string"",
        ""format"": ""uuid""
      }},
      ""Type"": {{
        ""type"": ""string"",
        ""enum"": [""GOODS"", ""DISH"", ""PREPARED"", ""SERVICE"", ""MODIFIER"", ""OUTER"", ""RATE""]
      }},
      ""UnitWeight"": {{
        ""type"": ""number""
      }},
      ""UnitCapacity"": {{
        ""type"": ""number""
      }},
      ""NotInStoreMovement"": {{
        ""type"": ""boolean""
      }},
      ""Containers"": {{
        ""type"": ""array"",
        ""items"": {{
          ""type"": ""object"",
          ""properties"": {{
            ""Id"": {{
              ""type"": [""integer"", ""null""]
            }},
            ""RMSContainerId"": {{
              ""type"": ""string"",
              ""format"": ""uuid""
            }},
            ""Num"": {{
              ""type"": ""string""
            }},
            ""Name"": {{
              ""type"": ""string""
            }},
            ""Count"": {{
              ""type"": ""number""
            }},
            ""MinContainerWeight"": {{
              ""type"": ""number""
            }},
            ""MaxContainerWeight"": {{
              ""type"": ""number""
            }},
            ""ContainerWeight"": {{
              ""type"": ""number""
            }},
            ""FullContainerWeight"": {{
              ""type"": ""number""
            }},
            ""BackwardRecalculation"": {{
              ""type"": ""boolean""
            }},
            ""UseInFront"": {{
              ""type"": ""boolean""
            }},
            ""Deleted"": {{
              ""type"": ""boolean""
            }}
          }},
          ""required"": [""RMSContainerId"", ""Num"", ""Name"", ""Count"", ""MinContainerWeight"", ""MaxContainerWeight"", ""ContainerWeight"", ""FullContainerWeight"", ""BackwardRecalculation"", ""UseInFront"", ""Deleted""]
        }}
      }}
    }},
    ""required"": [""ConsumerId"", ""ConsumerTaxId"", ""RMSProductId"", ""Deleted"", ""Name"", ""Description"", ""Num"", ""TaxCategory"", ""Category"", ""AccountingCategory"", ""MainUnit"", ""Type"", ""UnitWeight"", ""UnitCapacity"", ""NotInStoreMovement"", ""Containers""]
  }}
}}
";

    }
}
