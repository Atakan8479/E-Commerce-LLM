using ECommerce.FlashSaleOrchestrator.Application.Abstractions.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker.IntegrationEvents.Inventory;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.FlashSaleOrchestrator.Worker.Tests.IntegrationEvents.Inventory;

public sealed class StockDepletedIntegrationEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRequestCandidates_WithEventProductIdAndConfiguredLimit()
    {
        var productId =
            Guid.NewGuid();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var provider =
            new FakeAlternativeCandidateProvider
            {
                Result =
                [
                    new AlternativeCandidate(
                        Guid.NewGuid(),
                        "Alternative Mouse",
                        "mouse",
                        12)
                ]
            };

        var handler =
            CreateHandler(
                provider);

        var integrationEvent =
            CreateIntegrationEvent(
                productId);

        await handler.HandleAsync(
            integrationEvent,
            cancellationTokenSource.Token);

        Assert.Equal(
            1,
            provider.CallCount);

        Assert.Equal(
            productId,
            provider.ReceivedProductId);

        Assert.Equal(
            10,
            provider.ReceivedLimit);

        Assert.Equal(
            cancellationTokenSource.Token,
            provider.ReceivedCancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldCompleteSuccessfully_WhenNoCandidatesAreFound()
    {
        var provider =
            new FakeAlternativeCandidateProvider
            {
                Result = []
            };

        var handler =
            CreateHandler(
                provider);

        var integrationEvent =
            CreateIntegrationEvent(
                Guid.NewGuid());

        var exception =
            await Record.ExceptionAsync(
                () =>
                    handler.HandleAsync(
                        integrationEvent));

        Assert.Null(
            exception);

        Assert.Equal(
            1,
            provider.CallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldPropagateException_WhenCandidateRetrievalFails()
    {
        var provider =
            new FakeAlternativeCandidateProvider
            {
                Exception =
                    new InvalidOperationException(
                        "Candidate retrieval failed.")
            };

        var handler =
            CreateHandler(
                provider);

        var integrationEvent =
            CreateIntegrationEvent(
                Guid.NewGuid());

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    handler.HandleAsync(
                        integrationEvent));

        Assert.Equal(
            "Candidate retrieval failed.",
            exception.Message);

        Assert.Equal(
            1,
            provider.CallCount);
    }

    private static StockDepletedIntegrationEventHandler CreateHandler(
        IAlternativeCandidateProvider provider)
    {
        return new StockDepletedIntegrationEventHandler(
            provider,
            NullLogger<
                StockDepletedIntegrationEventHandler>.Instance);
    }

    private static StockDepletedIntegrationEvent CreateIntegrationEvent(
        Guid productId)
    {
        return new StockDepletedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            productId,
            $"correlation-{Guid.NewGuid():N}");
    }

    private sealed class FakeAlternativeCandidateProvider
        : IAlternativeCandidateProvider
    {
        public IReadOnlyList<AlternativeCandidate> Result { get; init; } =
            [];

        public Exception? Exception { get; init; }

        public int CallCount { get; private set; }

        public Guid ReceivedProductId { get; private set; }

        public int ReceivedLimit { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<IReadOnlyList<AlternativeCandidate>> GetCandidatesAsync(
            Guid depletedProductId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            ReceivedProductId =
                depletedProductId;

            ReceivedLimit =
                limit;

            ReceivedCancellationToken =
                cancellationToken;

            if (Exception is not null)
            {
                return Task.FromException<
                    IReadOnlyList<AlternativeCandidate>>(
                    Exception);
            }

            return Task.FromResult(
                Result);
        }
    }
}