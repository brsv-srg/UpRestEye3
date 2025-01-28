using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UpRestEye3.Controllers;
using System;
using System.Linq;
using UpRestEye3.Components.Pages;
using UpRestEye3.Models.DTO;
using UpRestEye3.Services.DataLayer;
using UpRestEye3.Services.BusinessLogic ;
using Google.Protobuf.WellKnownTypes;
using UpRestEye3.Models.BLO;

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
                                    new TaxesDTO { Category = InvoiceHelper.GetTaxCategory("13%"), Base = 2.00m, IVA = 2.00m*0.13m, Total = 2.00m + 2.00m*0.13m }
                                },
                                Products = new List<ProductDTO>
                                {
                                    new ProductDTO
                                    {
                                        ProductCode = "EXT-001",
                                        ProductName = "External Product 1",
                                        Unit = "pcs",
                                        Quantity = 10,
                                        Price = 100.00m
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
                                    new TaxesDTO { Category = InvoiceHelper.GetTaxCategory("23%"), Base = 4.00m, IVA = 4.00m*0.23m, Total = 4.00m + 4.00m*0.23m }
                                },
                                Products = new List<ProductDTO>
                                {
                                    new ProductDTO
                                    {
                                        ProductCode = "EXT-002",
                                        ProductName = "External Product 2",
                                        Unit = "pcs",
                                        Quantity = 10,
                                        Price = 100.00m
                                    },
                                    new ProductDTO
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
                                RMSProductId = Guid.NewGuid(),
                                Name = "Product A",
                                Description = "Product A Description",
                                Num = "001",
                                TaxCategory = Guid.NewGuid(),
                                Category = Guid.NewGuid(),
                                AccountingCategory = Guid.NewGuid(),
                                MainUnit = Guid.NewGuid(),
                                Type = ItemType.GOODS,
                                UnitWeight = 1.00m,
                                UnitCapacity = 1.00m,
                                NotInStoreMovement = false,
                                Containers = new List<ContainerDTO>
                                {
                                    new ContainerDTO
                                    {
                                        RMSContainerId = Guid.NewGuid(),
                                        Num = "001",
                                        Name = "Container 1",
                                        Count = 1,
                                        MinContainerWeight = 1.00m,
                                        MaxContainerWeight = 1.00m,
                                        ContainerWeight = 1.00m,
                                        FullContainerWeight = 1.00m,
                                        BackwardRecalculation = false,
                                        UseInFront = false,
                                        Deleted = false
                                    }
                                }
                            },
                            new RMSProductDTO
                            {
                                ConsumerId = 1,
                                ConsumerTaxId = "987654322",
                                RMSProductId = Guid.NewGuid(),
                                Name = "Product B",
                                Description = "Product A Description",
                                Num = "002",
                                TaxCategory = Guid.NewGuid(),
                                Category = Guid.NewGuid(),
                                AccountingCategory = Guid.NewGuid(),
                                MainUnit = Guid.NewGuid(),
                                Type = ItemType.GOODS,
                                UnitWeight = 1.00m,
                                UnitCapacity = 1.00m,
                                NotInStoreMovement = false,
                                Containers = new List<ContainerDTO>
                                {
                                    new ContainerDTO
                                    {
                                        RMSContainerId = Guid.NewGuid(),
                                        Num = "001",
                                        Name = "Container 1",
                                        Count = 1,
                                        MinContainerWeight = 1.00m,
                                        MaxContainerWeight = 1.00m,
                                        ContainerWeight = 1.00m,
                                        FullContainerWeight = 1.00m,
                                        BackwardRecalculation = false,
                                        UseInFront = false,
                                        Deleted = false
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
