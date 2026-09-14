namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;

public interface IAlternativeRecommendationExecutor
{
    Task<AlternativeRecommendationGenerationOutcome>
        ExecuteAsync(
            AlternativeRecommendationRequest request,
            CancellationToken cancellationToken = default);
}