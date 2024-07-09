using Microsoft.EntityFrameworkCore;

namespace TestRawSqlRunning;

public class BloggingContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Reply> Replies { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Tag> Tags { get; set; }

    public BloggingContext()
    {
    }

    public BloggingContext(DbContextOptions options) : base(options)
    {
    }
}