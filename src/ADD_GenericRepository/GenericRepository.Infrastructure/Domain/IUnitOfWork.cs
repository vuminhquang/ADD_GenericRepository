namespace GenericRepository.Domain;

public interface IUnitOfWork : IDisposable
{
    int SaveChanges();
    Task<int> SaveChangesAsync();
    void BeginTransaction();
    void CommitTransaction();
    void RollbackTransaction();
}

public interface IUnitOfWorkService : IUnitOfWork
{
    IRepository<TEntity> GetRepository<TEntity>() where TEntity : class;
   
}

public interface IUnitOfWorkServiceFactory
{
    IUnitOfWorkService GetUoWService();
}