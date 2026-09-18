using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;

namespace ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;

public interface IAlternativeRecommendationPlanRepository
{
    Task AddAsync(
        AlternativeRecommendationPlan plan,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlternativeRecommendationPlan>>
        ListByCorrelationIdAsync(
            string correlationId,
            CancellationToken cancellationToken = default);
}