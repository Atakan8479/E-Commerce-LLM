namespace ECommerce.FlashSaleOrchestrator.Application.Inventory.DecreaseStock;

public sealed class InventoryItemNotFoundException : InvalidOperationException
{
    public Guid ProductId { get; }

    public InventoryItemNotFoundException(Guid productId)
        : base(
            $"Inventory item for product '{productId}' was not found.")
    {
        ProductId = productId;
    }
}