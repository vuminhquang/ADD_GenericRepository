using GenericRepository.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Concurrent;
using System.Diagnostics;
using GenericRepository.Infrastructure.BulkInsert;

namespace GenericRepository.Infrastructure;

public class UnitOfWorkService : IUnitOfWorkService
{
    private readonly DbContext _context;
    private IDbContextTransaction? _currentTransaction;
    private readonly Dictionary<Type, dynamic> _repositories = new();
    private bool _disposed = false;

    public UnitOfWorkService(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity);
        if (_repositories.TryGetValue(type, out var value)) return (IRepository<TEntity>)value;
        value = new Repository<TEntity>(_context);
        _repositories[type] = value;
        return (IRepository<TEntity>)value;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _repositories.Clear();
            _context.Dispose();
        }

        _disposed = true;
    }

    public int SaveChanges()
    {
        return _context.SaveChanges();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void BeginTransaction()
    {
        _currentTransaction = _context.Database.BeginTransaction();
    }

    public void CommitTransaction()
    {
        try
        {
            _context.SaveChanges();
            _currentTransaction?.Commit();
        }
        catch
        {
            RollbackTransaction();
            throw;
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    public void RollbackTransaction()
    {
        _currentTransaction?.Rollback();
        _currentTransaction?.Dispose();
        _currentTransaction = null;
    }

    public async Task BulkInsertAsync<TEntity>(IEnumerable<TEntity>? entities, int batchSize = 1000) where TEntity : class
    {
        var enumerable = entities?.ToList();
        if (enumerable == null || enumerable.Count == 0)
            return;

        var bulkInserter = new BulkInserter<TEntity>(_context);
        await bulkInserter.BulkInsertAsync(enumerable, batchSize);
    }
}