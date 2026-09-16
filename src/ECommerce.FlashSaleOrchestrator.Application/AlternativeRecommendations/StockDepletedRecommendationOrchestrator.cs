using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;

namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;

public sealed class StockDepletedRecommendationOrchestrator
    : IStockDepletedRecommendationOrchestrator
{
    private const int AlternativeCandidateLimit =
        10;

    private readonly IAlternativeCandidateProvider
        _alternativeCandidateProvider;

    private readonly IAlternativeRecommendationExecutor
        _alternativeRecommendationExecutor;

    private readonly IAlternativeRecommendationPlanRepository
        _alternativeRecommendationPlanRepository;

    private readonly TimeProvider
        _timeProvider;

    public StockDepletedRecommendationOrchestrator(
        IAlternativeCandidateProvider alternativeCandidateProvider,
        IAlternativeRecommendationExecutor alternativeRecommendationExecutor,
        IAlternativeRecommendationPlanRepository
            alternativeRecommendationPlanRepository,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            alternativeCandidateProvider);

        ArgumentNullException.ThrowIfNull(
            alternativeRecommendationExecutor);

        ArgumentNullException.ThrowIfNull(
            alternativeRecommendationPlanRepository);

        ArgumentNullException.ThrowIfNull(
            timeProvider);

        _alternativeCandidateProvider =
            alternativeCandidateProvider;

        _alternativeRecommendationExecutor =
            alternativeRecommendationExecutor;

        _alternativeRecommendationPlanRepository =
            alternativeRecommendationPlanRepository;

        _timeProvider =
            timeProvider;
    }

    public async Task<AlternativeRecommendationPlan?>
        OrchestrateAsync(
            Guid eventId,
            Guid depletedProductId,
            string correlationId,
            CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException(
                "Event identifier cannot be empty.",
                nameof(eventId));
        }

        if (depletedProductId == Guid.Empty)
        {
            throw new ArgumentException(
                "Depleted product identifier cannot be empty.",
                nameof(depletedProductId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            correlationId);

        cancellationToken.ThrowIfCancellationRequested();

        var candidateSet =
            await _alternativeCandidateProvider
                .GetCandidateSetAsync(
                    depletedProductId,
                    AlternativeCandidateLimit,
                    cancellationToken);

        if (candidateSet is null)
        {
            return null;
        }

        var request =
            new AlternativeRecommendationRequest(
                correlationId,
                candidateSet.DepletedProduct,
                candidateSet.Candidates);

        var outcome =
            await _alternativeRecommendationExecutor
                .ExecuteAsync(
                    request,
                    cancellationToken);

        var plan =
            new AlternativeRecommendationPlan(
                eventId,
                depletedProductId,
                outcome.Result,
                outcome.Source,
                correlationId,
                _timeProvider
                    .GetUtcNow()
                    .UtcDateTime);

        await _alternativeRecommendationPlanRepository
            .AddAsync(
                plan,
                cancellationToken);

        return plan;
    }
}