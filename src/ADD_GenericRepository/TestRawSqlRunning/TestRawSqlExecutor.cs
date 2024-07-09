using GenericRepository.Infrastructure.RawSql;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace TestRawSqlRunning;

public class TestRawSqlExecutor
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TestRawSqlExecutor(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private BloggingContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<BloggingContext>()
            .UseSqlite("DataSource=:memory:") // Use SQLite in-memory database
            .Options;

        var context = new BloggingContext(options);
        
        context.Database.OpenConnection();
        context.Database.EnsureCreated(); // Ensure the schema is created

        // Add Blogs
        var blog1 = new Blog { BlogId = 1, Url = "https://example.com" };
        var blog2 = new Blog { BlogId = 2, Url = "https://test.com" };
        context.Blogs.AddRange(blog1, blog2);

        // Add Posts
        var post1 = new Post { PostId = 1, Title = "Post 1", Blog = blog1 };
        var post2 = new Post { PostId = 2, Title = "Post 2", Blog = blog2 };
        context.Posts.AddRange(post1, post2);

        // Add Comments
        var comment1 = new Comment { CommentId = 1, Content = "Comment 1", Post = post1 };
        var comment2 = new Comment { CommentId = 2, Content = "Comment 2", Post = post2 };
        context.Comments.AddRange(comment1, comment2);

        // Add Replies
        var reply1 = new Reply { ReplyId = 1, Content = "Reply 1", Comment = comment1 };
        var reply2 = new Reply { ReplyId = 2, Content = "Reply 2", Comment = comment2 };
        context.Replies.AddRange(reply1, reply2);

        // Add Likes
        var like1 = new Like { LikeId = 1, Comment = comment1 };
        var like2 = new Like { LikeId = 2, Comment = comment2 };
        context.Likes.AddRange(like1, like2);

        // Add Tags
        var tag1 = new Tag { TagId = 1, Name = "Tag 1", Post = post1 };
        var tag2 = new Tag { TagId = 2, Name = "Tag 2", Post = post2 };
        context.Tags.AddRange(tag1, tag2);

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
    
    [Fact]
    public void TestComplexExecuteRawSql()
    {
        using var context = CreateInMemoryContext();

        var query = context.Blogs
            .Include(b => b.Posts)
                .ThenInclude(p => p.Comments)
                    .ThenInclude(c => c.Replies)
            .Include(b => b.Posts)
                .ThenInclude(p => p.Comments)
                    .ThenInclude(c => c.Likes)
            .Include(b => b.Posts)
                .ThenInclude(p => p.Tags)
            .SelectMany(b => b.Posts, (b, p) => new { b, p })
            .SelectMany(bp => bp.p.Comments.DefaultIfEmpty(), (bp, c) => new { bp.b, bp.p, c })
            .SelectMany(bpc => bpc.c.Replies.DefaultIfEmpty(), (bpc, r) => new { bpc.b, bpc.p, bpc.c, r })
            .SelectMany(bpcr => bpcr.c.Likes.DefaultIfEmpty(), (bpcr, l) => new { bpcr.b, bpcr.p, bpcr.c, bpcr.r, l })
            .SelectMany(bpcrl => bpcrl.p.Tags.DefaultIfEmpty(), (bpcrl, t) => new
            {
                bpcrl.b.BlogId,
                bpcrl.b.Url,
                bpcrl.p.PostId,
                bpcrl.p.Title,
                bpcrl.c.CommentId,
                CommentContent = bpcrl.c.Content,
                ReplyId = bpcrl.r != null ? bpcrl.r.ReplyId : (int?)null,
                ReplyContent = bpcrl.r != null ? bpcrl.r.Content : null,
                LikeId = bpcrl.l != null ? bpcrl.l.LikeId : (int?)null,
                TagId = t != null ? t.TagId : (int?)null,
                TagName = t != null ? t.Name : null
            });

        var rawSql = query.ToQueryString();

        var result = RawSqlExecutor.ExecuteRawSql(context, rawSql);

        Assert.NotNull(result);
        Assert.True(result.Count > 0);

        // Example assertions for the first row
        var firstRow = result.First();
        Assert.Equal(1, firstRow.BlogId);
        Assert.Equal("https://example.com", firstRow.Url);
        Assert.Equal(1, firstRow.PostId);
        Assert.Equal("Post 1", firstRow.Title);
        Assert.Equal(1, firstRow.CommentId);
        Assert.Equal("Comment 1", firstRow.CommentContent);
        Assert.Equal(1, firstRow.ReplyId);
        Assert.Equal("Reply 1", firstRow.ReplyContent);
        Assert.Equal(1, firstRow.LikeId);
        Assert.Equal(1, firstRow.TagId);
        Assert.Equal("Tag 1", firstRow.TagName);

        // Optionally, print the raw SQL for debugging
        _testOutputHelper.WriteLine(rawSql);
    }
}