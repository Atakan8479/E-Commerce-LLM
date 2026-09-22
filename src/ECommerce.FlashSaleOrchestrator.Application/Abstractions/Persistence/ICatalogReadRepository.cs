namespace ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;

public interface ICatalogReadRepository
{
    Task<IReadOnlyList<CatalogItemReadModel>> ListAsync(
        CancellationToken cancellationToken = default);
}

public sealed record CatalogItemReadModel(
    Guid ProductId,
    string Name,
    string Category,
    int AvailableQuantity);