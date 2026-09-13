using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI;
using Microsoft.Extensions.Logging.Abstractions;
using ECommerce.FlashSaleOrchestrator.Application .AlternativeRecommendations.SemanticCaching;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI.SemanticCaching;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.AI;

public sealed class ResilientAlternativeRecommendationGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ShouldRetryAndReturnLlmResult_WhenSecondAttemptSucceeds()
    {
        var candidateId =
            Guid.NewGuid();

        var validResponse =
            $$"""
              {
                "recommendations": [
                  {
                    "productId": "{{candidateId}}",
                    "reason": "Suitable semantic alternative."
                  }
                ]
              }
              """;

        var fakeChatCompletionService =
            new FakeChatCompletionService(
                callCount =>
                    callCount == 1
                        ? "{ invalid-json"
                        : validResponse);

        var generator =
            CreateGenerator(
                fakeChatCompletionService);

        var result =
            await generator.GenerateAsync(
                CreateRequest(
                    candidateId));

        Assert.Equal(
            2,
            fakeChatCompletionService.CallCount);

        var recommendation =
            Assert.Single(
                result.Recommendations);

        Assert.Equal(
            candidateId,
            recommendation.ProductId);

        Assert.Equal(
            "Suitable semantic alternative.",
            recommendation.Reason);
    }

    [Fact]
    public async Task GenerateAsync_ShouldUseDeterministicFallback_WhenAllLlmAttemptsFail()
    {
        var candidateId =
            Guid.NewGuid();

        var fakeChatCompletionService =
            new FakeChatCompletionService(
                "{ invalid-json");

        var generator =
            CreateGenerator(
                fakeChatCompletionService);

        var result =
            await generator.GenerateAsync(
                CreateRequest(
                    candidateId));

        Assert.Equal(
            2,
            fakeChatCompletionService.CallCount);

        var recommendation =
            Assert.Single(
                result.Recommendations);

        Assert.Equal(
            candidateId,
            recommendation.ProductId);

        Assert.Equal(
            "Selected from the validated deterministic candidate set.",
            recommendation.Reason);
    }

    [Fact]
    public async Task GenerateAsync_ShouldUseDeterministicFallback_WhenLlmTransportKeepsFailing()
    {
        var candidateId =
            Guid.NewGuid();

        var fakeChatCompletionService =
            new FakeChatCompletionService(
                _ =>
                    throw new HttpRequestException(
                        "Simulated model transport failure."));

        var generator =
            CreateGenerator(
                fakeChatCompletionService);

        var result =
            await generator.GenerateAsync(
                CreateRequest(
                    candidateId));

        Assert.Equal(
            2,
            fakeChatCompletionService.CallCount);

        var recommendation =
            Assert.Single(
                result.Recommendations);

        Assert.Equal(
            candidateId,
            recommendation.ProductId);
    }

    [Fact]
    public async Task GenerateAsync_ShouldNotCallLlm_WhenNoCandidatesExist()
    {
        var fakeChatCompletionService =
            new FakeChatCompletionService(
                _ =>
                    throw new InvalidOperationException(
                        "LLM should not be called."));

        var generator =
            CreateGenerator(
                fakeChatCompletionService);

        var request =
            new AlternativeRecommendationRequest(
                "empty-candidate-correlation",
                new DepletedProductContext(
                    Guid.NewGuid(),
                    "Depleted Product",
                    "Electronics"),
                []);

        var result =
            await generator.GenerateAsync(
                request);

        Assert.Empty(
            result.Recommendations);

        Assert.Equal(
            0,
            fakeChatCompletionService.CallCount);
    }

    [Fact]
    public async Task GenerateAsync_ShouldPropagateCancellation_WithoutFallback()
    {
        var candidateId =
            Guid.NewGuid();

        var fakeChatCompletionService =
            new FakeChatCompletionService(
                """
                {
                  "recommendations": []
                }
                """);

        var generator =
            CreateGenerator(
                fakeChatCompletionService);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            () =>
                generator.GenerateAsync(
                    CreateRequest(
                        candidateId),
                    cancellationTokenSource.Token));

        Assert.Equal(
            0,
            fakeChatCompletionService.CallCount);
    }

    private static
        ResilientAlternativeRecommendationGenerator
        CreateGenerator(
            FakeChatCompletionService fakeChatCompletionService)
    {
        var uncachedGenerator =
            new SemanticKernelAlternativeRecommendationGenerator(
                fakeChatCompletionService);

        var cachedGenerator =
            new CachedSemanticAlternativeRecommendationGenerator(
                uncachedGenerator,
                new FakeEmbeddingGenerator(),
                new CacheMissSemanticRecommendationCache(),
                new SemanticRecommendationRepresentationBuilder(),
                new SemanticRecommendationCacheProfile(
                    "prompt-v1",
                    "schema-v1",
                    "cache-v1",
                    "embedding-v1"),
                NullLogger<
                    CachedSemanticAlternativeRecommendationGenerator>
                    .Instance);

        var fallbackGenerator =
            new DeterministicAlternativeRecommendationGenerator();

        return new ResilientAlternativeRecommendationGenerator(
            cachedGenerator,
            fallbackGenerator,
            NullLogger<
                ResilientAlternativeRecommendationGenerator>
                .Instance);
    }

    private sealed class FakeEmbeddingGenerator
    : ISemanticRecommendationEmbeddingGenerator
    {
        public Task<SemanticRecommendationEmbedding>
            GenerateAsync(
                string text,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                new SemanticRecommendationEmbedding(
                    new float[]
                    {
                    0.1f,
                    0.2f,
                    0.3f
                    }));
        }
    }

    private sealed class CacheMissSemanticRecommendationCache
        : ISemanticRecommendationCache
    {
        public Task<SemanticRecommendationCacheMatch?>
            FindAsync(
                SemanticRecommendationCacheLookup lookup,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<
                SemanticRecommendationCacheMatch?>(
                null);
        }

        public Task StoreAsync(
            SemanticRecommendationCacheEntry entry,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }

        public Task RemoveAsync(
            string entryId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }

    private static AlternativeRecommendationRequest CreateRequest(
        Guid candidateId)
    {
        return new AlternativeRecommendationRequest(
            "resilient-recommendation-correlation",
            new DepletedProductContext(
                Guid.NewGuid(),
                "Depleted Product",
                "Electronics"),
            [
                new AlternativeCandidate(
                    candidateId,
                    "Alternative Product",
                    "Electronics",
                    12)
            ]);
    }
}