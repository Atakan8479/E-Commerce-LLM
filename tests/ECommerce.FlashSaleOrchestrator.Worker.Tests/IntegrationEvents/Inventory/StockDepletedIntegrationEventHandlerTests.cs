using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application
    .IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker
    .IntegrationEvents.Inventory;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.FlashSaleOrchestrator.Worker.Tests
    .IntegrationEvents.Inventory;

public sealed class StockDepletedIntegrationEventHandlerTests
{
    [Fact]
    public async Task
        HandleAsync_ShouldForwardEventDataAndCancellationTokenToOrchestrator()
    {
        var integrationEvent =
            CreateIntegrationEvent();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var orchestrator =
            new FakeStockDepletedRecommendationOrchestrator
            {
                Result =
                    CreatePlan(
                        integrationEvent)
            };

        var handler =
            CreateHandler(
                orchestrator);

        await handler.HandleAsync(
            integrationEvent,
            cancellationTokenSource.Token);

        Assert.Equal(
            1,
            orchestrator.CallCount);

        Assert.Equal(
            integrationEvent.EventId,
            orchestrator.ReceivedEventId);

        Assert.Equal(
            integrationEvent.ProductId,
            orchestrator.ReceivedProductId);

        Assert.Equal(
            integrationEvent.CorrelationId,
            orchestrator.ReceivedCorrelationId);

        Assert.Equal(
            cancellationTokenSource.Token,
            orchestrator.ReceivedCancellationToken);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldCompleteSuccessfully_WhenOrchestratorReturnsNull()
    {
        var orchestrator =
            new FakeStockDepletedRecommendationOrchestrator
            {
                Result =
                    null
            };

        var handler =
            CreateHandler(
                orchestrator);

        var exception =
            await Record.ExceptionAsync(
                () =>
                    handler.HandleAsync(
                        CreateIntegrationEvent()));

        Assert.Null(
            exception);

        Assert.Equal(
            1,
            orchestrator.CallCount);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldPropagateException_WhenOrchestrationFails()
    {
        var orchestrator =
            new FakeStockDepletedRecommendationOrchestrator
            {
                Exception =
                    new InvalidOperationException(
                        "Recommendation orchestration failed.")
            };

        var handler =
            CreateHandler(
                orchestrator);

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    handler.HandleAsync(
                        CreateIntegrationEvent()));

        Assert.Equal(
            "Recommendation orchestration failed.",
            exception.Message);

        Assert.Equal(
            1,
            orchestrator.CallCount);
    }

    private static StockDepletedIntegrationEventHandler
        CreateHandler(
            IStockDepletedRecommendationOrchestrator orchestrator)
    {
        return new StockDepletedIntegrationEventHandler(
            orchestrator,
            NullLogger<
                StockDepletedIntegrationEventHandler>
                .Instance);
    }

    private static StockDepletedIntegrationEvent
        CreateIntegrationEvent()
    {
        return new StockDepletedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            $"correlation-{Guid.NewGuid():N}");
    }

    private static AlternativeRecommendationPlan
        CreatePlan(
            StockDepletedIntegrationEvent integrationEvent)
    {
        return new AlternativeRecommendationPlan(
            integrationEvent.EventId,
            integrationEvent.ProductId,
            new AlternativeRecommendationResult(
                []),
            AlternativeRecommendationSource.Deterministic,
            integrationEvent.CorrelationId,
            DateTime.UtcNow);
    }

    private sealed class
        FakeStockDepletedRecommendationOrchestrator
        : IStockDepletedRecommendationOrchestrator
    {
        public AlternativeRecommendationPlan?
            Result
        {
            get;
            init;
        }

        public Exception?
            Exception
        {
            get;
            init;
        }

        public int CallCount { get; private set; }

        public Guid ReceivedEventId { get; private set; }

        public Guid ReceivedProductId { get; private set; }

        public string?
            ReceivedCorrelationId
        {
            get;
            private set;
        }

        public CancellationToken
            ReceivedCancellationToken
        {
            get;
            private set;
        }

        public Task<AlternativeRecommendationPlan?>
            OrchestrateAsync(
                Guid eventId,
                Guid depletedProductId,
                string correlationId,
                CancellationToken cancellationToken = default)
        {
            CallCount++;

            ReceivedEventId =
                eventId;

            ReceivedProductId =
                depletedProductId;

            ReceivedCorrelationId =
                correlationId;

            ReceivedCancellationToken =
                cancellationToken;

            if (Exception is not null)
            {
                return Task.FromException<
                    AlternativeRecommendationPlan?>(
                    Exception);
            }

            return Task.FromResult(
                Result);
        }
    }
}