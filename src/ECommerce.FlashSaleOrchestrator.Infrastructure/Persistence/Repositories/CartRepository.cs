using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Domain.Carts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Repositories;

public sealed class CartRepository
    : ICartRepository
{
    private readonly FlashSaleOrchestratorDbContext _dbContext;

    public CartRepository(
        FlashSaleOrchestratorDbContext dbContext)
    {
        _dbContext =
            dbContext
            ?? throw new ArgumentNullException(
                nameof(dbContext));
    }

    public Task<Cart?> GetByIdAsync(
        CartId cartId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cartId);

        return _dbContext.Carts
            .Include(cart => cart.Items)
            .SingleOrDefaultAsync(
                cart => cart.Id == cartId,
                cancellationToken);
    }
}