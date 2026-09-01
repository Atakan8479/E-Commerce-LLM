using ECommerce.FlashSaleOrchestrator.Application.Abstractions.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;
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

        var candidateProductId =
            Guid.NewGuid();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var provider =
            new FakeAlternativeCandidateProvider
            {
                Result =
                [
                    new AlternativeCandidate(
                        candidateProductId,
                        "Alternative Mouse",
                        "mouse",
                        12)
                ]
            };

        var generator =
            new FakeAlternativeRecommendationGenerator();

        var handler =
            CreateHandler(
                provider,
                generator);

        var integrationEvent =
            CreateIntegrationEvent(
                productId);

        await handler.HandleAsync(
            integrationEvent,
            cancellationTokenSource.Token);

        Assert.Equal(
            1,
            provider.CandidateSetCallCount);

        Assert.Equal(
            productId,
            provider.ReceivedProductId);

        Assert.Equal(
            10,
            provider.ReceivedLimit);

        Assert.Equal(
            cancellationTokenSource.Token,
            provider.ReceivedCancellationToken);

        Assert.Equal(
            1,
            generator.CallCount);

        Assert.NotNull(
            generator.ReceivedRequest);

        Assert.Equal(
            integrationEvent.CorrelationId,
            generator.ReceivedRequest.CorrelationId);

        Assert.Equal(
            productId,
            generator.ReceivedRequest.DepletedProduct.ProductId);

        Assert.Equal(
            "Depleted Mouse",
            generator.ReceivedRequest.DepletedProduct.Name);

        Assert.Equal(
            "mouse",
            generator.ReceivedRequest.DepletedProduct.Category);

        var receivedCandidate =
            Assert.Single(
                generator.ReceivedRequest.Candidates);

        Assert.Equal(
            candidateProductId,
            receivedCandidate.ProductId);

        Assert.Equal(
            cancellationTokenSource.Token,
            generator.ReceivedCancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldCompleteSuccessfully_WhenNoCandidatesAreFound()
    {
        var provider =
            new FakeAlternativeCandidateProvider
            {
                Result = []
            };

        var generator =
            new FakeAlternativeRecommendationGenerator();

        var handler =
            CreateHandler(
                provider,
                generator);

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
            provider.CandidateSetCallCount);

        Assert.Equal(
            0,
            generator.CallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldCompleteSuccessfully_WhenDepletedProductDoesNotExist()
    {
        var provider =
            new FakeAlternativeCandidateProvider
            {
                ReturnNullCandidateSet =
                    true
            };

        var generator =
            new FakeAlternativeRecommendationGenerator();

        var handler =
            CreateHandler(
                provider,
                generator);

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
            provider.CandidateSetCallCount);

        Assert.Equal(
            0,
            generator.CallCount);
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

        var generator =
            new FakeAlternativeRecommendationGenerator();

        var handler =
            CreateHandler(
                provider,
                generator);

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
            provider.CandidateSetCallCount);

        Assert.Equal(
            0,
            generator.CallCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldPropagateException_WhenRecommendationGenerationFails()
    {
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

        var generator =
            new FakeAlternativeRecommendationGenerator
            {
                Exception =
                    new InvalidOperationException(
                        "Recommendation generation failed.")
            };

        var handler =
            CreateHandler(
                provider,
                generator);

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
            "Recommendation generation failed.",
            exception.Message);

        Assert.Equal(
            1,
            provider.CandidateSetCallCount);

        Assert.Equal(
            1,
            generator.CallCount);
    }

    private static StockDepletedIntegrationEventHandler CreateHandler(
        IAlternativeCandidateProvider provider,
        IAlternativeRecommendationGenerator generator)
    {
        return new StockDepletedIntegrationEventHandler(
            provider,
            generator,
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

        public bool ReturnNullCandidateSet { get; init; }

        public int CandidateSetCallCount { get; private set; }

        public Guid ReceivedProductId { get; private set; }

        public int ReceivedLimit { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<AlternativeCandidateSet?> GetCandidateSetAsync(
            Guid depletedProductId,
            int limit,
            CancellationToken cancellationToken = default)
        {
            CandidateSetCallCount++;

            ReceivedProductId =
                depletedProductId;

            ReceivedLimit =
                limit;

            ReceivedCancellationToken =
                cancellationToken;

            if (Exception is not null)
            {
                return Task.FromException<AlternativeCandidateSet?>(
                    Exception);
            }

            if (ReturnNullCandidateSet)
            {
                return Task.FromResult<AlternativeCandidateSet?>(
                    null);
            }

            AlternativeCandidateSet result =
                new(
                    new DepletedProductContext(
                        depletedProductId,
                        "Depleted Mouse",
                        "mouse"),
                    Result);

            return Task.FromResult<AlternativeCandidateSet?>(
                result);
        }
    }

    private sealed class FakeAlternativeRecommendationGenerator
        : IAlternativeRecommendationGenerator
    {
        public AlternativeRecommendationResult Result { get; init; } =
            new([]);

        public Exception? Exception { get; init; }

        public int CallCount { get; private set; }

        public AlternativeRecommendationRequest? ReceivedRequest { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<AlternativeRecommendationResult> GenerateAsync(
            AlternativeRecommendationRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            ReceivedRequest =
                request;

            ReceivedCancellationToken =
                cancellationToken;

            if (Exception is not null)
            {
                return Task.FromException<
                    AlternativeRecommendationResult>(
                    Exception);
            }

            return Task.FromResult(
                Result);
        }
    }
}