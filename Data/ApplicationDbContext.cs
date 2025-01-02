using Microsoft.EntityFrameworkCore;
using UpRestEye3.Models;

namespace UpRestEye3.Data
{
    public class ApplicationDbContext : DbContext
    {
        
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Taxes> TaxCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<SupplierInfo> Suppliers { get; set; }
        public DbSet<ConsumerInfo> Consumers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            // Настройка таблицы Invoice
            modelBuilder.Entity<Invoice>()
                .HasKey(i => i.Id); // Указываем первичный ключ для Invoice


            // Настраиваем уникальный индекс для Поставщика
            modelBuilder.Entity<SupplierInfo>()
                .HasIndex(s => s.TaxNumber)
                .IsUnique();

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Supplier)  // Навигационное свойство
                .WithMany()              // Связь "один-ко-многим"
                .HasForeignKey(p => p.SupplierId )      // Указываем внешний ключ
                .OnDelete(DeleteBehavior.Restrict);     // Поведение при удалении поставщика

            // Игнорируем объект SupplierInfo
            //modelBuilder.Entity<Invoice>().Ignore(i => i.Supplier);




            // Настраиваем уникальный индекс для Потребителя
            modelBuilder.Entity<ConsumerInfo>()
                .HasIndex(s => s.TaxNumber)
                .IsUnique();

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Consumer)  // Навигационное свойство
                .WithMany()              // Связь "один-ко-многим"
                .HasForeignKey(p => p.ConsumerId)       // Указываем внешний ключ
                .OnDelete(DeleteBehavior.Restrict);     // Поведение при удалении потребителя

            // Игнорируем объект ConsumerInfo
            //modelBuilder.Entity<Invoice>().Ignore(i => i.Consumer);



            // Настройка Owned Entity Types для коллекции
            modelBuilder.Entity<Invoice>()
                .OwnsMany(i => i.Products, p =>
                {
                    // Указываем имя таблицы для коллекции
                    p.ToTable("Products");
                    // Настраиваем внешний ключ, привязывающий коллекцию к Invoice
                    p.WithOwner().HasForeignKey("InvoiceId");
                    // Дополнительный первичный ключ для таблицы Products
                    p.HasKey("Id");
                    p.Property<int?>("Id").ValueGeneratedOnAdd(); 
                });
        }
    }
}
