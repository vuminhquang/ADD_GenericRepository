namespace TestRawSqlRunning;

public class Blog
{
    public int BlogId { get; set; }
    public string Url { get; set; }
    public List<Post> Posts { get; set; }
}

public class Post
{
    public int PostId { get; set; }
    public string Title { get; set; }
    public int BlogId { get; set; }
    public Blog Blog { get; set; }
    public List<Comment> Comments { get; set; }
    public List<Tag> Tags { get; set; }
}

public class Comment
{
    public int CommentId { get; set; }
    public string Content { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; }
    public List<Reply> Replies { get; set; }
    public List<Like> Likes { get; set; }
}

public class Reply
{
    public int ReplyId { get; set; }
    public string Content { get; set; }
    public int CommentId { get; set; }
    public Comment Comment { get; set; }
}

public class Like
{
    public int LikeId { get; set; }
    public int CommentId { get; set; }
    public Comment Comment { get; set; }
}

public class Tag
{
    public int TagId { get; set; }
    public string Name { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; }
}