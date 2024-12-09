using System;
using System.Collections.Generic;
using ZXing.QrCode.Internal;

namespace UpRestEye3.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierTaxNumber { get; set; } = string.Empty;
        public string? SupplierBankAccount { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmountInclTaxes { get; set; }
        public decimal TotalAmountExclTaxes { get; set; }
        public List<TaxCategory> TaxCategories { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public string ?FilePath { get; set; }
        public DateTime UploadTime { get; set; } = DateTime.Now;

        // Default constructor
        public Invoice()
        {
        }

        // Constructor from QRCodeData
        public Invoice(QRCodeData qrcode)
        {
            SupplierName = qrcode.SupplierTaxNumber;
            SupplierTaxNumber = qrcode.SupplierTaxNumber;
            InvoiceNumber = qrcode.DocNumber;
            InvoiceDate = DateTime.ParseExact(qrcode.DocDate, "yyyyMMdd", null);

            TotalAmountInclTaxes = decimal.Parse(qrcode.TotalAmount != "" ? qrcode.TotalAmount : "0.0");
            TotalAmountExclTaxes = decimal.Parse(qrcode.NetAmount != "" ? qrcode.NetAmount : "0.0");
            TaxCategories.Add(new TaxCategory { Category = "13%", Amount = decimal.Parse(qrcode.I6 != "" ? qrcode.I6 : "0.0") });
            TaxCategories.Add(new TaxCategory { Category = "23%", Amount = decimal.Parse(qrcode.N != "" ? qrcode.N : "0.0") });

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
        public string ExternalProductCode { get; set; } = string.Empty;
        public string ExternalProductName { get; set; } = string.Empty;
        public string InternalProductCode { get; set; } = string.Empty;
        public string InternalProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal ExternalPriceInclTaxes { get; set; }
        public string ExternalTaxCategory { get; set; } = string.Empty;
        public decimal ExternalTaxAmount { get; set; }
        public decimal ExternalPriceExclTaxes { get; set; }
        public decimal InternalPriceInclTaxes { get; set; }
        public string InternalTaxCategory { get; set; } = string.Empty;
        public decimal InternalTaxAmount { get; set; }
        public decimal InternalPriceExclTaxes { get; set; }
    }
}
