using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Application
    .Catalog.GetCatalog;

namespace ECommerce.FlashSaleOrchestrator.Application.Tests
    .Catalog;

public sealed class GetCatalogQueryHandlerTests
{
    [Fact]
    public async Task
        HandleAsync_ShouldReturnOrderedCatalogItems()
    {
        var firstProductId =
            Guid.NewGuid();

        var secondProductId =
            Guid.NewGuid();

        var repository =
            new FakeCatalogReadRepository(
            [
                new CatalogItemReadModel(
                    secondProductId,
                    "Wireless Mouse",
                    "gaming-mouse",
                    15),
                new CatalogItemReadModel(
                    firstProductId,
                    "Gaming Mouse",
                    "gaming-mouse",
                    0)
            ]);

        var handler =
            new GetCatalogQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetCatalogQuery());

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            firstProductId,
            result[0].ProductId);

        Assert.Equal(
            "Gaming Mouse",
            result[0].Name);

        Assert.Equal(
            0,
            result[0].AvailableQuantity);

        Assert.True(
            result[0].IsDepleted);

        Assert.Equal(
            secondProductId,
            result[1].ProductId);

        Assert.Equal(
            15,
            result[1].AvailableQuantity);

        Assert.False(
            result[1].IsDepleted);

        Assert.Equal(
            1,
            repository.ListCallCount);
    }

    private sealed class FakeCatalogReadRepository
        : ICatalogReadRepository
    {
        private readonly IReadOnlyList<
            CatalogItemReadModel> _items;

        public int ListCallCount
        {
            get;
            private set;
        }

        public FakeCatalogReadRepository(
            IReadOnlyList<CatalogItemReadModel> items)
        {
            _items =
                items;
        }

        public Task<IReadOnlyList<CatalogItemReadModel>>
            ListAsync(
                CancellationToken cancellationToken = default)
        {
            ListCallCount++;

            return Task.FromResult(
                _items);
        }
    }
}