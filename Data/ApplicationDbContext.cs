using Microsoft.EntityFrameworkCore;
using EyeRestWAs.Models;

namespace EyeRestWAs.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<TaxCategory> TaxCategories { get; set; }
        public DbSet<Product> Products { get; set; }
    }
}
