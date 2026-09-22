namespace ECommerce.FlashSaleOrchestrator.Application.Inventory.DecreaseStock;

public sealed class InventoryConcurrencyException : Exception
{
    public Guid ProductId { get; }

    public InventoryConcurrencyException(
        Guid productId,
        string? message = null,
        Exception? innerException = null)
        : base(
            message ??
            "Inventory changed while this request was being processed. " +
            "Refresh the current inventory state and retry if appropriate.",
            innerException)
    {
        ProductId = productId;
    }
}
