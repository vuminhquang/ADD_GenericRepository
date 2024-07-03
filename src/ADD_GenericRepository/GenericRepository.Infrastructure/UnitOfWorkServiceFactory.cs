using GenericRepository.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenericRepository.Infrastructure;

public class UnitOfWorkServiceFactory<T>(IDbContextFactory<T> dbContextFactory) : IUnitOfWorkServiceFactory
    where T : DbContext
{
    public IUnitOfWorkService GetUoWService()
    {
        var context = dbContextFactory.CreateDbContext();
        return new UnitOfWorkService(context);
    }
}