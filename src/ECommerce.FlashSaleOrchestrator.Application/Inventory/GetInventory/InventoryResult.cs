namespace ECommerce.FlashSaleOrchestrator.Application
    .Inventory.GetInventory;

public sealed record InventoryResult(
    Guid ProductId,
    int AvailableQuantity,
    bool IsDepleted);