using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpRestEye3.Models;
using UpRestEye3.Controllers;
using UpRestEye3.Services;
using System;
using System.Linq;
using UpRestEye3.Components.Pages;

namespace UpRestEye3.Data
{
    public class SeedData
    {
        private readonly IInvoiceService _invoiceService;

        public SeedData(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        public void Initialize(IServiceProvider serviceProvider)
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
                                Supplier = new Invoice.SupplierInfo
                                {
                                    Name = "Supplier A",
                                    TaxNumber = "123456789",
                                    BankAccount = "DE12345678901234567890"
                                },
                                Info = new Invoice.InvoiceInfo
                                {
                                    InvoiceNumber = "INV-001",
                                    InvoiceDate = DateTime.Now,
                                    TotalAmountInclTaxes = 1200.00m,
                                    TotalAmountExclTaxes = 1000.00m
                                },
                                TaxCategories = new List<TaxCategory>
                                {
                                    new TaxCategory { Category = "13%", Amount = 2.00m }
                                },
                                Products = new List<Product>
                                {
                                    new Product
                                    {
                                        ProductCode = "EXT-001",
                                        ProductName = "External Product 1",
                                        Unit = "pcs",
                                        Quantity = 10,
                                        Price = 100.00m
                                    }
                                }
                            },
                            new Invoice
                            {
                                Supplier = new Invoice.SupplierInfo
                                {
                                    Name = "Supplier B",
                                    TaxNumber = "987654321",
                                    BankAccount = null
                                },
                                Info = new Invoice.InvoiceInfo
                                {
                                    InvoiceNumber = "INV-002",
                                    InvoiceDate = DateTime.Now.AddDays(-1),
                                    TotalAmountInclTaxes = 2400.00m,
                                    TotalAmountExclTaxes = 2000.00m
                                },
                                TaxCategories = new List<TaxCategory>
                                {
                                    new TaxCategory { Category = "23%", Amount = 4.00m }
                                },
                                Products = new List<Product>
                                {
                                    new Product
                                    {
                                        ProductCode = "EXT-002",
                                        ProductName = "External Product 2",
                                        Unit = "pcs",
                                        Quantity = 10,
                                        Price = 100.00m
                                    },
                                    new Product
                                    {
                                        ProductCode = "EXT-003",
                                        ProductName = "External Product 3",
                                        Unit = "pcs",
                                        Quantity = 14,
                                        Price = 104.00m
                                    },
                                }
                            }
                        };

                _invoiceService.SaveInvoicesAsync(invoices);
            }
        }
    }
}
