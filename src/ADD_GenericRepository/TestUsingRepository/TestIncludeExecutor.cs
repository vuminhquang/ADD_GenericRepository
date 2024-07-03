using GenericRepository.Domain;
using GenericRepository.Infrastructure;

namespace TestUsingRepository;

public class IncludeExecutorTests
{
    private BloggingContext CreateContext()
    {
        var context = new BloggingContext();

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
    public void TestIncludeExecutor()
    {
        using (var context = CreateContext())
        {
            var blogs = new IncludeTree<Blog>();

            var includePath = blogs
                .Include(b => b.Posts)
                .ThenInclude((Post p) => p.Comments)
                .ThenInclude((Comment c) => c.Replies)
                .Include(b => b.Posts)
                .ThenInclude((Post p) => p.Comments)
                .ThenInclude((Comment c) => c.Likes)
                .Include(b => b.Posts)
                .ThenInclude((Post p) => p.Tags);

            var result = IncludeExecutor.ExecuteIncludes(context.Blogs, includePath).ToList();

            // Asserts
            Assert.Single(result);
            var blog = result.First();
            Assert.Equal("http://sample.com", blog.Url);
            Assert.Single(blog.Posts);
            
            var post = blog.Posts.First();
            Assert.Equal("First Post", post.Title);
            Assert.Single(post.Comments);
            Assert.Single(post.Tags);
            
            var comment = post.Comments.First();
            Assert.Equal("First Comment", comment.Content);
            Assert.Single(comment.Replies);
            Assert.Single(comment.Likes);
            
            var reply = comment.Replies.First();
            Assert.Equal("First Reply", reply.Content);
            
            var like = comment.Likes.First();
            Assert.Equal(1, like.LikeId);

            var tag = post.Tags.First();
            Assert.Equal("Tag1", tag.Name);
        }
    }
}