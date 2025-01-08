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

            base.OnModelCreating(modelBuilder);

            // Configure Invoice relationships
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Supplier)
                .WithMany()
                .HasForeignKey(i => i.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Consumer)
                .WithMany()
                .HasForeignKey(i => i.ConsumerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure owned types
            modelBuilder.Entity<Invoice>()
                .OwnsOne(i => i.Info);

            // Configure unique index for SupplierInfo
            modelBuilder.Entity<SupplierInfo>()
                .HasIndex(s => s.TaxNumber)
                .IsUnique();

            // Configure unique index for ConsumerInfo
            modelBuilder.Entity<ConsumerInfo>()
                .HasIndex(c => c.TaxNumber)
                .IsUnique();

            // Configure auto-generated IDs
            modelBuilder.Entity<Invoice>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SupplierInfo>()
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ConsumerInfo>()
                .Property(c => c.Id)
                .ValueGeneratedOnAdd();

            // Configure owned types for Products collection
            modelBuilder.Entity<Invoice>()
                .OwnsMany(i => i.Products, p =>
                {
                    p.ToTable("Products");
                    p.WithOwner().HasForeignKey("InvoiceId");
                    p.HasKey("Id");
                    p.Property<int?>("Id").ValueGeneratedOnAdd();
                });

            // Configure owned types for Taxes collection
            modelBuilder.Entity<Invoice>()
                .OwnsMany(i => i.TaxCategories, t =>
                {
                    t.ToTable("TaxCategories");
                    t.WithOwner().HasForeignKey("InvoiceId");
                    t.HasKey("Id");
                    t.Property<int?>("Id").ValueGeneratedOnAdd();
                });
        }
    }
}
