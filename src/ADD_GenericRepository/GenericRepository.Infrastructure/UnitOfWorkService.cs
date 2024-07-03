using GenericRepository.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GenericRepository.Infrastructure;

public class UnitOfWorkService : IUnitOfWorkService
{
    private readonly DbContext _context;
    private IDbContextTransaction? _currentTransaction;
    private readonly Dictionary<Type, dynamic> _repositories = new();
    private bool _disposed = false;

    public UnitOfWorkService(DbContext context)
    {
        _context = context;
    }

    public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity);
        if (!_repositories.ContainsKey(type))
        {
            _repositories[type] = new Repository<TEntity>(_context);
        }
        return (IRepository<TEntity>)_repositories[type];
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
}