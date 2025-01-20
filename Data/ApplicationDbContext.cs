using Microsoft.EntityFrameworkCore;
using UpRestEye3.Models.DAO;


namespace UpRestEye3.Data
{
    public class ApplicationDbContext : DbContext
    {
        
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<InvoiceDAO> Invoices { get; set; }
        public DbSet<TaxesDAO> TaxCategories { get; set; }
        public DbSet<ProductDAO> Products { get; set; }
        public DbSet<SupplierDAO> Suppliers { get; set; }
        public DbSet<ConsumerDAO> Consumers { get; set; }
        public DbSet<UserDAO> Users { get; set; }
        public DbSet<ConnectionParameterDAO> ConnectionParameters { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            ////////////////////////////////////////////////////////////////
            /// Invoice relationships
            ////////////////////////////////////////////////////////////////
            

            // Configure Invoice relationships
            modelBuilder.Entity<InvoiceDAO>()
                .HasOne(i => i.Supplier)
                .WithMany(s => s.Invoices)
                .HasForeignKey(i => i.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoiceDAO>()
                .HasOne(i => i.Consumer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.ConsumerId)
                .OnDelete(DeleteBehavior.Restrict);


            // Configure auto-generated IDs
            modelBuilder.Entity<InvoiceDAO>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();

            // Configure owned types for Products collection
            modelBuilder.Entity<InvoiceDAO>()
                .OwnsMany(i => i.Products, p =>
                {
                    p.ToTable("Products");
                    p.WithOwner().HasForeignKey("InvoiceId");
                    p.HasKey("Id");
                    p.Property<int?>("Id").ValueGeneratedOnAdd();
                });

            // Configure owned types for Taxes collection
            modelBuilder.Entity<InvoiceDAO>()
                .OwnsMany(i => i.TaxCategories, t =>
                {
                    t.ToTable("TaxCategories");
                    t.WithOwner().HasForeignKey("InvoiceId");
                    t.HasKey("Id");
                    t.Property<int?>("Id").ValueGeneratedOnAdd();
                });
            ////////////////////////////////////////////////////////////////

            ////////////////////////////////////////////////////////////////
            /// Supplier
            ////////////////////////////////////////////////////////////////

            // Configure unique index for SupplierInfo
            modelBuilder.Entity<SupplierDAO>()
                .HasIndex(s => new { s.TaxNumber, s.ConsumerId })
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
                .HasMany(c => c.Users)
                .WithOne(u => u.Consumer)
                .HasForeignKey(u => u.ConsumerId);

            modelBuilder.Entity<ConsumerDAO>()
                .HasMany(c => c.Suppliers)
                .WithOne(s => s.Consumer)
                .HasForeignKey(s => s.ConsumerId);

            modelBuilder.Entity<ConsumerDAO>()
                .HasOne(c => c.ConnectionParameter)
                .WithOne(cp => cp.Consumer)
                .HasForeignKey<ConnectionParameterDAO>(cp => cp.ConsumerId);

            ////////////////////////////////////////////////////////////////
            /// User
            ////////////////////////////////////////////////////////////////


            modelBuilder.Entity<UserDAO>()
                .HasIndex(u => u.Login)
                .IsUnique();

            modelBuilder.Entity<UserDAO>()
                .HasKey(u => u.Id);
            
            // Configure auto-generated IDs
            modelBuilder.Entity<UserDAO>()
                .Property(u => u.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<UserDAO>()
                .HasOne(u => u.Consumer)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.ConsumerId);


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

        }
    }
}
