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
        public  DbSet<Employee> Employee { get; set; }
        public  DbSet<CountryState> CountryState { get; set; }
        public  DbSet<City> City { get; set; }
        public  DbSet<Product> Product { get; set; }
        public  DbSet<ProductCategory> ProductCategory { get; set; }
        public  DbSet<ProductUnit> ProductUnit { get; set; }
        public  DbSet<ProductUnitCommission> ProductUnitCommission { get; set; }
        public  DbSet<ProductAssemblyDefinition> ProductAssemblyDefinition { get; set; }
        public  DbSet<ProductAssemblyOperation> ProductAssemblyOperation { get; set; }
        public  DbSet<Supplier> Supplier { get; set; }
        public  DbSet<Customer> Customer { get; set; }
        public  DbSet<CustomerSource> CustomerSource { get; set; }
        public  DbSet<Car> Car { get; set; }
        public  DbSet<Store> Store { get; set; }
        public  DbSet<StoreTransaction> StoreTransaction { get; set; }
        public  DbSet<StoreTransactionItem> StoreTransactionItem { get; set; }
        public  DbSet<Project> Project { get; set; }
        public  DbSet<ExpenseCategory> ExpenseCategory { get; set; }
        public  DbSet<ExpenseType> ExpenseType { get; set; }
        public  DbSet<OrderLine> OrderLine { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<StoreTransaction>(entity =>
            {
                entity.HasOne(t => t.OutStore)
                      .WithMany()
                      .HasForeignKey(t => t.OutStoreId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(t => t.InStore)
                      .WithMany()
                      .HasForeignKey(t => t.InStoreId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(t => t.Auditor)
                      .WithMany()
                      .HasForeignKey(t => t.AuditorId)
                      .OnDelete(DeleteBehavior.NoAction);

                // Removed the erroneous entity.HasOne(t => t.Items) block
            });

            modelBuilder.Entity<StoreTransactionItem>(entity =>
            {
                entity.HasOne(i => i.Transaction)
                      .WithMany(t => t.Items)   // <-- this line is what wires up StoreTransaction.Items
                      .HasForeignKey(i => i.TransactionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.Product)
                      .WithMany()
                      .HasForeignKey(i => i.ProductId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<OrderLine>()
            .HasMany(o => o.Cities)
            .WithMany(c => c.OrderLines)
            .UsingEntity<Dictionary<string, object>>(
                "order_line_city",
                j => j.HasOne<City>().WithMany().HasForeignKey("city_id").OnDelete(DeleteBehavior.NoAction),
                j => j.HasOne<OrderLine>().WithMany().HasForeignKey("order_line_id").OnDelete(DeleteBehavior.Cascade)
            );

            modelBuilder.Entity<OrderLine>()
            .HasMany(o => o.ProductCategories)
            .WithMany(pc => pc.OrderLines)
            .UsingEntity<Dictionary<string, object>>(
                "order_line_product_category",
                j => j.HasOne<ProductCategory>().WithMany().HasForeignKey("product_category_id").OnDelete(DeleteBehavior.NoAction),
                j => j.HasOne<OrderLine>().WithMany().HasForeignKey("order_line_id").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("order_line_id", "product_category_id");
                    j.ToTable("order_line_product_category");
                }
            );

            modelBuilder.Entity<ProductAssemblyDefinition>(entity =>
            {
                entity.HasOne(d => d.OutProduct)
                      .WithMany()
                      .HasForeignKey(d => d.OutProductId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.InProduct)
                      .WithMany()
                      .HasForeignKey(d => d.InProductId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ProductAssemblyOperation>(entity =>
            {
                entity.HasOne(o => o.OutProduct)
                      .WithMany()
                      .HasForeignKey(o => o.OutProductId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(o => o.InStore)
                      .WithMany()
                      .HasForeignKey(o => o.InStoreId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(o => o.OutStore)
                      .WithMany()
                      .HasForeignKey(o => o.OutStoreId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
