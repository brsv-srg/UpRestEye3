using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UpRestEye3.Models.DAO;
using UpRestEye3.Models.Account;
using Microsoft.Extensions.Logging;

namespace UpRestEye3.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<AppUser>(options)
    {

        public DbSet<InvoiceDAO> Invoices { get; set; }
        public DbSet<TaxesDAO> TaxCategories { get; set; }
        public DbSet<InvoiceProductDAO> InvoiceProducts { get; set; }
        public DbSet<SupplierDAO> Suppliers { get; set; }
        public DbSet<ConsumerDAO> Consumers { get; set; }
        public DbSet<ConnectionParameterDAO> ConnectionParameters { get; set; }
        public DbSet<RMSProductDAO> RMSProducts { get; set; }
        public DbSet<ContainerDAO> Containers { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            ////////////////////////////////////////////////////////////////
            /// Invoice relationships
            ////////////////////////////////////////////////////////////////
            

            // Configure auto-generated IDs
            modelBuilder.Entity<InvoiceDAO>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<InvoiceDAO>()
                .HasOne(i => i.Consumer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.ConsumerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Invoice relationships
            modelBuilder.Entity<InvoiceDAO>()
                .HasOne(i => i.Supplier)
                .WithMany(s => s.Invoices)
                .HasForeignKey(i => i.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure owned types for Products collection
            modelBuilder.Entity<InvoiceDAO>()
                .HasMany(i => i.Products)
                .WithOne(p => p.Invoice)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);


            // Configure owned types for Taxes collection
            modelBuilder.Entity<InvoiceDAO>()
                .HasMany(i => i.TaxCategories)
                .WithOne(t => t.Invoice)
                .HasForeignKey(t => t.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            ////////////////////////////////////////////////////////////////

            ////////////////////////////////////////////////////////////////
            /// Supplier
            ////////////////////////////////////////////////////////////////

            // Configure unique index for SupplierInfo
            modelBuilder.Entity<SupplierDAO>()
                .HasIndex(s => new { s.ConsumerId, s.TaxNumber })
                .IsUnique();

            modelBuilder.Entity<SupplierDAO>()
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();
                
            modelBuilder.Entity<SupplierDAO>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<SupplierDAO>()
                .HasMany(s => s.Invoices)
                .WithOne(i => i.Supplier)
                .HasForeignKey(i => i.SupplierId);

            modelBuilder.Entity<SupplierDAO>()
                .HasOne(s => s.Consumer)
                .WithMany(c => c.Suppliers)
                .HasForeignKey(s => s.ConsumerId);
        

            ////////////////////////////////////////////////////////////////
            /// Consumer
            ////////////////////////////////////////////////////////////////

            // Configure unique index for ConsumerInfo
            modelBuilder.Entity<ConsumerDAO>()
                .HasIndex(c => c.TaxNumber)
                .IsUnique();

            modelBuilder.Entity<ConsumerDAO>()
                .Property(c => c.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ConsumerDAO>()
               .HasKey(s => s.Id);

            modelBuilder.Entity<ConsumerDAO>()
                .HasMany(c => c.Invoices)
                .WithOne(i => i.Consumer)
                .HasForeignKey(i => i.ConsumerId);

            modelBuilder.Entity<ConsumerDAO>()
                .HasMany(c => c.Suppliers)
                .WithOne(s => s.Consumer)
                .HasForeignKey(s => s.ConsumerId);

            modelBuilder.Entity<ConsumerDAO>()
                .HasOne(c => c.ConnectionParameter)
                .WithOne(cp => cp.Consumer)
                .HasForeignKey<ConnectionParameterDAO>(cp => cp.ConsumerId);

            //////////////////////////////////////////////////////////////////
            ///// User
            //////////////////////////////////////////////////////////////////


            //modelBuilder.Entity<AppUser>()
            //    .HasKey(u => u.Id);
            
            //// Configure auto-generated IDs
            //modelBuilder.Entity<AppUser>()
            //    .Property(u => u.Id)
            //    .ValueGeneratedOnAdd();

            //modelBuilder.Entity<AppUser>()
            //    .HasIndex(u => u.Login)
            //    .IsUnique();



            ////////////////////////////////////////////////////////////////
            /// Connection Parameters
            ////////////////////////////////////////////////////////////////
            
            modelBuilder.Entity<ConnectionParameterDAO>()
                .HasKey(cp => cp.Id);   
            
            modelBuilder.Entity<ConnectionParameterDAO>()
                .Property(cp => cp.Id)
                .ValueGeneratedOnAdd();
        
            modelBuilder.Entity<ConnectionParameterDAO>()
                .HasOne(cp => cp.Consumer)
                .WithOne(c => c.ConnectionParameter)
                .HasForeignKey<ConnectionParameterDAO>(cp => cp.ConsumerId);



            ////////////////////////////////////////////////////////////////
            /// RMS Products relationships
            ////////////////////////////////////////////////////////////////

            // Configure Invoice relationships
            modelBuilder.Entity<RMSProductDAO>()
                .HasOne(p => p.Consumer)
                .WithMany(c => c.RMSProducts)
                .HasForeignKey(i => i.ConsumerId)
                .OnDelete(DeleteBehavior.Restrict);


            // Configure auto-generated IDs
            modelBuilder.Entity<RMSProductDAO>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<RMSProductDAO>()
                .HasIndex(p => new { p.ConsumerId, p.RMSProductId })
                .IsUnique();


            // Configure RMSProduct vs RMSContainer
            modelBuilder.Entity<RMSProductDAO>()
                .HasMany(p => p.Containers)
                .WithOne()
                .HasForeignKey(c => c.RMSProductId)
                .OnDelete(DeleteBehavior.Restrict);




            ////////////////////////////////////////////////////////////////
            /// Invoice Products
            ////////////////////////////////////////////////////////////////


            // Configure Invoice Product vs Invoice 
            modelBuilder.Entity<InvoiceProductDAO>()
                .HasOne(i => i.Invoice)
                .WithMany(p => p.Products)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Invoice Product vs RMSProduct
            modelBuilder.Entity<InvoiceProductDAO>()
                .HasOne(i => i.RMSProduct)
                .WithMany()
                .HasForeignKey(p => p.RMSProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Invoice Product vs RMSContainer
            modelBuilder.Entity<InvoiceProductDAO>()
                .HasOne(i => i.RMSContainer)
                .WithMany()
                .HasForeignKey(p => p.RMSContainerId)
                .OnDelete(DeleteBehavior.Restrict);



            ////////////////////////////////////////////////////////////////
            /// Invoice Tax Categories
            ////////////////////////////////////////////////////////////////


            // Configure Taxes vs Invoices
            modelBuilder.Entity<TaxesDAO>()
                .HasOne(c => c.Invoice)
                .WithMany(i => i.TaxCategories)
                .HasForeignKey(t => t.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);




            // TODO надо ли вписывать связь с продуктами инвойса для продукта ОМС
            // TODO надо ли вписывать связь с продуктами инвойса для контейнера ОМС

        }
    }
}
