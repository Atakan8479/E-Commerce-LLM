namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;

public interface IStockDepletedRecommendationOrchestrator
{
    Task<AlternativeRecommendationPlan?> OrchestrateAsync(
        Guid eventId,
        Guid depletedProductId,
        string correlationId,
        CancellationToken cancellationToken = default);
}