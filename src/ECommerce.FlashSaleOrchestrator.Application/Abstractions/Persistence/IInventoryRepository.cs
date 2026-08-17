using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Application.Abstractions.Persistence;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default);
}