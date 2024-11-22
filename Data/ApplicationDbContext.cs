using Microsoft.EntityFrameworkCore;
using UpRestEye3.Models;

namespace UpRestEye3.Data
{
    public class ApplicationDbContext : DbContext
    {
        
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<TaxCategory> TaxCategories { get; set; }
        public DbSet<Product> Products { get; set; }
    }
}
