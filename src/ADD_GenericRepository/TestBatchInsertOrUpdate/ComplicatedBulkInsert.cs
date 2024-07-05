using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GenericRepository.Infrastructure.Tests;

public class ComplicatedBulkInsert : IDisposable
{
    private readonly DbContextOptions<TestComplicatedDbContext> _contextOptions;
    private readonly TestComplicatedDbContext _context;
    private readonly UnitOfWorkService _unitOfWorkService;

    public ComplicatedBulkInsert()
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
        var connection = new SqliteConnection(connectionStringBuilder.ToString());

        _contextOptions = new DbContextOptionsBuilder<TestComplicatedDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestComplicatedDbContext(_contextOptions);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _unitOfWorkService = new UnitOfWorkService(_context);
    }

    [Fact]
    public async Task BulkInsertAsync_ShouldInsertLargeNumberOfEntities()
    {
        var products = new List<Product>();
        for (int i = 0; i < 500000; i++)
        {
            products.Add(new Product { Id = Guid.NewGuid(), Name = $"Product{i}", Price = i });
        }

        await _context.Products.AddRangeAsync(products);
        // bulk insert products
        await _unitOfWorkService.BulkInsertAsync(products);

        var orders = new List<Order>();
        var random = new Random();

        for (int i = 0; i < 500000; i++)
        {
            var orderItems = new List<OrderItem>();
            for (int j = 0; j < 10; j++)
            {
                var productId = products[random.Next(0, 500000)].Id;
                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    Quantity = random.Next(1, 100)
                });
            }

            orders.Add(new Order
            {
                Id = Guid.NewGuid(),
                OrderDate = DateTime.Now.AddDays(random.Next(-365, 0)),
                OrderItems = orderItems
            });
        }

        await _unitOfWorkService.BulkInsertAsync(orders);

        var dbOrders = await _context.Orders.Include(o => o.OrderItems).ToListAsync();
        Assert.Equal(500000, dbOrders.Count);

        var dbOrderItems = await _context.OrderItems.Include(oi => oi.Product).ToListAsync();
        Assert.Equal(500000 * 10, dbOrderItems.Count);
        Assert.All(dbOrderItems, oi => Assert.NotNull(oi.Product));
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}

public class Order
{
    public Guid Id { get; set; }
    public DateTime OrderDate { get; set; }
    public List<OrderItem> OrderItems { get; set; }
}

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public Order Order { get; set; }
    public Product Product { get; set; }
}

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public List<OrderItem> OrderItems { get; set; }
}

public class TestComplicatedDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Product> Products { get; set; }

    public TestComplicatedDbContext(DbContextOptions<TestComplicatedDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasMany(o => o.OrderItems).WithOne(oi => oi.Order).HasForeignKey(oi => oi.OrderId);
        modelBuilder.Entity<Product>().HasMany(p => p.OrderItems).WithOne(oi => oi.Product).HasForeignKey(oi => oi.ProductId);
    }
}