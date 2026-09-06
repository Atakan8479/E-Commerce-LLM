namespace ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;

public sealed record AlternativeRecommendationResult(
    IReadOnlyList<AlternativeRecommendation> Recommendations);

public sealed record AlternativeRecommendation(
    Guid ProductId,
    string Reason);