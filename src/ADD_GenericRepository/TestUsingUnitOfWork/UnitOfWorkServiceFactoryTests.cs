using GenericRepository.Domain;
using GenericRepository.Infrastructure;

namespace TestUsingUnitOfWork;

public class UnitOfWorkServiceFactoryTests : IClassFixture<DependencyInjection>
{
    private readonly IUnitOfWorkServiceFactory _unitOfWorkServiceFactory;

    public UnitOfWorkServiceFactoryTests(DependencyInjection fixture)
    {
        _unitOfWorkServiceFactory = fixture.UnitOfWorkServiceFactory;
    }

    [Fact]
    public void GetUoWService_ReturnsUnitOfWorkServiceInstance()
    {
        // Act
        var unitOfWorkService = _unitOfWorkServiceFactory.GetUoWService();

        // Assert
        Assert.NotNull(unitOfWorkService);
        Assert.IsType<UnitOfWorkService>(unitOfWorkService);
    }
}