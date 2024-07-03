using GenericRepository.Domain;

namespace TestConcurrentProcessing;

public class ParallelProcessingTests : IClassFixture<DependencyInjection>
{
    private readonly IUnitOfWorkServiceFactory _unitOfWorkServiceFactory;

    public ParallelProcessingTests(DependencyInjection fixture)
    {
        _unitOfWorkServiceFactory = fixture.UnitOfWorkServiceFactory;
    }

    [Fact]
    public async Task Parallel_1_AddEntities_ToDatabase()
    {
        var tasks = new List<Task>();

        Parallel.For(0, 10, i =>
        {
            tasks.Add(Task.Run(async () =>
            {
                using var unitOfWork = _unitOfWorkServiceFactory.GetUoWService();
                var repository = unitOfWork.GetRepository<Blog>();
                var blog = new Blog { Url = $"http://example.com/{i}" };
                repository.Add(blog);
                await unitOfWork.SaveChangesAsync();
            }));
        });

        await Task.WhenAll(tasks);

        using var finalUnitOfWork = _unitOfWorkServiceFactory.GetUoWService();
        var allBlogs = finalUnitOfWork.GetRepository<Blog>().GetQueryable().ToList();

        Assert.Equal(10, allBlogs.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.Contains(allBlogs, b => b.Url == $"http://example.com/{i}");
        }
    }
    
    [Fact]
    public async Task Parallel_2_AddAndRemoveEntities_ToDatabase()
    {
        var tasks = new List<Task>();

        // Add entities in parallel
        Parallel.For(0, 10, i =>
        {
            tasks.Add(Task.Run(async () =>
            {
                using var unitOfWork = _unitOfWorkServiceFactory.GetUoWService();
                var repository = unitOfWork.GetRepository<Blog>();
                var blog = new Blog { Url = $"http://example.com/{i}" };
                repository.Add(blog);
                await unitOfWork.SaveChangesAsync();
            }));
        });

        await Task.WhenAll(tasks);

        // Remove entities in parallel
        tasks.Clear();
        Parallel.For(0, 10, i =>
        {
            tasks.Add(Task.Run(async () =>
            {
                using var unitOfWork = _unitOfWorkServiceFactory.GetUoWService();
                var repository = unitOfWork.GetRepository<Blog>();
                var blog = repository.GetQueryable().FirstOrDefault(b => b.Url == $"http://example.com/{i}");
                if (blog != null)
                {
                    repository.Delete(blog);
                    await unitOfWork.SaveChangesAsync();
                }
            }));
        });

        await Task.WhenAll(tasks);

        using var finalUnitOfWork = _unitOfWorkServiceFactory.GetUoWService();
        var allBlogs = finalUnitOfWork.GetRepository<Blog>().GetQueryable().ToList();

        Assert.Empty(allBlogs);
    }
    
    /// <summary>
    /// This is not passed as we did not make the code parallel yet
    /// </summary>
    // [Fact]
    // public async Task Parallel_3_AddEntities_ParallelAndOnce_ToDatabase()
    // {
    //     var tasks = new List<Task>();
    //     using var unitOfWork = _unitOfWorkServiceFactory.GetUoWService();
    //     Parallel.For(0, 10, i =>
    //     {
    //         tasks.Add(Task.Run(async () =>
    //         {
    //             var repository = unitOfWork.GetRepository<Blog>();
    //             var blog = new Blog { Url = $"http://example.com/{i}" };
    //             // repository.Add(blog);
    //         }));
    //     });
    //
    //     await Task.WhenAll(tasks);
    //     await unitOfWork.SaveChangesAsync();
    //     
    //     using var finalUnitOfWork = _unitOfWorkServiceFactory.GetUoWService();
    //     var allBlogs = finalUnitOfWork.GetRepository<Blog>().GetQueryable().ToList();
    //
    //     Assert.Equal(10, allBlogs.Count);
    //     for (int i = 0; i < 10; i++)
    //     {
    //         Assert.Contains(allBlogs, b => b.Url == $"http://example.com/{i}");
    //     }
    // }
}