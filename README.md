# ADD_GenericRepository

Believe me, you will never need to write a single Repository again by using this generic repository.

## Repository

**Important:** If your purpose is read-only, then the `Repository` is enough, which is why you will not see `SaveChanges` methods in there. Use `UnitOfWorkService` when you need to write data.

## Dependency Injection

If you just want to work with the repository and don't care about the `UnitOfWorkService`:

```csharp
// Register the generic repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

## Include

Traditional layered models like DDD (Domain-Driven Design) often limit interactions with the database. The ADD (Abstract Driven Design) model overcomes this limitation by separating abstractions from their implementations.

The `Repository` class is designed to extend the use of `Include` without being dependent on a specific infrastructure. This separation is achieved by abstracting the repository interface and implementing it in the infrastructure layer.

### Example Usage

Below is an example of how to use the `Repository` class with extended `Include` capabilities:

- **Blog**
  - **Post**
    - **Comment**
      - **Reply**
      - **Like**
    - **Tag**

```csharp
var blogs = new IncludeTree<Blog>(); // Blog is the entity class

var includePath = blogs
    .Include(b => b.Posts)
    .ThenInclude(p => p.Comments)
    .ThenInclude(c => c.Replies)
    .Include(b => b.Posts)
    .ThenInclude(p => p.Comments)
    .ThenInclude(c => c.Likes)
    .Include(b => b.Posts)
    .ThenInclude(p => p.Tags);

// Execute with the repository
var result = await repository.IncludeExecute(includeTree).ToListAsync(); // full DbSet
var result = await repository.IncludeExecute(repository.GetQueryable().AsNoTracking(), includeTree).ToListAsync();
```

## UnitOfWorkService

The `UnitOfWorkService` class provides a way to manage database transactions and repositories efficiently and in an organized manner. This service is built on top of Entity Framework Core and helps maintain a consistent and error-free data access layer.

### Initializing UnitOfWorkService

First, create an instance of your `DbContext` class and then initialize the `UnitOfWorkService` with it. It is important to:

```csharp
.AddTransient<DbContext, YourContext>()
```

```csharp
var collection = new ServiceCollection()
    .AddDbContext<BloggingContext>(options =>
    {
        // Options for your DbContext
    })
    .AddDbContextFactory<BloggingContext>()
    .AddTransient<DbContext, BloggingContext>()
    .AddTransient<IUnitOfWorkService, UnitOfWorkService>()
    .AddTransient<IUnitOfWorkServiceFactory, UnitOfWorkServiceFactory<BloggingContext>>();

var serviceProvider = collection.BuildServiceProvider();
```

### Using Repositories

You can get the repository for a specific entity type using the `GetRepository<TEntity>` method.

```csharp
var blogRepository = unitOfWorkService.GetRepository<Blog>();

// Adding a new blog
var newBlog = new Blog { Url = "http://example.com" };
blogRepository.Add(newBlog);
unitOfWorkService.SaveChanges();
```

### Transaction Management

You can manage transactions using the `BeginTransaction`, `CommitTransaction`, and `RollbackTransaction` methods.

```csharp
try
{
    unitOfWorkService.BeginTransaction();

    var blogRepository = unitOfWorkService.GetRepository<Blog>();
    var newBlog = new Blog { Url = "http://example.com" };
    blogRepository.Add(newBlog);

    unitOfWorkService.SaveChanges();
    unitOfWorkService.CommitTransaction();
}
catch
{
    unitOfWorkService.RollbackTransaction();
    throw;
}
```

### Disposing UnitOfWorkService

Always ensure that the `UnitOfWorkService` is properly disposed of to release resources.

```csharp
unitOfWorkService.Dispose();
```

### Batch Insert or Update

The `UnitOfWorkService` also provides a method to handle batch insert or update operations asynchronously:

```csharp
await unitOfWorkService.BatchInsertOrUpdateAsync(batchSize: 1000);
```

This method processes added, modified, and deleted entities in batches and executes the corresponding SQL commands.

### Example

Here is a complete example demonstrating the usage of `UnitOfWorkService`:

```csharp
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class Blog
{
    public int BlogId { get; set; }
    public string Url { get; set; }
}

public class YourDbContext : DbContext
{
    public YourDbContext(DbContextOptions<YourDbContext> options)
        : base(options) { }

    public DbSet<Blog> Blogs { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        var collection = new ServiceCollection()
            .AddDbContext<YourDbContext>(options =>
            {
                // Configure your DbContext options here
                options.UseSqlServer("YourConnectionString");
            })
            .AddDbContextFactory<YourDbContext>()
            .AddTransient<DbContext, YourDbContext>()
            .AddTransient<IUnitOfWorkService, UnitOfWorkService>()
            .AddTransient<IUnitOfWorkServiceFactory, UnitOfWorkServiceFactory<YourDbContext>>();

        var serviceProvider = collection.BuildServiceProvider();

        using (var scope = serviceProvider.CreateScope())
        {
            var unitOfWorkService = scope.ServiceProvider.GetRequiredService<IUnitOfWorkService>();

            try
            {
                unitOfWorkService.BeginTransaction();

                var blogRepository = unitOfWorkService.GetRepository<Blog>();
                var newBlog = new Blog { Url = "http://example.com" };
                blogRepository.Add(newBlog);

                await unitOfWorkService.SaveChangesAsync();
                unitOfWorkService.CommitTransaction();
            }
            catch
            {
                unitOfWorkService.RollbackTransaction();
                throw;
            }

            // Batch insert or update example
            var blogs = new List<Blog>
            {
                new Blog { Url = "http://example1.com" },
                new Blog { Url = "http://example2.com" }
            };

            foreach (var blog in blogs)
            {
                var blogRepository = unitOfWorkService.GetRepository<Blog>();
                blogRepository.Add(blog);
            }

            await unitOfWorkService.BatchInsertOrUpdateAsync(batchSize: 1000);

            unitOfWorkService.Dispose();
        }
    }
}
```

### Summary

The `UnitOfWorkService` provides a structured way to manage database transactions and repositories while being built on top of Entity Framework Core. This service ensures that database operations are handled efficiently and consistently, reducing the risk of errors and improving code maintainability. By using the `UnitOfWorkService`, developers can focus on the business logic rather than the intricacies of database interactions.

## License

This project is licensed under the MIT License.
