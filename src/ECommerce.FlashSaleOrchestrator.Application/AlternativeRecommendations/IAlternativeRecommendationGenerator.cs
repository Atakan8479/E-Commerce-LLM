namespace ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;

public interface IAlternativeRecommendationGenerator
{
    Task<AlternativeRecommendationResult> GenerateAsync(
        AlternativeRecommendationRequest request,
        CancellationToken cancellationToken = default);
}