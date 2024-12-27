using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using ZXing.QrCode.Internal;
using Google.Api;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using static Google.Cloud.Vision.V1.TextAnnotation.Types;
using System.Globalization;


namespace UpRestEye3.Models
{
    public class Invoice
    {
        [JsonIgnore]
        public int? Id { get; set; }


        [JsonIgnore]
        public int? SupplierId { get; set; }
        public SupplierInfo Supplier { get; set; } = new SupplierInfo();


        [JsonIgnore]
        public int? ConsumerId { get; set; }
        public ConsumerInfo Consumer { get; set; } = new ConsumerInfo();

        public InvoiceInfo Info { get; set; } = new InvoiceInfo();
        public List<Product> Products { get; set; } = [];
        public List<TaxCategory> TaxCategories { get; set; } = [];

        public string? FilePath { get; set; }
        public DateTime UploadTime { get; set; } = DateTime.Now;

        public string? Comments { get; set; }
        public string? Status { get; set; }

        // Default constructor
        public Invoice()
        {
        }

        // Constructor from QRCodeData
        public Invoice(QRCodeData? qrCode, string? filePath)
        {
            if (qrCode != null)
            {
                Supplier.Name = qrCode.SupplierTaxNumber;
                Supplier.TaxNumber = qrCode.SupplierTaxNumber;

                Info.InvoiceNumber = qrCode.DocNumber;
                Info.InvoiceDate = DateTime.ParseExact(qrCode.DocDate, "yyyyMMdd", null);
                Info.TotalAmountInclTaxes = decimal.Parse(qrCode.TotalAmount != "" ? qrCode.TotalAmount : "0.0");
                Info.TotalAmountExclTaxes = decimal.Parse(qrCode.NetAmount != "" ? qrCode.NetAmount : "0.0");

                TaxCategories.Add(new TaxCategory { Category = "13%", Amount = decimal.Parse(qrCode.I6 != "" ? qrCode.I6 : "0.0") });
                TaxCategories.Add(new TaxCategory { Category = "23%", Amount = decimal.Parse(qrCode.N != "" ? qrCode.N : "0.0") });
            }

            FilePath = filePath;
        }
  

        [Owned]
        public class InvoiceInfo
        {
            public string InvoiceNumber { get; set; } = string.Empty;
            public DateTime InvoiceDate { get; set; }
            public decimal TotalAmountInclTaxes { get; set; }
            public decimal TotalAmountExclTaxes { get; set; }
        }
    }

    public class SupplierInfo
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string? BankAccount { get; set; } = string.Empty;
    }
    public class ConsumerInfo
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
    }

    public class TaxCategory
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class Product
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float Quantity { get; set; }
        public decimal Price { get; set; }
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
}
