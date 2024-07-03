using GenericRepository.Domain;
using GenericRepository.Infrastructure;

namespace TestUsingUnitOfWork;

public class UnitOfWorkServiceTests : IClassFixture<DependencyInjection>
{
    private readonly IUnitOfWorkService _unitOfWorkService;

    public UnitOfWorkServiceTests(DependencyInjection fixture)
    {
        _unitOfWorkService = fixture.UnitOfWorkService;
    }

    [Fact]
    public void GetRepository_ReturnsRepositoryInstance()
    {
        // Act
        var repository = _unitOfWorkService.GetRepository<Blog>();

        // Assert
        Assert.NotNull(repository);
        Assert.IsType<Repository<Blog>>(repository);
    }

    [Fact]
    public void SaveChanges_CallsDbContextSaveChanges()
    {
        // Act
        var result = _unitOfWorkService.SaveChanges();

        // Assert
        Assert.Equal(0, result); // No changes made, should return 0
    }

    [Fact]
    public async Task SaveChangesAsync_CallsDbContextSaveChangesAsync()
    {
        // Act
        var result = await _unitOfWorkService.SaveChangesAsync();

        // Assert
        Assert.Equal(0, result); // No changes made, should return 0
    }

    [Fact]
    public void Dispose_CallsDbContextDispose()
    {
        // Act
        _unitOfWorkService.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => _unitOfWorkService.SaveChanges());
    }

    [Fact]
    public void CommitTransaction_CommitsTransaction()
    {
        // Arrange
        _unitOfWorkService.BeginTransaction();

        var repository = _unitOfWorkService.GetRepository<Blog>();
        var blog = new Blog { BlogId = 1, Url = "http://example.com" };
        repository.Add(blog);

        // Act
        _unitOfWorkService.CommitTransaction();

        // Assert
        var committedBlog = repository.GetQueryable().FirstOrDefault(b => b.BlogId == 1);
        Assert.NotNull(committedBlog);
        Assert.Equal("http://example.com", committedBlog.Url);
    }

    [Fact]
    public void RollbackTransaction_RollsBackTransaction()
    {
        // Arrange
        _unitOfWorkService.BeginTransaction();

        var repository = _unitOfWorkService.GetRepository<Blog>();
        var blog = new Blog { BlogId = 2, Url = "http://example.com" };
        repository.Add(blog);

        // Act
        _unitOfWorkService.RollbackTransaction();

        // Assert
        var rolledBackBlog = repository.GetQueryable().FirstOrDefault(b => b.BlogId == 2);
        Assert.Null(rolledBackBlog);
    }

    [Fact]
    public void Dispose_SuppressesFinalization()
    {
        // Arrange
        var unitOfWorkService = new UnitOfWorkService(new BloggingContext());

        // Act
        unitOfWorkService.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => unitOfWorkService.SaveChanges());
        Assert.Throws<ObjectDisposedException>(() => unitOfWorkService.SaveChangesAsync().GetAwaiter().GetResult());
    }
}