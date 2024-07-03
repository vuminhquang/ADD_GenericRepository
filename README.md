# ADD_GenericRepository


## Example usage:

- **Blog**
  - **Post**
    - **Comment**
      - **Reply**
      - **Like**
    - **Tag**

```
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