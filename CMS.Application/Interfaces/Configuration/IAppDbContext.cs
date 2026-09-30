using CMS.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CMS.Application.Interfaces.Configuration;

public interface IAppDbContext
{
    // HR
    DbSet<Department> Department { get; }
    DbSet<Job> Job { get; }
    DbSet<Employee> Employee { get; }

    // Geography
    DbSet<CountryState> CountryState { get; }
    DbSet<City> City { get; }

    // Catalog
    DbSet<Product> Product { get; }
    DbSet<ProductCategory> ProductCategory { get; }
    DbSet<ProductUnit> ProductUnit { get; }
    DbSet<ProductUnitCommission> ProductUnitCommission { get; }
    DbSet<ProductAssemblyDefinition> ProductAssemblyDefinition { get; }
    DbSet<ProductAssemblyOperation> ProductAssemblyOperation { get; }

    // Partners / misc
    DbSet<Supplier> Supplier { get; }
    DbSet<Customer> Customer { get; }
    DbSet<CustomerSource> CustomerSource { get; }
    DbSet<Car> Car { get; }
    DbSet<Project> Project { get; }

    // Inventory
    DbSet<Store> Store { get; }
    DbSet<StoreTransaction> StoreTransaction { get; }
    DbSet<StoreTransactionItem> StoreTransactionItem { get; }

    // Finance
    DbSet<ExpenseCategory> ExpenseCategory { get; }
    DbSet<ExpenseType> ExpenseType { get; }
    DbSet<MoneySafeType> MoneySafeType { get; }
    DbSet<MoneySafeCategory> MoneySafeCategory { get; }
    DbSet<MoneySafe> MoneySafe { get; }
    DbSet<MoneySafeTransaction> MoneySafeTransaction { get; }

    // Sales
    DbSet<Order> Order { get; }
    DbSet<OrderItem> OrderItem { get; }
    DbSet<OrderLine> OrderLine { get; }
    DbSet<OrderTechHistory> OrderTechHistory { get; }
    DbSet<OrderNoteHistory> OrderNoteHistory { get; }

    // Add only if your services use them
    DatabaseFacade Database { get; }
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}