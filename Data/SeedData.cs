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
                                Supplier = new SupplierInfo
                                {
                                    Name = "Supplier A",
                                    TaxNumber = "123456789",
                                    BankAccount = "DE12345678901234567890"
                                },
                                Info = new Invoice.InvoiceInfo
                                {
                                    InvoiceNumber = "INV-001",
                                    InvoiceDate = DateTime.Now,
                                    TotalIVA = 2.00m*0.13m,
                                    TotalAmount = 2.00m + 2.00m*0.13m
                                },
                                TaxCategories = new List<Taxes>
                                {
                                    new Taxes { Category = InvoiceHelper.GetTaxCategory("13%"), Base = 2.00m, IVA = 2.00m*0.13m, Total = 2.00m + 2.00m*0.13m }
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
                                Supplier = new SupplierInfo
                                {
                                    Name = "Supplier B",
                                    TaxNumber = "987654321",
                                    BankAccount = null
                                },
                                Info = new Invoice.InvoiceInfo
                                {
                                    InvoiceNumber = "INV-002",
                                    InvoiceDate =  DateTime.Now,
                                    TotalIVA = 4.00m*0.23m,
                                    TotalAmount = 4.00m + 4.00m*0.23m
                                },
                                TaxCategories = new List<Taxes>
                                {
                                    new Taxes { Category = InvoiceHelper.GetTaxCategory("23%"), Base = 4.00m, IVA = 4.00m*0.23m, Total = 4.00m + 4.00m*0.23m }
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

                foreach (var invoice in invoices)
                {
                    _invoiceService.SaveInvoiceAsync(invoice);
                }
            }
        }
    }
}
