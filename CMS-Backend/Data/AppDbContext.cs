using CMS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CMS.Api.Data
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
        public  DbSet<MoneySafeType> MoneySafeType { get; set; }
        public  DbSet<MoneySafeCategory> MoneySafeCategory { get; set; }
        public  DbSet<MoneySafe> MoneySafe { get; set; }
        public  DbSet<MoneySafeTransaction> MoneySafeTransaction { get; set; }
        public  DbSet<Order> Order { get; set; }
        public  DbSet<OrderItem> OrderItem { get; set; }
        public  DbSet<OrderTechHistory> OrderTechHistory { get; set; }
        public  DbSet<OrderNoteHistory> OrderNoteHistory { get; set; }


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

            modelBuilder.Entity<MoneySafe>(e =>
            {
                e.Property(x => x.OpeningBalance).HasPrecision(18, 4);
                e.Property(x => x.OpeningBalanceCurrency).HasPrecision(18, 4);
            });

            modelBuilder.Entity<MoneySafeTransaction>(e =>
            {
                e.HasOne(x => x.OutMoneySafe)
                 .WithMany()
                 .HasForeignKey(x => x.OutMoneySafeId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.InMoneySafe)
                 .WithMany()
                 .HasForeignKey(x => x.InMoneySafeId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Order>(e =>
            {
                e.HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Auditor).WithMany().HasForeignKey(x => x.AuditorId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OrderTechHistory>(e =>
            {
                e.HasOne(x => x.Tech).WithMany().HasForeignKey(x => x.TechId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Auditor).WithMany().HasForeignKey(x => x.AuditorId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OrderNoteHistory>(e =>
            {
                e.HasOne(x => x.Auditor).WithMany().HasForeignKey(x => x.AuditorId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OrderItem>(e =>
            {
                e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.Property(x => x.ProductPrice).HasPrecision(18, 2);
                e.Property(x => x.ProductQuantity).HasPrecision(18, 3); // decide precision if quantities can be fractional
                e.Property(x => x.ProductTotal).HasPrecision(18, 2);
                e.Property(x => x.ProductDiscount).HasPrecision(18, 2);
                e.Property(x => x.ProductNetTotal).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Employee>(e =>
            {
                e.HasOne<IdentityUser>()
                   .WithMany()
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RefreshToken>(e =>
            {
                e.HasOne<IdentityUser>()
                   .WithMany()
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
