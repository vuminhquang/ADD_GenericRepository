using System.Linq.Expressions;
using GenericRepository.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenericRepository.Infrastructure;

public class Repository<TEntity>(DbContext context) : IRepository<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();
    private readonly DbContext _context = context;

    public IQueryable<TEntity> GetQueryable()
    {
        return _dbSet;
    }

    public void Add(TEntity entity)
    {
        _dbSet.Add(entity);
    }

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public Task AddAsync(TEntity entity)
    {
        return _dbSet.AddAsync(entity).AsTask();
    }

    public Task UpdateAsync(TEntity entity)
    {
        if (_context.Entry(entity).State == EntityState.Detached)
        {
            _dbSet.Attach(entity);
        }
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity)
    {
        if (_context.Entry(entity).State == EntityState.Detached)
        {
            _dbSet.Attach(entity);
        }
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public Task<int> ExecuteDeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return _dbSet.Where(predicate).ExecuteDeleteAsync();
    }

    public IQueryable<TEntity> IncludeExecute(IncludeTree<TEntity> includeTree)
    {
        return IncludeExecutor.ExecuteIncludes(_dbSet, includeTree);
    }
    
    public IQueryable<TEntity> IncludeExecute(IQueryable<TEntity> query, IncludeTree<TEntity> includeTree)
    {
        return IncludeExecutor.ExecuteIncludes(query, includeTree);
    }
}