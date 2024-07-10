using System.Diagnostics;
using EFCore.BulkExtensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace GenericRepository.Infrastructure.Tests
{
    public class UnitOfWorkServiceBenchmarkTests : IDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly DbContextOptions<TestDbContext> _contextOptions;
        private readonly TestDbContext _context;
        private readonly UnitOfWorkService _unitOfWorkService;
        private readonly List<TestEntity> _entities;

        public UnitOfWorkServiceBenchmarkTests(ITestOutputHelper testOutputHelper)
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

            _entities = Enumerable.Range(1, 100000).Select(i => new TestEntity { Id = Guid.NewGuid(), Name = $"Entity{i}" }).ToList();
        }

        [Fact]
        public async Task Benchmark_SaveChangesAsync()
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
        public async Task Benchmark_BulkInsertAsync()
        {
            // Clear the context to avoid any potential conflicts
            _context.TestEntities.RemoveRange(_context.TestEntities);
            await _context.SaveChangesAsync();

            // Ensure the database is empty before the operation
            Assert.Equal(0, await _context.TestEntities.CountAsync());

            // Start the stopwatch for benchmarking
            var stopwatch = Stopwatch.StartNew();
            await _unitOfWorkService.BulkInsertAsync(_entities, 10000);
            stopwatch.Stop();

            // Ensure the database has the correct number of entries after the operation
            _testOutputHelper.WriteLine($"Entities count: {_entities.Count}");
            Assert.Equal(_entities.Count, await _context.TestEntities.CountAsync());

            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _testOutputHelper.WriteLine($"BulkInsertAsync: {elapsedMilliseconds} ms");
        }

        [Fact]
        public async Task Benchmark_3rdParty_BulkInsertAsync()
        {
            // Clear the context to avoid any potential conflicts
            _context.TestEntities.RemoveRange(_context.TestEntities);
            await _context.SaveChangesAsync();

            var stopwatch = Stopwatch.StartNew();
            await _context.BulkInsertAsync(_entities, config =>
            {
                config.BatchSize = 10000;
            });
            stopwatch.Stop();
            
            // Ensure the database has the correct number of entries after the operation
            _testOutputHelper.WriteLine($"Entities count: {_entities.Count}");
            Assert.Equal(_entities.Count, await _context.TestEntities.CountAsync());

            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _testOutputHelper.WriteLine($"BulkInsertAsync: {elapsedMilliseconds} ms");
        }
        
        [Fact]
        public async Task Benchmark_2_BulkInserts()
        {
            // Clear the context to avoid any potential conflicts
            _context.TestEntities.RemoveRange(_context.TestEntities);
            await _context.SaveChangesAsync();

            var stopwatch = Stopwatch.StartNew();
            await _unitOfWorkService.BulkInsertAsync(_entities, 10000);
            stopwatch.Stop();

            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _testOutputHelper.WriteLine($"BulkInsertAsync: {elapsedMilliseconds} ms");
        
            // Clear the context to avoid any potential conflicts
            // _context.TestEntities.RemoveRange(_context.TestEntities);
            // await _context.SaveChangesAsync();

            stopwatch = Stopwatch.StartNew();
            await _context.BulkInsertAsync(_entities, config =>
            {
                config.BatchSize = 10000;
            });
            stopwatch.Stop();

            elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _testOutputHelper.WriteLine($"BulkInsert 3rd Party: {elapsedMilliseconds} ms");
        }

        
        [Fact]
        public async Task Benchmark_2_BulkInserts_ChangeOrders()
        {
            // Clear the context to avoid any potential conflicts
            _context.TestEntities.RemoveRange(_context.TestEntities);
            await _context.SaveChangesAsync();

            var stopwatch = Stopwatch.StartNew();
            await _context.BulkInsertAsync(_entities, config =>
            {
                config.BatchSize = 10000;
            });
            stopwatch.Stop();

            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _testOutputHelper.WriteLine($"BulkInsert 3rd Party: {elapsedMilliseconds} ms");
        
            // Clear the context to avoid any potential conflicts
            // _context.TestEntities.RemoveRange(_context.TestEntities);
            // await _context.SaveChangesAsync();

            stopwatch = Stopwatch.StartNew();
            await _unitOfWorkService.BulkInsertAsync(_entities, 10000);
            stopwatch.Stop();

            elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _testOutputHelper.WriteLine($"BulkInsertAsync: {elapsedMilliseconds} ms");
        }
        
        // Dispose method to close the in-memory SQLite connection
        public void Dispose()
        {
            _context.Database.CloseConnection();
            _context.Dispose();
        }
    }
}