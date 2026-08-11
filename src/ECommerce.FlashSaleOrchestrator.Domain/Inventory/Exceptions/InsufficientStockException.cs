using ECommerce.FlashSaleOrchestrator.Domain.Products;

namespace ECommerce.FlashSaleOrchestrator.Domain.Inventory.Exceptions;

public sealed class InsufficientStockException : InvalidOperationException
{
    public ProductId ProductId { get; }

    public int AvailableQuantity { get; }

    public int RequestedQuantity { get; }

    public InsufficientStockException(
        ProductId productId,
        int availableQuantity,
        int requestedQuantity)
        : base(
            $"Insufficient stock for product '{productId}'. " +
            $"Available: {availableQuantity}, requested: {requestedQuantity}.")
    {
        ProductId = productId;
        AvailableQuantity = availableQuantity;
        RequestedQuantity = requestedQuantity;
    }
}