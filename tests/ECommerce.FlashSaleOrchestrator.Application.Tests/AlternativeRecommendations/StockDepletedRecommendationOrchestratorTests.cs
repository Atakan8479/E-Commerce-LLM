using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;

namespace ECommerce.FlashSaleOrchestrator.Application.Tests
    .AlternativeRecommendations;

public sealed class
    StockDepletedRecommendationOrchestratorTests
{
    [Fact]
    public async Task
        OrchestrateAsync_ShouldPersistPlanWithExecutionOutcome()
    {
        var eventId =
            Guid.NewGuid();

        var depletedProductId =
            Guid.NewGuid();

        var candidateId =
            Guid.NewGuid();

        var createdAtUtc =
            new DateTimeOffset(
                2026,
                9,
                16,
                12,
                30,
                0,
                TimeSpan.Zero);

        var candidateSet =
            new AlternativeCandidateSet(
                new DepletedProductContext(
                    depletedProductId,
                    "Depleted Product",
                    "Electronics"),
                [
                    new AlternativeCandidate(
                        candidateId,
                        "Alternative Product",
                        "Electronics",
                        12)
                ]);

        var result =
            new AlternativeRecommendationResult(
                [
                    new AlternativeRecommendation(
                        candidateId,
                        "Suitable alternative.")
                ]);

        var outcome =
            new AlternativeRecommendationGenerationOutcome(
                result,
                AlternativeRecommendationSource.Llm);

        var candidateProvider =
            new FakeAlternativeCandidateProvider(
                candidateSet);

        var executor =
            new FakeAlternativeRecommendationExecutor(
                outcome);

        var repository =
            new FakeAlternativeRecommendationPlanRepository();

        var orchestrator =
            new StockDepletedRecommendationOrchestrator(
                candidateProvider,
                executor,
                repository,
                new FixedTimeProvider(
                    createdAtUtc));

        var plan =
            await orchestrator.OrchestrateAsync(
                eventId,
                depletedProductId,
                "orchestration-correlation");

        Assert.NotNull(
            plan);

        Assert.Equal(
            1,
            candidateProvider.CallCount);

        Assert.Equal(
            depletedProductId,
            candidateProvider.LastDepletedProductId);

        Assert.Equal(
            10,
            candidateProvider.LastLimit);

        Assert.Equal(
            1,
            executor.CallCount);

        Assert.NotNull(
            executor.LastRequest);

        Assert.Equal(
            "orchestration-correlation",
            executor.LastRequest.CorrelationId);

        Assert.Same(
            candidateSet.DepletedProduct,
            executor.LastRequest.DepletedProduct);

        Assert.Same(
            candidateSet.Candidates,
            executor.LastRequest.Candidates);

        Assert.Equal(
            eventId,
            plan.EventId);

        Assert.Equal(
            depletedProductId,
            plan.OriginalProductId);

        Assert.Same(
            result,
            plan.Result);

        Assert.Equal(
            AlternativeRecommendationSource.Llm,
            plan.Source);

        Assert.Equal(
            "orchestration-correlation",
            plan.CorrelationId);

        Assert.Equal(
            createdAtUtc.UtcDateTime,
            plan.CreatedAtUtc);

        Assert.Same(
            plan,
            repository.AddedPlan);

        Assert.Equal(
            1,
            repository.CallCount);
    }

    [Fact]
    public async Task
        OrchestrateAsync_ShouldPersistDeterministicPlan_WhenNoCandidatesExist()
    {
        var depletedProductId =
            Guid.NewGuid();

        var candidateSet =
            new AlternativeCandidateSet(
                new DepletedProductContext(
                    depletedProductId,
                    "Depleted Product",
                    "Electronics"),
                []);

        var result =
            new AlternativeRecommendationResult(
                []);

        var outcome =
            new AlternativeRecommendationGenerationOutcome(
                result,
                AlternativeRecommendationSource.Deterministic);

        var candidateProvider =
            new FakeAlternativeCandidateProvider(
                candidateSet);

        var executor =
            new FakeAlternativeRecommendationExecutor(
                outcome);

        var repository =
            new FakeAlternativeRecommendationPlanRepository();

        var orchestrator =
            new StockDepletedRecommendationOrchestrator(
                candidateProvider,
                executor,
                repository,
                new FixedTimeProvider(
                    DateTimeOffset.UtcNow));

        var plan =
            await orchestrator.OrchestrateAsync(
                Guid.NewGuid(),
                depletedProductId,
                "empty-candidate-correlation");

        Assert.NotNull(
            plan);

        Assert.Equal(
            1,
            executor.CallCount);

        Assert.NotNull(
            executor.LastRequest);

        Assert.Empty(
            executor.LastRequest.Candidates);

        Assert.Equal(
            AlternativeRecommendationSource.Deterministic,
            plan.Source);

        Assert.Empty(
            plan.Result.Recommendations);

        Assert.Same(
            plan,
            repository.AddedPlan);

        Assert.Equal(
            1,
            repository.CallCount);
    }

    [Fact]
    public async Task
        OrchestrateAsync_ShouldReturnNullWithoutExecutionOrPersistence_WhenDepletedProductDoesNotExist()
    {
        var candidateProvider =
            new FakeAlternativeCandidateProvider(
                null);

        var executor =
            new FakeAlternativeRecommendationExecutor(
                new AlternativeRecommendationGenerationOutcome(
                    new AlternativeRecommendationResult(
                        []),
                    AlternativeRecommendationSource.Deterministic));

        var repository =
            new FakeAlternativeRecommendationPlanRepository();

        var orchestrator =
            new StockDepletedRecommendationOrchestrator(
                candidateProvider,
                executor,
                repository,
                new FixedTimeProvider(
                    DateTimeOffset.UtcNow));

        var plan =
            await orchestrator.OrchestrateAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "missing-product-correlation");

        Assert.Null(
            plan);

        Assert.Equal(
            0,
            executor.CallCount);

        Assert.Equal(
            0,
            repository.CallCount);

        Assert.Null(
            repository.AddedPlan);
    }

    private sealed class FakeAlternativeCandidateProvider
        : IAlternativeCandidateProvider
    {
        private readonly AlternativeCandidateSet?
            _candidateSet;

        public FakeAlternativeCandidateProvider(
            AlternativeCandidateSet? candidateSet)
        {
            _candidateSet =
                candidateSet;
        }

        public int CallCount { get; private set; }

        public Guid LastDepletedProductId
        {
            get;
            private set;
        }

        public int LastLimit { get; private set; }

        public Task<AlternativeCandidateSet?>
            GetCandidateSetAsync(
                Guid depletedProductId,
                int limit,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;

            LastDepletedProductId =
                depletedProductId;

            LastLimit =
                limit;

            return Task.FromResult(
                _candidateSet);
        }
    }

    private sealed class FakeAlternativeRecommendationExecutor
        : IAlternativeRecommendationExecutor
    {
        private readonly
            AlternativeRecommendationGenerationOutcome
            _outcome;

        public FakeAlternativeRecommendationExecutor(
            AlternativeRecommendationGenerationOutcome outcome)
        {
            _outcome =
                outcome;
        }

        public int CallCount { get; private set; }

        public AlternativeRecommendationRequest?
            LastRequest
        {
            get;
            private set;
        }

        public Task<
            AlternativeRecommendationGenerationOutcome>
            ExecuteAsync(
                AlternativeRecommendationRequest request,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;

            LastRequest =
                request;

            return Task.FromResult(
                _outcome);
        }
    }

    private sealed class
        FakeAlternativeRecommendationPlanRepository
        : IAlternativeRecommendationPlanRepository
    {
        public int CallCount { get; private set; }

        public AlternativeRecommendationPlan?
            AddedPlan
        {
            get;
            private set;
        }

        public Task AddAsync(
            AlternativeRecommendationPlan plan,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;

            AddedPlan =
                plan;

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AlternativeRecommendationPlan>>
            ListByCorrelationIdAsync(
                string correlationId,
                CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                correlationId);

            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<
                IReadOnlyList<AlternativeRecommendationPlan>>(
                    Array.Empty<AlternativeRecommendationPlan>());
        }
    }

    private sealed class FixedTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset
            _utcNow;

        public FixedTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow =
                utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}