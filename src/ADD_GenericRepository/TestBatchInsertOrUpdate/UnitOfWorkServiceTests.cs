using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GenericRepository.Infrastructure.Tests;

public class UnitOfWorkServiceTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly UnitOfWorkService _unitOfWorkService;

    public UnitOfWorkServiceTests()
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
        var connection = new SqliteConnection(connectionStringBuilder.ToString());

        var contextOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestDbContext(contextOptions);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _unitOfWorkService = new UnitOfWorkService(_context);
    }

    [Fact]
    public async Task BatchInsertOrUpdateAsync_ShouldInsertEntities()
    {
        var repository = _unitOfWorkService.GetRepository<TestEntity>();
        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Entity1" },
            new TestEntity { Id = 2, Name = "Entity2" }
        };

        foreach (var entity in entities)
        {
            await repository.AddAsync(entity);
        }

        await _unitOfWorkService.BatchInsertOrUpdateAsync();

        var dbEntities = await _context.TestEntities.ToListAsync();
        Assert.Equal(2, dbEntities.Count);
        Assert.Contains(dbEntities, e => e.Name == "Entity1");
        Assert.Contains(dbEntities, e => e.Name == "Entity2");
    }

    [Fact]
    public async Task BatchInsertOrUpdateAsync_ShouldUpdateEntities()
    {
        var repository = _unitOfWorkService.GetRepository<TestEntity>();
        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Entity1" },
            new TestEntity { Id = 2, Name = "Entity2" }
        };

        await _context.TestEntities.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        entities[0].Name = "UpdatedEntity1";
        entities[1].Name = "UpdatedEntity2";

        foreach (var entity in entities)
        {
            repository.Update(entity);
        }

        await _unitOfWorkService.BatchInsertOrUpdateAsync();

        var dbEntities = await _context.TestEntities.ToListAsync();
        Assert.Equal(2, dbEntities.Count);
        Assert.Contains(dbEntities, e => e.Name == "UpdatedEntity1");
        Assert.Contains(dbEntities, e => e.Name == "UpdatedEntity2");
    }

    // Dispose method to close the in-memory SQLite connection
    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}