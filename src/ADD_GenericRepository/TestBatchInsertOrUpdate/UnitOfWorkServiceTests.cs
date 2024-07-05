using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace GenericRepository.Infrastructure.Tests;

public class UnitOfWorkServiceTests : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly DbContextOptions<TestDbContext> _contextOptions;
    private readonly TestDbContext _context;
    private readonly UnitOfWorkService _unitOfWorkService;
    private readonly List<TestEntity> _entities;

    public UnitOfWorkServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
        var connection = new SqliteConnection(connectionStringBuilder.ToString());

        _contextOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;

        _context = new TestDbContext(_contextOptions);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _unitOfWorkService = new UnitOfWorkService(_context);

        _entities = Enumerable.Range(1, 1000).Select(i => new TestEntity { Id = Guid.NewGuid(), Name = $"Entity{i}" }).ToList();
    }

[Fact]
        public async Task SaveChangesAsync_ShouldSaveEntities()
        {
            var repository = _unitOfWorkService.GetRepository<TestEntity>();

            foreach (var entity in _entities)
            {
                await repository.AddAsync(entity);
            }

            var stopwatch = Stopwatch.StartNew();
            await _unitOfWorkService.SaveChangesAsync();
            stopwatch.Stop();

            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _testOutputHelper.WriteLine($"SaveChangesAsync: {elapsedMilliseconds} ms");
        }

        [Fact]
        public async Task BulkInsertAsync_ShouldInsertEntities()
        {
            // Clear the context to avoid any potential conflicts
            _context.TestEntities.RemoveRange(_context.TestEntities);
            await _context.SaveChangesAsync();

            var entities = new List<TestEntity>
            {
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" },
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity2" }
            };

            var stopwatch = Stopwatch.StartNew();
            await _unitOfWorkService.BulkInsertAsync(entities);
            stopwatch.Stop();

            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _testOutputHelper.WriteLine($"BulkInsertAsync: {elapsedMilliseconds} ms");

            var dbEntities = await _context.TestEntities.ToListAsync();
            Assert.Equal(2, dbEntities.Count);
            Assert.Contains(dbEntities, e => e.Name == "Entity1");
            Assert.Contains(dbEntities, e => e.Name == "Entity2");
        }

        [Fact]
        public async Task BulkInsertAsync_ShouldUpdateEntities()
        {
            var entities = new List<TestEntity>
            {
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" },
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity2" }
            };

            await _context.TestEntities.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            entities[0].Name = "UpdatedEntity1";
            entities[1].Name = "UpdatedEntity2";

            var stopwatch = Stopwatch.StartNew();
            await _unitOfWorkService.BulkInsertAsync(entities);
            stopwatch.Stop();

            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _testOutputHelper.WriteLine($"BulkInsertAsync: {elapsedMilliseconds} ms");

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