using EFCore.SaveOnly.Library;
using Microsoft.EntityFrameworkCore;
using Testing.Models;

namespace Testing;

public class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options), IIsNew
{
    public Func<object, bool> IsNewConvention => entity => entity switch
    {
        Customer c => c.Id == 0,
        Product p => p.Id == 0,
        Order o => o.Id == 0,
        OrderLine ol => ol.Id == 0,
        _ => false
    };

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
}
