using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using ZXing.QrCode.Internal;
using Google.Api;
using System.Text.Json;
using System.Text;
using static Google.Cloud.Vision.V1.TextAnnotation.Types;


namespace UpRestEye3.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public SupplierInfo Supplier { get; set; } = new SupplierInfo();
        public ConsumerInfo Consumer { get; set; } = new ConsumerInfo();
        public InvoiceInfo Info { get; set; } = new InvoiceInfo();
        public List<Product> Products { get; set; } = new();
        public List<TaxCategory> TaxCategories { get; set; } = new();

        public string? FilePath { get; set; }
        public DateTime UploadTime { get; set; } = DateTime.Now;

        public string? Comments { get; set; }
        public string? Status { get; set; }

        // Default constructor
        public Invoice()
        {
        }

        // Constructor from QRCodeData
        public Invoice(QRCodeData qrcode)
        {
            Supplier.Name = qrcode.SupplierTaxNumber;
            Supplier.TaxNumber = qrcode.SupplierTaxNumber;

            Info.InvoiceNumber = qrcode.DocNumber;
            Info.InvoiceDate = DateTime.ParseExact(qrcode.DocDate, "yyyyMMdd", null);
            Info.TotalAmountInclTaxes = decimal.Parse(qrcode.TotalAmount != "" ? qrcode.TotalAmount : "0.0");
            Info.TotalAmountExclTaxes = decimal.Parse(qrcode.NetAmount != "" ? qrcode.NetAmount : "0.0");

            TaxCategories.Add(new TaxCategory { Category = "13%", Amount = decimal.Parse(qrcode.I6 != "" ? qrcode.I6 : "0.0") });
            TaxCategories.Add(new TaxCategory { Category = "23%", Amount = decimal.Parse(qrcode.N != "" ? qrcode.N : "0.0") });
        }


        public class SupplierInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string TaxNumber { get; set; } = string.Empty;
            public string? BankAccount { get; set; } = string.Empty;
        }
        public class ConsumerInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string TaxNumber { get; set; } = string.Empty;
        }

        [Owned]
        public class InvoiceInfo
        {
            public string InvoiceNumber { get; set; } = string.Empty;
            public DateTime InvoiceDate { get; set; }
            public decimal TotalAmountInclTaxes { get; set; }
            public decimal TotalAmountExclTaxes { get; set; }
        }

        public Invoice(JsonDocument jsonDocument)
        {
            var root = jsonDocument.RootElement;

            try
            {
                Supplier.Name = root.GetProperty("Supplier").GetProperty("Name").GetString() ?? string.Empty;
                Supplier.TaxNumber = root.GetProperty("Supplier").GetProperty("TaxNumber").GetString() ?? string.Empty;
                Supplier.BankAccount = root.GetProperty("Supplier").GetProperty("BankAccount").GetString();

                Consumer.Name = root.GetProperty("Consumer").GetProperty("Name").GetString() ?? string.Empty;
                Consumer.TaxNumber = root.GetProperty("Consumer").GetProperty("TaxNumber").GetString() ?? string.Empty;

                Info.InvoiceNumber = root.GetProperty("Info").GetProperty("InvoiceNumber").GetString() ?? string.Empty;

                Info.InvoiceDate = root.GetProperty("Info").GetProperty("InvoiceDate").GetDateTime();

                Info.TotalAmountInclTaxes = root.GetProperty("Info").GetProperty("TotalAmountInclTaxes").GetDecimal();
                Info.TotalAmountExclTaxes = root.GetProperty("Info").GetProperty("TotalAmountExclTaxes").GetDecimal();

                FilePath = root.GetProperty("FilePath").GetString();
                UploadTime = root.GetProperty("UploadTime").GetDateTime();
                Comments = root.GetProperty("Comments").GetString();
                Status = root.GetProperty("Status").GetString();


                var taxCategories = root.GetProperty("TaxCategories").EnumerateArray();
                foreach (var taxCategory in taxCategories)
                {
                    TaxCategories.Add(new TaxCategory
                    {
                        Category = taxCategory.GetProperty("Category").GetString() ?? string.Empty,
                        Amount = decimal.Parse(taxCategory.GetProperty("Amount").GetString() ?? "0.0")
                    });
                }

                var products = root.GetProperty("Products").EnumerateArray();
                foreach (var product in products)
                {
                    Products.Add(new Product
                    {
                        ProductCode = product.GetProperty("ProductCode").GetString() ?? string.Empty,
                        ProductName = product.GetProperty("ProductName").GetString() ?? string.Empty,
                        Unit = product.GetProperty("Unit").GetString() ?? string.Empty,
                        Quantity = (float)product.GetProperty("Quantity").GetDecimal(),
                        Price = product.GetProperty("Price").GetDecimal()
                    });
                }
            }
            catch (Exception ex)
            {
                // Handle or log the error as needed
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
            }
        }
    }

    public class TaxCategory
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
