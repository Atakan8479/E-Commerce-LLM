namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.GetRecommendationPlans;

public sealed record RecommendationPlanResult(
    Guid EventId,
    Guid OriginalProductId,
    IReadOnlyList<RecommendationPlanItemResult>
        Recommendations,
    AlternativeRecommendationSource Source,
    string CorrelationId,
    DateTime CreatedAtUtc);

public sealed record RecommendationPlanItemResult(
    Guid ProductId,
    string Reason);