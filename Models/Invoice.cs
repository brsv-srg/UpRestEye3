using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using ZXing.QrCode.Internal;

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
