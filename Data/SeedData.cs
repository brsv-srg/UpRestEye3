using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpRestEye3.Models;
using UpRestEye3.Controllers;
using UpRestEye3.Services;
using System;
using System.Linq;

namespace UpRestEye3.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();


                if (context.Invoices.Any())
                {
                    return;   // DB has been seeded
                }

                var invoices = new List<Invoice>
                    {
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
                                new TaxCategory { Category = "13%", Amount = 2.00m }
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
                                new TaxCategory { Category = "23%", Amount = 4.00m }
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
                    };

                var invoicesController = services.GetRequiredService<InvoiceService>();
                //invoicesController.SaveInvoices(invoices).Wait();
            }
        }
    }
}
