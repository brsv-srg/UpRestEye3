using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpRestEye3.Controllers;
using System;
using System.Linq;
using UpRestEye3.Components.Pages;
using UpRestEye3.Models.DTO;
using Google.Protobuf.WellKnownTypes;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.BusinessLogic;
using UpRestEye3.Services.DataLayer;

namespace UpRestEye3.Data
{
    public class SeedData
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IRMSProductService _productService;

        public SeedData(IInvoiceService invoiceService, IRMSProductService productService)
        {
            _invoiceService = invoiceService;
            _productService = productService;
        }

        public void InitializeInvoices(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();

                if (context.Invoices.Any())
                {
                    return;   // DB has been seeded
                }

                var invoices = new List<InvoiceDTO>
                        {
                            new InvoiceDTO
                            {
                                Consumer = new ConsumerDTO
                                {
                                    Name = "Consumer A",
                                    TaxNumber = "987654322"
                                },
                                Supplier = new SupplierDTO
                                {
                                    Name = "Supplier A",
                                    TaxNumber = "123456789",
                                    BankAccount = "DE12345678901234567890"
                                },
                                InvoiceNumber = "INV-001",
                                InvoiceDate = DateTime.Now,
                                TotalIVA = 2.00m*0.13m,
                                TotalAmount = 2.00m + 2.00m*0.13m,
                                TaxCategories = new List<TaxesDTO>
                                {
                                    new TaxesDTO { TaxCategory = InvoiceHelper.GetTaxCategory("13%"), Base = 2.00m, IVA = 2.00m*0.13m, Total = 2.00m + 2.00m*0.13m }
                                },
                                Products = new List<InvoiceProductDTO>
                                {
                                    new InvoiceProductDTO
                                    {
                                        ProductCode = "EXT-001",
                                        ProductName = "External Product 1",
                                        Unit = "pcs",
                                        Container = "Container 1",
                                        UnitsCountInContainer = 1.00m,
                                        QuantityOfContainers = 10,
                                        ProductTotalValue = 100.00m
                                    }
                                }
                            },
                            new InvoiceDTO
                            {
                                Consumer = new ConsumerDTO
                                {
                                    Name = "Consumer A",
                                    TaxNumber = "987654322"
                                },
                                Supplier = new SupplierDTO
                                {
                                    Name = "Supplier B",
                                    TaxNumber = "987654321",
                                    BankAccount = ""
                                },
                                InvoiceNumber = "INV-002",
                                InvoiceDate =  DateTime.Now,
                                TotalIVA = 4.00m*0.23m,
                                TotalAmount = 4.00m + 4.00m*0.23m,
                                TaxCategories = new List<TaxesDTO>
                                {
                                    new TaxesDTO { TaxCategory = InvoiceHelper.GetTaxCategory("23%"), Base = 4.00m, IVA = 4.00m*0.23m, Total = 4.00m + 4.00m*0.23m }
                                },
                                Products = new List<InvoiceProductDTO>
                                {
                                    new InvoiceProductDTO
                                    {
                                        ProductCode = "EXT-002",
                                        ProductName = "External Product 2",
                                        Unit = "pcs",
                                        Container = "Container 1",
                                        UnitsCountInContainer = 1.00m,
                                        QuantityOfContainers = 10,
                                        ProductTotalValue = 100.00m
                                    },
                                    new InvoiceProductDTO
                                    {
                                        ProductCode = "EXT-003",
                                        ProductName = "External Product 3",
                                        Unit = "pcs",
                                        Container = "Container 1",
                                        UnitsCountInContainer = 1.00m,
                                        QuantityOfContainers = 10,
                                        ProductTotalValue = 104.00m
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
        public void InitializeRMSProducts(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<ApplicationDbContext>();

                if (context.RMSProducts.Any())
                {
                    return;   // DB has been seeded
                }

                var products = new List<RMSProductDTO>
                        {
                            new RMSProductDTO
                            {
                                ConsumerId = 1,
                                ConsumerTaxId = "987654322",
                                RMSProductExtGuid = Guid.NewGuid(),
                                Name = "Product A",
                                Description = "Product A Description",
                                Num = "001",
                                MainUnit = Guid.NewGuid(),
                                Containers = new List<RMSContainerDTO>
                                {
                                    new RMSContainerDTO
                                    {
                                        RMSContainerExtGuid = Guid.NewGuid(),
                                        Num = "001",
                                        Name = "Container 1",
                                        Count = 1,
                                        ContainerWeight = 1.00m,
                                        FullContainerWeight = 1.00m
                                    }
                                }
                            },
                            new RMSProductDTO
                            {
                                ConsumerId = 1,
                                ConsumerTaxId = "987654322",
                                RMSProductExtGuid = Guid.NewGuid(),
                                Name = "Product B",
                                Description = "Product A Description",
                                Num = "002",
                                MainUnit = Guid.NewGuid(),
                                Containers = new List<RMSContainerDTO>
                                {
                                    new RMSContainerDTO
                                    {
                                        RMSContainerExtGuid = Guid.NewGuid(),
                                        Num = "001",
                                        Name = "Container 1",
                                        Count = 1,
                                        ContainerWeight = 1.00m,
                                        FullContainerWeight = 1.00m
                                    }
                                }
                            }
                        };

                foreach (var product in products)
                {
                    _productService.SaveProductAsync(product);
                }
            }

        }
    }
}
