using CMS_Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CMS_Backend.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public  DbSet<ApiActivityLog> ApiActivityLog { get; set; }
        public  DbSet<ApiDataChangeLog> ApiDataChangeLog { get; set; }
        public  DbSet<RefreshToken> RefreshTokens { get; set; }
        public  DbSet<Department> Department { get; set; }
        public  DbSet<Job> Job { get; set; }
        public  DbSet<CountryState> CountryState { get; set; }
        public  DbSet<City> City { get; set; }
        public  DbSet<Employee> Employee { get; set; }
        public  DbSet<ProductCategory> ProductCategory { get; set; }
        public  DbSet<Product> Product { get; set; }
        public  DbSet<ProductUnit> ProductUnit { get; set; }
        public  DbSet<ProductUnitCommission> ProductUnitCommission { get; set; }
        public  DbSet<Supplier> Supplier { get; set; }
        public  DbSet<Customer> Customer { get; set; }
        public  DbSet<CustomerSource> CustomerSource { get; set; }


        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
