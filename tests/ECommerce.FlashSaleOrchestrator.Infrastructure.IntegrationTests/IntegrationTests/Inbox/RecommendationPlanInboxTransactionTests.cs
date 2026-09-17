using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application
    .IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.Inbox;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.Repositories;
using ECommerce.FlashSaleOrchestrator.Worker
    .IntegrationEvents.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Inbox;

public sealed class RecommendationPlanInboxTransactionTests
{
    [Fact]
    public async Task
        ProcessAsync_ShouldCommitInboxAndRecommendationPlanTogether()
    {
        await using var database =
            await InboxTestDatabase.CreateAsync();

        var integrationEvent =
            CreateIntegrationEvent();

        await using (var context =
                     database.CreateContext())
        {
            var repository =
                new AlternativeRecommendationPlanRepository(
                    context);

            var candidateProvider =
                new FixedAlternativeCandidateProvider(
                    integrationEvent.ProductId);

            var executor =
                new FixedAlternativeRecommendationExecutor();

            var orchestrator =
                new StockDepletedRecommendationOrchestrator(
                    candidateProvider,
                    executor,
                    repository,
                    new FixedTimeProvider(
                        DateTimeOffset.UtcNow));

            var handler =
                new StockDepletedIntegrationEventHandler(
                    orchestrator,
                    NullLogger<
                        StockDepletedIntegrationEventHandler>
                        .Instance);

            var processor =
                new InboxIntegrationEventProcessor<
                    StockDepletedIntegrationEvent>(
                    context,
                    handler,
                    NullLogger<
                        InboxIntegrationEventProcessor<
                            StockDepletedIntegrationEvent>>
                        .Instance);

            var result =
                await processor.ProcessAsync(
                    integrationEvent);

            Assert.Equal(
                IntegrationEventProcessingResult.Processed,
                result);
        }

        await using var verificationContext =
            database.CreateContext();

        var inboxMessageCount =
            await verificationContext
                .InboxMessages
                .AsNoTracking()
                .CountAsync(
                    message =>
                        message.Id ==
                        integrationEvent.EventId);

        var recommendationPlanCount =
            await verificationContext
                .AlternativeRecommendationPlans
                .AsNoTracking()
                .CountAsync(
                    record =>
                        EF.Property<Guid>(
                            record,
                            "EventId") ==
                        integrationEvent.EventId);

        Assert.Equal(
            1,
            inboxMessageCount);

        Assert.Equal(
            1,
            recommendationPlanCount);

        var inboxMessage =
            await verificationContext
                .InboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id ==
                        integrationEvent.EventId);

