using GenericRepository.Domain;
using GenericRepository.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace TestUsingRepository;

public class TestNotInclude
{
    private BloggingContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BloggingContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        var context = new BloggingContext(options);

        // Add test data
        context.Blogs.Add(new Blog
        {
            BlogId = 1,
            Url = "http://sample.com",
            Posts = new List<Post>
            {
                new Post
                {
                    PostId = 1,
                    Title = "First Post",
                    Comments = new List<Comment>
                    {
                        new Comment
                        {
                            CommentId = 1,
                            Content = "First Comment",
                            Replies = new List<Reply>
                            {
                                new Reply { ReplyId = 1, Content = "First Reply" }
                            },
                            Likes = new List<Like>
                            {
                                new Like { LikeId = 1 }
                            }
                        }
                    },
                    Tags = new List<Tag>
                    {
                        new Tag { TagId = 1, Name = "Tag1" }
                    }
                }
            }
        });

        context.SaveChanges();
        return context;
    }

    [Fact]
    public void TestIncludeExecutorWithoutComments()
    {
        using (var context = CreateContext())
        {
            var blogs = new IncludeTree<Blog>();

            var includePath = blogs
                .Include(b => b.Posts)
                .ThenInclude((Post p) => p.Tags);

            var result = IncludeExecutor.ExecuteIncludes(context.Blogs.AsNoTracking(), includePath).ToList();

            // Asserts
            Assert.Single(result);
            var blog = result.First();
            Assert.Equal("http://sample.com", blog.Url);
            Assert.Single(blog.Posts);

            var post = blog.Posts.First();
            Assert.Equal("First Post", post.Title);

            // Assert that Comments is not included and is null
            Assert.Null(post.Comments);

            // Assert that Tags are included
            Assert.Single(post.Tags);
            var tag = post.Tags.First();
            Assert.Equal("Tag1", tag.Name);
        }
    }
}