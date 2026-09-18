using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.GetRecommendationPlans;

namespace ECommerce.FlashSaleOrchestrator.Application.Tests
    .AlternativeRecommendations;

public sealed class
    GetRecommendationPlansByCorrelationIdQueryHandlerTests
{
    [Fact]
    public async Task
        HandleAsync_ShouldMapPlansReturnedByRepository()
    {
        var correlationId =
            $"correlation-{Guid.NewGuid():N}";

        var eventId =
            Guid.NewGuid();

        var originalProductId =
            Guid.NewGuid();

        var recommendedProductId =
            Guid.NewGuid();

        var createdAtUtc =
            new DateTime(
                2026,
                9,
                18,
                20,
                30,
                0,
                DateTimeKind.Utc);

        var plan =
            new AlternativeRecommendationPlan(
                eventId,
                originalProductId,
                new AlternativeRecommendationResult(
                    new[]
                    {
                        new AlternativeRecommendation(
                            recommendedProductId,
                            "Suitable alternative.")
                    }),
                AlternativeRecommendationSource.Llm,
                correlationId,
                createdAtUtc);

        var repository =
            new FakeAlternativeRecommendationPlanRepository(
                new[]
                {
                    plan
                });

        var handler =
            new GetRecommendationPlansByCorrelationIdQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetRecommendationPlansByCorrelationIdQuery(
                    correlationId));

        var mappedPlan =
            Assert.Single(
                result);

        Assert.Equal(
            eventId,
            mappedPlan.EventId);

        Assert.Equal(
            originalProductId,
            mappedPlan.OriginalProductId);

        Assert.Equal(
            AlternativeRecommendationSource.Llm,
            mappedPlan.Source);

        Assert.Equal(
            correlationId,
            mappedPlan.CorrelationId);

        Assert.Equal(
            createdAtUtc,
            mappedPlan.CreatedAtUtc);

        var recommendation =
            Assert.Single(
                mappedPlan.Recommendations);

        Assert.Equal(
            recommendedProductId,
            recommendation.ProductId);

        Assert.Equal(
            "Suitable alternative.",
            recommendation.Reason);

        Assert.Equal(
            1,
            repository.CallCount);

        Assert.Equal(
            correlationId,
            repository.LastCorrelationId);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReturnEmpty_WhenRepositoryReturnsNoPlans()
    {
        var repository =
            new FakeAlternativeRecommendationPlanRepository(
                Array.Empty<
                    AlternativeRecommendationPlan>());

        var handler =
            new GetRecommendationPlansByCorrelationIdQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetRecommendationPlansByCorrelationIdQuery(
                    $"missing-{Guid.NewGuid():N}"));

        Assert.Empty(
            result);

        Assert.Equal(
            1,
            repository.CallCount);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectWhitespaceCorrelationId()
    {
        var repository =
            new FakeAlternativeRecommendationPlanRepository(
                Array.Empty<
                    AlternativeRecommendationPlan>());

        var handler =
            new GetRecommendationPlansByCorrelationIdQueryHandler(
                repository);

        await Assert.ThrowsAsync<ArgumentException>(
            () =>
                handler.HandleAsync(
                    new GetRecommendationPlansByCorrelationIdQuery(
                        "   ")));

        Assert.Equal(
            0,
            repository.CallCount);
    }

    private sealed class
        FakeAlternativeRecommendationPlanRepository
        : IAlternativeRecommendationPlanRepository
    {
        private readonly IReadOnlyList<
            AlternativeRecommendationPlan> _plans;

        public FakeAlternativeRecommendationPlanRepository(
            IReadOnlyList<
                AlternativeRecommendationPlan> plans)
        {
            _plans =
                plans
                ?? throw new ArgumentNullException(
                    nameof(plans));
        }

        public int CallCount { get; private set; }

        public string? LastCorrelationId
        {
            get;
            private set;
        }

        public Task AddAsync(
            AlternativeRecommendationPlan plan,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<
            IReadOnlyList<AlternativeRecommendationPlan>>
            ListByCorrelationIdAsync(
                string correlationId,
                CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                correlationId);

            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;

            LastCorrelationId =
                correlationId;

            return Task.FromResult(
                _plans);
        }
    }
}