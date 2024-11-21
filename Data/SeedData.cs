using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EyeRestWAs.Models;
using System;
using System.Linq;

namespace EyeRestWAs.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                if (context.Invoices.Any())
                {
                    return;   // DB has been seeded
                }

                context.Invoices.AddRange(
                    new Invoice
                    {
                        SupplierName = "Supplier A",
                        SupplierTaxNumber = "123456789",
                        SupplierBankAccount = "DE12345678901234567890",
                        InvoiceNumber = "INV-001",
                        InvoiceDate = DateTime.Now,
                        TotalAmountInclTaxes = 1200.00m,
                        TotalAmountExclTaxes = 1000.00m,
                        TaxCategories = new List<TaxCategory>
                        {
                            new TaxCategory { Category = "VAT", Amount = 200.00m }
                        },
                        Products = new List<Product>
                        {
                            new Product
                            {
                                ExternalProductCode = "EXT-001",
                                ExternalProductName = "External Product 1",
                                InternalProductCode = "INT-001",
                                InternalProductName = "Internal Product 1",
                                Unit = "pcs",
                                Quantity = 10,
                                ExternalPriceInclTaxes = 120.00m,
                                ExternalTaxCategory = "VAT",
                                ExternalTaxAmount = 20.00m,
                                ExternalPriceExclTaxes = 100.00m,
                                InternalPriceInclTaxes = 120.00m,
                                InternalTaxCategory = "VAT",
                                InternalTaxAmount = 20.00m,
                                InternalPriceExclTaxes = 100.00m
                            }
                        }
                    },
                    new Invoice
                    {
                        SupplierName = "Supplier B",
                        SupplierTaxNumber = "987654321",
                        SupplierBankAccount = null,
                        InvoiceNumber = "INV-002",
                        InvoiceDate = DateTime.Now.AddDays(-1),
                        TotalAmountInclTaxes = 2400.00m,
                        TotalAmountExclTaxes = 2000.00m,
                        TaxCategories = new List<TaxCategory>
                        {
                            new TaxCategory { Category = "VAT", Amount = 400.00m }
                        },
                        Products = new List<Product>
                        {
                            new Product
                            {
                                ExternalProductCode = "EXT-002",
                                ExternalProductName = "External Product 2",
                                InternalProductCode = "INT-002",
                                InternalProductName = "Internal Product 2",
                                Unit = "pcs",
                                Quantity = 20,
                                ExternalPriceInclTaxes = 240.00m,
                                ExternalTaxCategory = "VAT",
                                ExternalTaxAmount = 40.00m,
                                ExternalPriceExclTaxes = 200.00m,
                                InternalPriceInclTaxes = 240.00m,
                                InternalTaxCategory = "VAT",
                                InternalTaxAmount = 40.00m,
                                InternalPriceExclTaxes = 200.00m
                            }
                        }
                    }
                );

                context.SaveChanges();
            }
        }
    }
}
