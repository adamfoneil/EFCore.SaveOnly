using EFCore.SaveOnly.Library;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testing.Models;

namespace Testing;

[TestClass]
[DoNotParallelize]
public sealed class SaveOnlyTests
{
    private static PostgreSqlContainer _container = null!;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _container = new PostgreSqlBuilder().Build();
        await _container.StartAsync();
        await using var db = CreateDb();
        await db.Database.EnsureCreatedAsync();
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _container.DisposeAsync();
    }

    private static StoreDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return new StoreDbContext(options);
    }

    private static async Task<StoreDbContext> CreateFreshDbAsync()
    {
        // Truncate all tables and reset sequences instead of dropping the database.
        // This avoids Postgres error 55006 and is significantly faster.
        await using var adminDb = CreateDb();
        await adminDb.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE "OrderLines", "Orders", "Products", "Customers" RESTART IDENTITY CASCADE""");
        return CreateDb();
    }

    // -------------------------------------------------------------------------
    // 1. Insert a single entity and verify it can be read back
    // -------------------------------------------------------------------------
    [TestMethod]
    public async Task InsertCustomer()
    {
        await using var db = await CreateFreshDbAsync();

        var customer = new Customer { Name = "Alice", Email = "alice@example.com" };

        await db.SaveOnlyAsync(s => s.Row(customer));

        Assert.AreNotEqual(0, customer.Id, "Id should be set by the database after insert.");

        await using var readDb = CreateDb();
        var saved = await readDb.Customers.FindAsync(customer.Id);

        Assert.IsNotNull(saved);
        Assert.AreEqual("Alice", saved.Name);
        Assert.AreEqual("alice@example.com", saved.Email);
    }

    // -------------------------------------------------------------------------
    // 2. Full row update — change a customer's email
    // -------------------------------------------------------------------------
    [TestMethod]
    public async Task UpdateCustomerEmail()
    {
        await using var db = await CreateFreshDbAsync();

        var customer = new Customer { Name = "Bob", Email = "bob@old.com" };
        await db.SaveOnlyAsync(s => s.Row(customer));

        customer.Email = "bob@new.com";
        await db.SaveOnlyAsync(s => s.Row(customer));

        await using var readDb = CreateDb();
        var saved = await readDb.Customers.FindAsync(customer.Id);

        Assert.IsNotNull(saved);
        Assert.AreEqual("bob@new.com", saved.Email);
    }

    // -------------------------------------------------------------------------
    // 3. Column-specific update — only Price should change, Name must stay the same
    // -------------------------------------------------------------------------
    [TestMethod]
    public async Task ColumnUpdateProductPrice()
    {
        await using var db = await CreateFreshDbAsync();

        var product = new Product { Name = "Widget", Price = 9.99m, StockQuantity = 100 };
        await db.SaveOnlyAsync(s => s.Row(product));

        product.Price = 14.99m;
        product.Name = "Widget (SHOULD NOT CHANGE)";

        await db.SaveOnlyAsync(s => s.Row(product, nameof(Product.Price)));

        await using var readDb = CreateDb();
        var saved = await readDb.Products.FindAsync(product.Id);

        Assert.IsNotNull(saved);
        Assert.AreEqual(14.99m, saved.Price, "Price should be updated.");
        Assert.AreEqual("Widget", saved.Name, "Name should not have changed.");
    }

    // -------------------------------------------------------------------------
    // 4. Column-specific update on a new entity must throw InvalidOperationException
    // -------------------------------------------------------------------------
    [TestMethod]
    public async Task ColumnUpdateOnNewEntityThrows()
    {
        await using var db = await CreateFreshDbAsync();

        var product = new Product { Name = "Ghost", Price = 1.00m };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => db.SaveOnlyAsync(s => s.Row(product, nameof(Product.Price))));
    }

    // -------------------------------------------------------------------------
    // 5. Delete an entity and verify it is gone
    // -------------------------------------------------------------------------
    [TestMethod]
    public async Task DeleteOrderLine()
    {
        await using var db = await CreateFreshDbAsync();

        var customer = new Customer { Name = "Carol", Email = "carol@example.com" };
        var product = new Product { Name = "Gadget", Price = 29.99m, StockQuantity = 50 };
        await db.SaveOnlyAsync(s => s.Row(customer).Row(product));

        var order = new Order { CustomerId = customer.Id, OrderDate = DateTime.UtcNow };
        await db.SaveOnlyAsync(s => s.Row(order));

        var line = new OrderLine { OrderId = order.Id, ProductId = product.Id, Quantity = 2, UnitPrice = product.Price };
        await db.SaveOnlyAsync(s => s.Row(line));

        await db.SaveOnlyAsync(s => s.Delete(line));

        await using var readDb = CreateDb();
        var deleted = await readDb.OrderLines.FindAsync(line.Id);
        Assert.IsNull(deleted, "OrderLine should have been deleted.");
    }

    // -------------------------------------------------------------------------
    // 6. Batch insert — Save(IEnumerable<T>) inserts all items
    // -------------------------------------------------------------------------
    [TestMethod]
    public async Task BatchInsertProducts()
    {
        await using var db = await CreateFreshDbAsync();

        var products = Enumerable.Range(1, 5)
            .Select(i => new Product { Name = $"Product {i}", Price = i * 1.50m, StockQuantity = i * 10 })
            .ToList();

        await db.SaveOnlyAsync(s => s.Rows(products));

        await using var readDb = CreateDb();
        var count = await readDb.Products.CountAsync();

        Assert.AreEqual(5, count);
        Assert.IsTrue(products.All(p => p.Id != 0), "All products should have been assigned a database Id.");
    }

    // -------------------------------------------------------------------------
    // 7. Full order workflow — customer + products + order with lines
    // -------------------------------------------------------------------------
    [TestMethod]
    public async Task FullOrderWorkflow()
    {
        await using var db = await CreateFreshDbAsync();

        // Insert customer and two products
        var customer = new Customer { Name = "Dave", Email = "dave@example.com" };
        var productA = new Product { Name = "Sprocket", Price = 5.00m, StockQuantity = 200 };
        var productB = new Product { Name = "Cog", Price = 12.50m, StockQuantity = 80 };
        await db.SaveOnlyAsync(s => s.Row(customer).Row(productA).Row(productB));

        // Insert order
        var order = new Order { CustomerId = customer.Id, OrderDate = DateTime.UtcNow, Status = "Pending" };
        await db.SaveOnlyAsync(s => s.Row(order));

        // Insert two order lines
        var lineA = new OrderLine { OrderId = order.Id, ProductId = productA.Id, Quantity = 3, UnitPrice = productA.Price };
        var lineB = new OrderLine { OrderId = order.Id, ProductId = productB.Id, Quantity = 1, UnitPrice = productB.Price };
        //await db.SaveOnlyAsync(s => { s.Row(lineA); s.Row(lineB); });
        await db.SaveOnlyAsync(s => s.Rows([lineA, lineB]));

        // Reduce stock for both products using column-specific updates
        productA.StockQuantity -= 3;
        productB.StockQuantity -= 1;
        await db.SaveOnlyAsync(s => s
            .Row(productA, nameof(Product.StockQuantity))
            .Row(productB, nameof(Product.StockQuantity)));

        // Update order status to Shipped
        order.Status = "Shipped";
        await db.SaveOnlyAsync(s => s.Row(order, nameof(Order.Status)));

        // Verify final state
        await using var readDb = CreateDb();

        var savedOrder = await readDb.Orders
            .Include(o => o.OrderLines)
            .FirstAsync(o => o.Id == order.Id);

        Assert.AreEqual("Shipped", savedOrder.Status);
        Assert.AreEqual(2, savedOrder.OrderLines.Count);

        var expectedTotal = (3 * 5.00m) + (1 * 12.50m);
        var actualTotal = savedOrder.OrderLines.Sum(l => l.Quantity * l.UnitPrice);
        Assert.AreEqual(expectedTotal, actualTotal);

        var savedProductA = await readDb.Products.FindAsync(productA.Id);
        var savedProductB = await readDb.Products.FindAsync(productB.Id);
        Assert.AreEqual(197, savedProductA!.StockQuantity);
        Assert.AreEqual(79, savedProductB!.StockQuantity);
    }

    // -------------------------------------------------------------------------
    // 8. Parent + child rows inserted in one SaveOnlyAsync call via navigation property
    // -------------------------------------------------------------------------
    [TestMethod]
    public async Task InsertOrderWithLinesInOneOperation()
    {
        await using var db = await CreateFreshDbAsync();

        var customer = new Customer { Name = "Eve", Email = "eve@example.com" };
        var productA = new Product { Name = "Bolt", Price = 2.00m, StockQuantity = 500 };
        var productB = new Product { Name = "Nut", Price = 1.50m, StockQuantity = 300 };
        await db.SaveOnlyAsync(s => s.Row(customer).Row(productA).Row(productB));

        var lineA = new OrderLine { ProductId = productA.Id, Quantity = 4, UnitPrice = productA.Price };
        var lineB = new OrderLine { ProductId = productB.Id, Quantity = 6, UnitPrice = productB.Price };
        var order = new Order
        {
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            OrderLines = [lineA, lineB]
        };

        await db.SaveOnlyAsync(s => s.Row(order).Rows(order.OrderLines));

        Assert.AreNotEqual(0, order.Id, "Order Id should be set after insert.");
        Assert.IsTrue(order.OrderLines.All(l => l.Id != 0), "All OrderLine Ids should be set after insert.");
        Assert.IsTrue(order.OrderLines.All(l => l.OrderId == order.Id), "All OrderLines should reference the correct OrderId.");

        await using var readDb = CreateDb();
        var saved = await readDb.Orders
            .Include(o => o.OrderLines)
            .FirstAsync(o => o.Id == order.Id);

        Assert.AreEqual(2, saved.OrderLines.Count);
        var expectedTotal = (4 * 2.00m) + (6 * 1.50m);
        var actualTotal = saved.OrderLines.Sum(l => l.Quantity * l.UnitPrice);
        Assert.AreEqual(expectedTotal, actualTotal);
    }
}
