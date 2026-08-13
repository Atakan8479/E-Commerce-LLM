using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Repositories;

public sealed class InventoryRepository
    : IInventoryRepository
{
    private readonly FlashSaleOrchestratorDbContext _dbContext;

    public InventoryRepository(
        FlashSaleOrchestratorDbContext dbContext)
    {
        _dbContext =
            dbContext
            ?? throw new ArgumentNullException(
                nameof(dbContext));
    }

    public Task<InventoryItem?> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productId);

        return _dbContext.InventoryItems
            .SingleOrDefaultAsync(
                inventoryItem =>
                    inventoryItem.ProductId == productId,
                cancellationToken);
    }
}