namespace ECommerce.FlashSaleOrchestrator.Api
    .Contracts.Inventory;

public sealed record DecreaseStockResponse(
    Guid ProductId,
    int RemainingQuantity,
    bool IsDepleted,
    bool RecommendationRequested,
    string CorrelationId);