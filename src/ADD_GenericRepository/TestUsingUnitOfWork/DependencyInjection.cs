using GenericRepository.Domain;
using GenericRepository.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TestUsingUnitOfWork;

public class DependencyInjection : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public DependencyInjection()
    {
        var collection = new ServiceCollection()
            .AddDbContext<BloggingContext>(options =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();
                options.UseSqlite(connection);
            })
            .AddDbContextFactory<BloggingContext>()
            .AddTransient<DbContext, BloggingContext>()
            .AddTransient<IUnitOfWorkService, UnitOfWorkService>()
            .AddTransient<IUnitOfWorkServiceFactory, UnitOfWorkServiceFactory<BloggingContext>>();

        _serviceProvider = collection.BuildServiceProvider();

        // Ensure database is created
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BloggingContext>();
            context.Database.EnsureCreated();
        }
    }

    public IUnitOfWorkService UnitOfWorkService => _serviceProvider.GetRequiredService<IUnitOfWorkService>();
    public IUnitOfWorkServiceFactory UnitOfWorkServiceFactory => _serviceProvider.GetRequiredService<IUnitOfWorkServiceFactory>();

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}