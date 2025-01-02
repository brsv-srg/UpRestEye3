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
using OpenCvSharp;
using Tensorflow;


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
        public List<Taxes> TaxCategories { get; set; } = [];

        public string? FilePath { get; set; }
        public DateTime UploadTime { get; set; } = DateTime.Now;

        public string? Comments { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.New;

        // Default constructor
        public Invoice()
        {
        }

        // Constructor from QRCodeData
        public Invoice(QRCodeData? qrCode, string? filePath)
        {
            Update(qrCode, filePath);
        }
        public void Update(QRCodeData? qrCode, string? filePath)
        {
            if (qrCode != null)
            {
                Supplier.Name = qrCode.SupplierTaxNumber;
                Supplier.TaxNumber = qrCode.SupplierTaxNumber;

                Info.InvoiceNumber = qrCode.DocNumber;
                Info.InvoiceDate = qrCode.DocDate;
                Info.TotalIVA = qrCode.TotalIVA;
                Info.TotalAmount = qrCode.TotalAmount;


                TaxCategories.Add(new Taxes { Category = Taxes.GetCategory("0%"), Base = qrCode.Base0, IVA = qrCode.Base0, Total = qrCode.Base0 });
                TaxCategories.Add(new Taxes { Category = Taxes.GetCategory("6%"), Base = qrCode.Base6, IVA = qrCode.IVA6, Total = qrCode.Base6 + qrCode.IVA6 });
                TaxCategories.Add(new Taxes { Category = Taxes.GetCategory("13%"), Base = qrCode.Base13, IVA = qrCode.IVA13, Total = qrCode.Base13 + qrCode.IVA13 });
                TaxCategories.Add(new Taxes { Category = Taxes.GetCategory("23%"), Base = qrCode.Base23, IVA = qrCode.IVA23, Total = qrCode.Base23 + qrCode.IVA23 });
                Status = InvoiceStatus.QRCodeProcessed;
                FilePath = filePath;
            }
            else
            if (filePath != null)
            {
                Status = InvoiceStatus.RawFile;
                FilePath = filePath;    
            }
            else
            {
                Status = InvoiceStatus.Error;
                throw new ArgumentException("Both QRCodeData and filePath are null");
            }
        } 
        
        public void Update(Invoice invoiceNew)
        {
            if(Supplier.TaxNumber != invoiceNew.Supplier.TaxNumber &&
                Supplier.TaxNumber == invoiceNew.Consumer.TaxNumber)
            {
                Supplier.Name = invoiceNew.Consumer.Name;
                Supplier.Id = null;
            }
            else
            if (Supplier.TaxNumber == invoiceNew.Supplier.TaxNumber)
            {
                Supplier.Name = invoiceNew.Supplier.Name;
                Supplier.Id = null;
            }
            else
            if(Supplier.TaxNumber == null)
            {
                Supplier.Name = invoiceNew.Supplier.Name;
                Supplier.TaxNumber = invoiceNew.Supplier.TaxNumber;
                Supplier.Id = null;
            }

            Supplier.BankAccount = invoiceNew.Supplier.BankAccount;



            if (Consumer.TaxNumber != invoiceNew.Consumer.TaxNumber &&
               Consumer.TaxNumber == invoiceNew.Supplier.TaxNumber)
            {
                Consumer.Name = invoiceNew.Supplier.Name;
                Consumer.Id = null;
            }
            else
           if (Consumer.TaxNumber == invoiceNew.Consumer.TaxNumber)
            {
                Consumer.Name = invoiceNew.Consumer.Name;
                Consumer.Id = null;
            }
            else
           if (Consumer.TaxNumber == null)
            {
                Consumer.Name = invoiceNew.Consumer.Name;
                Consumer.TaxNumber = invoiceNew.Consumer.TaxNumber;
                Consumer.Id = null;
            }

            Info.InvoiceNumber = invoiceNew.Info.InvoiceNumber;
            Info.InvoiceDate = invoiceNew.Info.InvoiceDate;
            Info.TotalIVA = invoiceNew.Info.TotalIVA;
            Info.TotalAmount = invoiceNew.Info.TotalAmount;

            Products = invoiceNew.Products;
            TaxCategories = invoiceNew.TaxCategories;
            
            Status = invoiceNew.Status;
            FilePath = invoiceNew.FilePath;
            
        }

        [Owned]
        public class InvoiceInfo
        {
            public string InvoiceNumber { get; set; } = string.Empty;
            public DateOnly InvoiceDate { get; set; }
            public decimal TotalIVA { get; set; }
            public decimal TotalAmount { get; set; }
        }
    }
    public enum InvoiceStatus
    {
        New,
        RawFile,
        QRCodeProcessed,
        TextProcessed,
        ProductsMapped,
        SavedToSystem,
        Error
    }

    public enum TaxCategory
    {
        Normal,
        Intermediate,
        Reduced,
        Zero
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

    public class Taxes
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public TaxCategory Category { get; set; } = TaxCategory.Intermediate;
        public decimal Base { get; set; } = 0.0m;
        public decimal IVA { get; set; } = 0.0m;
        public decimal Total { get; set; } = 0.0m;

        public static TaxCategory GetCategory(string stringCategory)
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

    public class Product
    {
        [JsonIgnore]
        public int? Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float Quantity { get; set; } = 0.0f;
        public decimal Price { get; set; } = 0.0m;
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

