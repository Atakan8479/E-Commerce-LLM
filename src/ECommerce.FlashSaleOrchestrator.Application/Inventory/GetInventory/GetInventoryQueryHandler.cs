using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Application
    .Inventory.GetInventory;

public sealed class GetInventoryQueryHandler
    : IQueryHandler<GetInventoryQuery, InventoryResult?>
{
    private readonly IInventoryRepository
        _inventoryRepository;

    public GetInventoryQueryHandler(
        IInventoryRepository inventoryRepository)
    {
        _inventoryRepository =
            inventoryRepository
            ?? throw new ArgumentNullException(
                nameof(inventoryRepository));
    }

    public async Task<InventoryResult?> HandleAsync(
        GetInventoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        var productId =
            ProductId.From(
                query.ProductId);

        var inventoryItem =
            await _inventoryRepository.GetByProductIdAsync(
                productId,
                cancellationToken);

        if (inventoryItem is null)
        {
            return null;
        }

        return new InventoryResult(
            inventoryItem.ProductId.Value,
            inventoryItem.AvailableQuantity.Value,
            inventoryItem.IsDepleted);
    }
}