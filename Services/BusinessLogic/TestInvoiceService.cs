using Microsoft.EntityFrameworkCore;
using UpRestEye3.Data;
using UpRestEye3.Models.DTO;
using UpRestEye3.Models.BLO;
using UpRestEye3.Services.DataLayer;

namespace UpRestEye3.Services.BusinessLogic
{
    public class TestInvoiceService
    {
        private readonly IServiceProvider _serviceProvider;

        public TestInvoiceService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task RunTests()
        {
            int? Id = await TestCreateInvoiceAsync();
            await TestUpdateInvoiceAsync(Id);
            await TestUpdateSupplierAsync(Id);
            await TestUpdateConsumerAsync(Id);
            await TestDeleteAsync(Id);
        }

        private async Task<int?> TestCreateInvoiceAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var invoiceService = services.GetRequiredService<IInvoiceService>();

                // Create new SupplierInfo
                var supplier = new SupplierDTO
                {
                    Name = "",
                    TaxNumber = "123456729",
                    BankAccount = "DE12345678901234567855"
                };

                // Create new ConsumerInfo
                var consumer = new ConsumerDTO
                {
                    Name = "Consumer A",
                    TaxNumber = "987654322"
                };

                // Create new Invoice
                var invoice = new InvoiceDTO
                {
                    Supplier = supplier,
                    Consumer = consumer,
                    InvoiceNumber = $"INV-003 {DateTime.Now}",
                    InvoiceDate = DateTime.Now,
                    TotalIVA = 2.00m * 0.13m,
                    TotalAmount = 2.00m + 2.00m * 0.13m,
                    TaxCategories = new List<TaxesDTO>
                    {
                        new TaxesDTO { TaxCategory = TaxCategoryEnum.Intermediate, Base = 2.00m, IVA = 2.00m * 0.13m, Total = 2.00m + 2.00m * 0.13m }
                    },
                    Products = new List<InvoiceProductDTO>
                    {
                        new InvoiceProductDTO
                        {
                            ProductCode = "EXT-002",
                            ProductName = "External Product 1",
                            Unit = "pcs",
                            Quantity = 2.00m,
                            Container = "box",
                            Count = 1.00m,
                            ProductTotalValue = 100.00m
                        }
                    }
                };

                // Save Invoice
                int? id = await invoiceService.SaveInvoiceAsync(invoice);

                Console.WriteLine($"Invoice Created: {invoice.InvoiceNumber}");
                return id;
            }
        }

        private async Task TestUpdateInvoiceAsync(int? Id)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var invoiceService = services.GetRequiredService<IInvoiceService>();
                var context = services.GetRequiredService<ApplicationDbContext>();

                // Retrieve existing Invoice
                var invoice = await context.Invoices
                    .Include(i => i.Supplier)
                    .Include(i => i.Consumer)
                    .Include(i => i.TaxCategories)
                    .Include(i => i.Products)
                    .FirstOrDefaultAsync(i => Id != null && Id == i.Id || Id == null && i.InvoiceNumber == "INV-003");

                if (invoice != null)
                {
                    // Update Invoice Info
                    invoice.TotalIVA = 3.00m * 0.13m;
                    invoice.TotalAmount = 3.00m + 3.00m * 0.13m;

                    // Save updated Invoice
                    await invoiceService.SaveInvoiceAsync(invoice);

                    Console.WriteLine($"Invoice Updated: {invoice.InvoiceNumber}");
                }
            }
        }

        private async Task TestUpdateSupplierAsync(int? Id)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var invoiceService = services.GetRequiredService<IInvoiceService>();
                var context = services.GetRequiredService<ApplicationDbContext>();

                // Retrieve existing Invoice
                var invoice = await context.Invoices
                    .Include(i => i.Supplier)
                    .Include(i => i.Consumer)
                    .Include(i => i.TaxCategories)
                    .Include(i => i.Products)
                    .FirstOrDefaultAsync(i => Id != null && Id == i.Id || Id == null && i.InvoiceNumber == "INV-003");

                if (invoice != null)
                {
                    // Update SupplierInfo
                    invoice.Supplier.Name = "Updated Supplier B";
                    invoice.Supplier.BankAccount = "DE09876543210987654321";

                    // Save updated Invoice
                    await invoiceService.SaveInvoiceAsync(invoice);

                    Console.WriteLine($"Supplier Updated: {invoice.Supplier.Name}");
                }
            }
        }

        private async Task TestUpdateConsumerAsync(int? Id)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var invoiceService = services.GetRequiredService<IInvoiceService>();
                var context = services.GetRequiredService<ApplicationDbContext>();

                // Retrieve existing Invoice
                var invoice = await context.Invoices
                    .Include(i => i.Supplier)
                    .Include(i => i.Consumer)
                    .Include(i => i.TaxCategories)
                    .Include(i => i.Products)
                    .FirstOrDefaultAsync(i => Id != null && Id == i.Id || Id == null && i.InvoiceNumber == "INV-003");

                if (invoice != null)
                {
                    // Update ConsumerInfo
                    invoice.Consumer.Name = "Updated Consumer A";

                    // Save updated Invoice
                    await invoiceService.SaveInvoiceAsync(invoice);

                    Console.WriteLine($"Consumer Updated: {invoice.Consumer.Name}");
                }
            }
        }
   
        private async Task TestDeleteAsync(int? Id)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var invoiceService = services.GetRequiredService<IInvoiceService>();
                var context = services.GetRequiredService<ApplicationDbContext>();

                var listId = new List<int> { Id ?? 0 };
                await invoiceService.DeleteInvoicesAsync(listId);

                Console.WriteLine($"Invoice delete: {Id}");
                
            }
        }
    }
}