        Assert.NotNull(
            inboxMessage.ProcessedAtUtc);
    }

    [Fact]
    public async Task
    ProcessAsync_ShouldPersistSingleRecommendationPlan_WhenSameEventIsProcessedTwice()
    {
        await using var database =
            await InboxTestDatabase.CreateAsync();

        var integrationEvent =
            CreateIntegrationEvent();

        var executor =
            new CountingAlternativeRecommendationExecutor();

        await using (var context =
                     database.CreateContext())
        {
            var repository =
                new AlternativeRecommendationPlanRepository(
                    context);

            var candidateProvider =
                new FixedAlternativeCandidateProvider(
                    integrationEvent.ProductId);

            var orchestrator =
                new StockDepletedRecommendationOrchestrator(
                    candidateProvider,
                    executor,
                    repository,
                    new FixedTimeProvider(
                        DateTimeOffset.UtcNow));

            var handler =
                new StockDepletedIntegrationEventHandler(
                    orchestrator,
                    NullLogger<
                        StockDepletedIntegrationEventHandler>
                        .Instance);

            var processor =
                new InboxIntegrationEventProcessor<
                    StockDepletedIntegrationEvent>(
                    context,
                    handler,
                    NullLogger<
                        InboxIntegrationEventProcessor<
                            StockDepletedIntegrationEvent>>
                        .Instance);

            var firstResult =
                await processor.ProcessAsync(
                    integrationEvent);

            var secondResult =
                await processor.ProcessAsync(
                    integrationEvent);

            Assert.Equal(
                IntegrationEventProcessingResult.Processed,
                firstResult);

            Assert.Equal(
                IntegrationEventProcessingResult.AlreadyProcessed,
                secondResult);

            Assert.Equal(
                1,
                executor.InvocationCount);
        }

        await using var verificationContext =
            database.CreateContext();

        var inboxMessageCount =
            await verificationContext
                .InboxMessages
                .AsNoTracking()
                .CountAsync(
                    message =>
                        message.Id ==
                        integrationEvent.EventId);

        var recommendationPlanCount =
            await verificationContext
                .AlternativeRecommendationPlans
                .AsNoTracking()
                .CountAsync(
                    record =>
                        EF.Property<Guid>(
                            record,
                            "EventId") ==
                        integrationEvent.EventId);

        Assert.Equal(
            1,
            inboxMessageCount);

        Assert.Equal(
            1,
            recommendationPlanCount);

        var inboxMessage =
            await verificationContext
                .InboxMessages
                .AsNoTracking()
                .SingleAsync(
                    message =>
                        message.Id ==
                        integrationEvent.EventId);

        Assert.NotNull(
            inboxMessage.ProcessedAtUtc);
    }

    [Fact]
    public async Task
        ProcessAsync_ShouldRollbackInboxAndRecommendationPlan_WhenHandlerFailsAfterOrchestration()
    {
        await using var database =
            await InboxTestDatabase.CreateAsync();

        var integrationEvent =
            CreateIntegrationEvent();

        await using (var context =
                     database.CreateContext())
        {
            var repository =
                new AlternativeRecommendationPlanRepository(
                    context);

            var candidateProvider =
                new FixedAlternativeCandidateProvider(
                    integrationEvent.ProductId);

            var executor =
                new FixedAlternativeRecommendationExecutor();

            var orchestrator =
                new StockDepletedRecommendationOrchestrator(
                    candidateProvider,
                    executor,
                    repository,
                    new FixedTimeProvider(
                        DateTimeOffset.UtcNow));

            var handler =
                new FailAfterOrchestrationHandler(
                    orchestrator);

            var processor =
                new InboxIntegrationEventProcessor<
                    StockDepletedIntegrationEvent>(
                    context,
                    handler,
                    NullLogger<
                        InboxIntegrationEventProcessor<
                            StockDepletedIntegrationEvent>>
                        .Instance);

            var exception =
                await Assert.ThrowsAsync<
                    InvalidOperationException>(
                    () =>
                        processor.ProcessAsync(
                            integrationEvent));

            Assert.Equal(
                "Simulated failure after recommendation orchestration.",
                exception.Message);

            Assert.Equal(
                1,
                handler.InvocationCount);
        }

        await using var verificationContext =
            database.CreateContext();

        var inboxMessageCount =
            await verificationContext
                .InboxMessages
                .AsNoTracking()
                .CountAsync(
                    message =>
                        message.Id ==
                        integrationEvent.EventId);

        var recommendationPlanCount =
            await verificationContext
                .AlternativeRecommendationPlans
                .AsNoTracking()
                .CountAsync(
                    record =>
                        EF.Property<Guid>(
                            record,
                            "EventId") ==
                        integrationEvent.EventId);

        Assert.Equal(
            0,
            inboxMessageCount);

        Assert.Equal(
            0,
            recommendationPlanCount);
    }

    private static StockDepletedIntegrationEvent
        CreateIntegrationEvent()
    {
        return new StockDepletedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            $"transaction-correlation-{Guid.NewGuid():N}");
    }

    private sealed class FixedAlternativeCandidateProvider
        : IAlternativeCandidateProvider
    {
        private readonly Guid
            _depletedProductId;

        public FixedAlternativeCandidateProvider(
            Guid depletedProductId)
        {
            _depletedProductId =
                depletedProductId;
        }

        public Task<AlternativeCandidateSet?>
            GetCandidateSetAsync(
                Guid depletedProductId,
                int limit,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Assert.Equal(
                _depletedProductId,
                depletedProductId);

            Assert.Equal(
                10,
                limit);

            return Task.FromResult<
                AlternativeCandidateSet?>(
                new AlternativeCandidateSet(
                    new DepletedProductContext(
                        depletedProductId,
                        "Depleted Product",
                        "Electronics"),
                    []));
        }
    }

    private sealed class FixedAlternativeRecommendationExecutor
        : IAlternativeRecommendationExecutor
    {
        public Task<
            AlternativeRecommendationGenerationOutcome>
            ExecuteAsync(
                AlternativeRecommendationRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            cancellationToken.ThrowIfCancellationRequested();

            Assert.Empty(
                request.Candidates);

            return Task.FromResult(
                new AlternativeRecommendationGenerationOutcome(
                    new AlternativeRecommendationResult(
                        []),
                    AlternativeRecommendationSource.Deterministic));
        }
    }

    private sealed class FailAfterOrchestrationHandler
        : Application.Abstractions.Messaging
            .IIntegrationEventHandler<
                StockDepletedIntegrationEvent>
    {
        private readonly
            IStockDepletedRecommendationOrchestrator
            _orchestrator;

        public FailAfterOrchestrationHandler(
            IStockDepletedRecommendationOrchestrator orchestrator)
        {
            _orchestrator =
                orchestrator
                ?? throw new ArgumentNullException(
                    nameof(orchestrator));
        }

        public int InvocationCount { get; private set; }

        public async Task HandleAsync(
            StockDepletedIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                integrationEvent);

            InvocationCount++;

            var plan =
                await _orchestrator.OrchestrateAsync(
                    integrationEvent.EventId,
                    integrationEvent.ProductId,
                    integrationEvent.CorrelationId,
                    cancellationToken);

            Assert.NotNull(
                plan);

            throw new InvalidOperationException(
                "Simulated failure after recommendation orchestration.");
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

    private sealed class CountingAlternativeRecommendationExecutor
    : IAlternativeRecommendationExecutor
    {
        private int
            _invocationCount;

        public int InvocationCount =>
            Volatile.Read(
                ref _invocationCount);

        public Task<
            AlternativeRecommendationGenerationOutcome>
            ExecuteAsync(
                AlternativeRecommendationRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            cancellationToken.ThrowIfCancellationRequested();

            Interlocked.Increment(
                ref _invocationCount);

            return Task.FromResult(
                new AlternativeRecommendationGenerationOutcome(
                    new AlternativeRecommendationResult(
                        []),
                    AlternativeRecommendationSource.Deterministic));
        }
    }
}