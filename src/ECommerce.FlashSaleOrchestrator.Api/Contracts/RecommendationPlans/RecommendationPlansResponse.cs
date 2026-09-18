namespace ECommerce.FlashSaleOrchestrator.Api
    .Contracts.RecommendationPlans;

public sealed record RecommendationPlansResponse(
    string CorrelationId,
    IReadOnlyList<RecommendationPlanResponse> Plans);

public sealed record RecommendationPlanResponse(
    Guid EventId,
    Guid OriginalProductId,
    IReadOnlyList<RecommendationPlanItemResponse>
        Recommendations,
    string Source,
    DateTime CreatedAtUtc);

public sealed record RecommendationPlanItemResponse(
    Guid ProductId,
    string Reason);