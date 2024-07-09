using GenericRepository.Infrastructure.RawSql;
using Microsoft.EntityFrameworkCore;

namespace TestRawSqlRunning;

public class TestRawSqlExecutor
{
    private BloggingContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<BloggingContext>()
            .UseSqlite("DataSource=:memory:") // Use SQLite in-memory database
            .Options;

        var context = new BloggingContext(options);
    
        context.Database.OpenConnection();
        context.Database.EnsureCreated(); // Ensure the schema is created

        context.Blogs.Add(new Blog { BlogId = 1, Url = "https://example.com" });
        context.Blogs.Add(new Blog { BlogId = 2, Url = "https://test.com" });
        context.SaveChanges();

        return context;
    }

    [Fact]
    public void TestExecuteRawSql()
    {
        using var context = CreateInMemoryContext();
        var sql = "SELECT BlogId, Url FROM Blogs";
        var result = RawSqlExecutor.ExecuteRawSql(context, sql);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        dynamic blog1 = result.First();
        Assert.Equal(1, blog1.BlogId);
        Assert.Equal("https://example.com", blog1.Url);

        dynamic blog2 = result.Last();
        Assert.Equal(2, blog2.BlogId);
        Assert.Equal("https://test.com", blog2.Url);
    }
}