namespace ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Inventory;

public sealed record InventoryResponse(
    Guid ProductId,
    int AvailableQuantity,
    bool IsDepleted);