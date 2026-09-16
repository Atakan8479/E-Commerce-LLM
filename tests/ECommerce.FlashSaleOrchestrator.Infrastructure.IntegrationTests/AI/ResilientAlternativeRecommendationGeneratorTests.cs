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
    public async Task GenerateAsync_ShouldNotCacheDeterministicFallback_WhenAllLlmAttemptsFail()
    {
        var candidateId =
            Guid.NewGuid();

        var fakeChatCompletionService =
            new FakeChatCompletionService(
                "{ invalid-json");

        var cache =
            new CacheMissSemanticRecommendationCache();

        var generator =
            CreateGenerator(
                fakeChatCompletionService,
                cache);

        var result =
            await generator.GenerateAsync(
                CreateRequest(
                    candidateId));

        Assert.Equal(
            2,
            fakeChatCompletionService.CallCount);

        Assert.Equal(
            2,
            cache.FindCount);

        Assert.Equal(
            0,
            cache.StoreCount);

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
    public async Task ExecuteAsync_ShouldReportDeterministic_WhenNoCandidatesExist()
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
                "empty-candidate-source-correlation",
                new DepletedProductContext(
                    Guid.NewGuid(),
                    "Depleted Product",
                    "Electronics"),
                []);

        var outcome =
            await generator.ExecuteAsync(
                request);

        Assert.Equal(
            AlternativeRecommendationSource.Deterministic,
            outcome.Source);

        Assert.Empty(
            outcome.Result.Recommendations);

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

    [Fact]
    public async Task ExecuteAsync_ShouldReportLlm_WhenPrimarySucceeds()
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
                validResponse);

        var generator =
            CreateGenerator(
                fakeChatCompletionService);

        var outcome =
            await generator.ExecuteAsync(
                CreateRequest(
                    candidateId));

        Assert.Equal(
            AlternativeRecommendationSource.Llm,
            outcome.Source);

        var recommendation =
            Assert.Single(
                outcome.Result.Recommendations);

        Assert.Equal(
            candidateId,
            recommendation.ProductId);
    }

    [Fact]
public async Task ExecuteAsync_ShouldReportCache_WhenSemanticCacheHitIsValid()
{
    var candidateId =
        Guid.NewGuid();

    var request =
        CreateRequest(
            candidateId);

    var cachedResult =
        new AlternativeRecommendationResult(
            [
                new AlternativeRecommendation(
                    candidateId,
                    "Cached semantic alternative.")
            ]);

    var cache =
        new CacheHitSemanticRecommendationCache(
            cachedResult);

    var fakeChatCompletionService =
        new FakeChatCompletionService(
            _ =>
                throw new InvalidOperationException(
                    "LLM should not be called on a valid cache hit."));

    var generator =
        CreateGenerator(
            fakeChatCompletionService,
            cache);

    var outcome =
        await generator.ExecuteAsync(
            request);

    Assert.Equal(
        AlternativeRecommendationSource.Cache,
        outcome.Source);

    Assert.Same(
        cachedResult,
        outcome.Result);

    Assert.Equal(
        1,
        cache.FindCount);

    Assert.Equal(
        0,
        fakeChatCompletionService.CallCount);
}

    [Fact]
    public async Task ExecuteAsync_ShouldReportDeterministic_WhenAllLlmAttemptsFail()
    {
        var candidateId =
            Guid.NewGuid();

        var fakeChatCompletionService =
            new FakeChatCompletionService(
                "{ invalid-json");

        var generator =
            CreateGenerator(
                fakeChatCompletionService);

        var outcome =
            await generator.ExecuteAsync(
                CreateRequest(
                    candidateId));

        Assert.Equal(
            AlternativeRecommendationSource.Deterministic,
            outcome.Source);

        Assert.Equal(
            2,
            fakeChatCompletionService.CallCount);

        var recommendation =
            Assert.Single(
                outcome.Result.Recommendations);

        Assert.Equal(
            candidateId,
            recommendation.ProductId);
    }

    private static
    ResilientAlternativeRecommendationGenerator
    CreateGenerator(
        FakeChatCompletionService fakeChatCompletionService,
        ISemanticRecommendationCache? cache = null)
    {
        var uncachedGenerator =
            new SemanticKernelAlternativeRecommendationGenerator(
                fakeChatCompletionService);

        var cachedGenerator =
            new CachedSemanticAlternativeRecommendationGenerator(
                uncachedGenerator,
                new FakeEmbeddingGenerator(),
                cache ??
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

    private sealed class CacheHitSemanticRecommendationCache
    : ISemanticRecommendationCache
    {
        private readonly AlternativeRecommendationResult
            _result;

        public CacheHitSemanticRecommendationCache(
            AlternativeRecommendationResult result)
        {
            ArgumentNullException.ThrowIfNull(
                result);

            _result =
                result;
        }

        public int FindCount { get; private set; }

        public Task<SemanticRecommendationCacheMatch?>
            FindAsync(
                SemanticRecommendationCacheLookup lookup,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                lookup);

            cancellationToken.ThrowIfCancellationRequested();

            FindCount++;

            return Task.FromResult<
                SemanticRecommendationCacheMatch?>(
                new SemanticRecommendationCacheMatch(
                    "resilient-cache-hit",
                    0.99,
                    _result));
        }

        public Task StoreAsync(
            SemanticRecommendationCacheEntry entry,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                entry);

            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }

        public Task RemoveAsync(
            string entryId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                entryId);

            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }

    private sealed class CacheMissSemanticRecommendationCache
    : ISemanticRecommendationCache
    {
        public int FindCount { get; private set; }

        public int StoreCount { get; private set; }

        public int RemoveCount { get; private set; }

        public Task<SemanticRecommendationCacheMatch?>
            FindAsync(
                SemanticRecommendationCacheLookup lookup,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                lookup);

            cancellationToken.ThrowIfCancellationRequested();

            FindCount++;

            return Task.FromResult<
                SemanticRecommendationCacheMatch?>(
                null);
        }

        public Task StoreAsync(
            SemanticRecommendationCacheEntry entry,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                entry);

            cancellationToken.ThrowIfCancellationRequested();

            StoreCount++;

            return Task.CompletedTask;
        }

        public Task RemoveAsync(
            string entryId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                entryId);

            cancellationToken.ThrowIfCancellationRequested();

            RemoveCount++;

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