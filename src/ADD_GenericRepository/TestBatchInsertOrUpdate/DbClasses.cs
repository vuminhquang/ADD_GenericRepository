using Microsoft.EntityFrameworkCore;

namespace GenericRepository.Infrastructure.Tests;

public class TestDbContext : DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>().ToTable("TestEntities");
    }
}

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}