using ECommerce.FlashSaleOrchestrator.Domain.Carts;

namespace ECommerce.FlashSaleOrchestrator.Application.Abstractions.Persistence;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(
        CartId cartId,
        CancellationToken cancellationToken = default);
}