using System.Linq.Expressions;

namespace GenericRepository.Domain;

public interface IRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> GetQueryable();
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    Task AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);

    Task<int> ExecuteDeleteAsync(Expression<Func<TEntity,bool>> predicate);
    IQueryable<TEntity> IncludeExecute(IncludeTree<TEntity> includeTree);
    IQueryable<TEntity> IncludeExecute(IQueryable<TEntity> query, IncludeTree<TEntity> includeTree);
}