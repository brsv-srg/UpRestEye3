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

            TotalAmountInclTaxes = decimal.Parse(qrcode.TotalAmount);
            TotalAmountExclTaxes = decimal.Parse(qrcode.NetAmount);
            TaxCategories.Add(new TaxCategory { Id=1, Category = "13%", Amount = decimal.Parse(qrcode.I6) });
            TaxCategories.Add(new TaxCategory { Id=2, Category = "23%", Amount = decimal.Parse(qrcode.N) });

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
