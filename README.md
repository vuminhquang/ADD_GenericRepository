# ADD_GenericRepository

## Context

Traditional layered models like DDD (Domain-Driven Design) often limit interactions with the database. The ADD (Abstract Driven Design) model overcomes this limitation by separating abstractions from their implementations.

The `Repository` class is designed to extend the use of `Include` without being dependent on a specific infrastructure. This separation is achieved by abstracting the repository interface and implementing it in the infrastructure layer.

## Example Usage

Below is an example of how to use the `Repository` class with extended `Include` capabilities:

- **Blog**
  - **Post**
    - **Comment**
      - **Reply**
      - **Like**
    - **Tag**

```csharp
var blogs = new IncludeTree<Blog>();//Blog is the entity class

var includePath = blogs
    .Include(b => b.Posts)
    .ThenInclude(p => p.Comments)
    .ThenInclude(c => c.Replies)
    .Include(b => b.Posts)
    .ThenInclude(p => p.Comments)
    .ThenInclude(c => c.Likes)
    .Include(b => b.Posts)
    .ThenInclude(p => p.Tags);

// Execute the include paths
IncludeExecutor.ExecuteIncludes(context.Blogs, includePath); // context.Blog is the DbSet

// Execute with the repository
var result = await repository.IncludeExecute(includeTree).ToListAsync(); // full DbSet
var result = await repository.IncludeExecute(repository.GetQueryable().AsNoTracking(), includeTree).ToListAsync();
```