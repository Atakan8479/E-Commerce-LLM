using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;

namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.GetRecommendationPlans;

public sealed record GetRecommendationPlansByCorrelationIdQuery(
    string CorrelationId)
    : IQuery<IReadOnlyList<RecommendationPlanResult>>;