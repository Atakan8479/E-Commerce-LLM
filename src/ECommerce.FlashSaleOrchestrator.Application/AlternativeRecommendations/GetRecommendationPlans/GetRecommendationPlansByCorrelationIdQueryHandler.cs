using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;

namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.GetRecommendationPlans;

public sealed class
    GetRecommendationPlansByCorrelationIdQueryHandler
    : IQueryHandler<
        GetRecommendationPlansByCorrelationIdQuery,
        IReadOnlyList<RecommendationPlanResult>>
{
    private readonly
        IAlternativeRecommendationPlanRepository
        _repository;

    public GetRecommendationPlansByCorrelationIdQueryHandler(
        IAlternativeRecommendationPlanRepository repository)
    {
        _repository =
            repository
            ?? throw new ArgumentNullException(
                nameof(repository));
    }

    public async Task<IReadOnlyList<RecommendationPlanResult>>
        HandleAsync(
            GetRecommendationPlansByCorrelationIdQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            query.CorrelationId);

        var plans =
            await _repository
                .ListByCorrelationIdAsync(
                    query.CorrelationId,
                    cancellationToken);

        return plans
            .Select(
                plan =>
                    new RecommendationPlanResult(
                        plan.EventId,
                        plan.OriginalProductId,
                        plan.Result
                            .Recommendations
                            .Select(
                                recommendation =>
                                    new RecommendationPlanItemResult(
                                        recommendation.ProductId,
                                        recommendation.Reason))
                            .ToArray(),
                        plan.Source,
                        plan.CorrelationId,
                        plan.CreatedAtUtc))
            .ToArray();
    }
}