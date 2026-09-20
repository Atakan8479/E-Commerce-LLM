using System.Runtime.CompilerServices;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ECommerce.FlashSaleOrchestrator
    .Infrastructure.IntegrationTests.AI;

public sealed class
    ResilientAlternativeRecommendationTimeoutTests
{
    [Fact]
    public async Task
        ExecuteAsync_ShouldRetryTimedOutLlmAndUseDeterministicFallback()
    {
        var candidateId =
            Guid.NewGuid();

        var chatCompletionService =
            new BlockingChatCompletionService();

        var generator =
            CreateGenerator(
                chatCompletionService,
                TimeSpan.FromMilliseconds(
                    50));

        var outcome =
            await generator.ExecuteAsync(
                CreateRequest(
                    candidateId));

        Assert.Equal(
            AlternativeRecommendationSource.Deterministic,
            outcome.Source);

        Assert.Equal(
            2,
            chatCompletionService.CallCount);

        var recommendation =
            Assert.Single(
                outcome.Result.Recommendations);

        Assert.Equal(
            candidateId,
            recommendation.ProductId);

        Assert.Equal(
            "Selected from the validated deterministic candidate set.",
            recommendation.Reason);
    }

    [Fact]
    public async Task
        ExecuteAsync_ShouldPropagateCallerCancellationWithoutRetryOrFallback()
    {
        var candidateId =
            Guid.NewGuid();

        var chatCompletionService =
            new BlockingChatCompletionService();

        var generator =
            CreateGenerator(
                chatCompletionService,
                TimeSpan.FromSeconds(
                    5));

        using var cancellationTokenSource =
            new CancellationTokenSource(
                TimeSpan.FromMilliseconds(
                    50));

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            () =>
                generator.ExecuteAsync(
                    CreateRequest(
                        candidateId),
                    cancellationTokenSource.Token));

        Assert.Equal(
            1,
            chatCompletionService.CallCount);
    }

    private static
        ResilientAlternativeRecommendationGenerator
        CreateGenerator(
            BlockingChatCompletionService
                chatCompletionService,
            TimeSpan requestTimeout)
    {
        var semanticKernelGenerator =
            new SemanticKernelAlternativeRecommendationGenerator(
                chatCompletionService);

        var timeoutGenerator =
            new TimeoutUncachedAlternativeRecommendationGenerator(
                semanticKernelGenerator,
                new AlternativeRecommendationAiOptions(
                    requestTimeout));

        var cachedGenerator =
            new CachedSemanticAlternativeRecommendationGenerator(
                timeoutGenerator,
                new FakeEmbeddingGenerator(),
                new CacheMissSemanticRecommendationCache(),
                new SemanticRecommendationRepresentationBuilder(),
                new SemanticRecommendationCacheProfile(
                    "timeout-prompt-v1",
                    "timeout-schema-v1",
                    "timeout-cache-v1",
                    "timeout-embedding-v1"),
                NullLogger<
                    CachedSemanticAlternativeRecommendationGenerator>
                    .Instance);

        return new ResilientAlternativeRecommendationGenerator(
            cachedGenerator,
            new DeterministicAlternativeRecommendationGenerator(),
            NullLogger<
                ResilientAlternativeRecommendationGenerator>
                .Instance);
    }

    private static AlternativeRecommendationRequest
        CreateRequest(
            Guid candidateId)
    {
        return new AlternativeRecommendationRequest(
            $"resilience-timeout-{Guid.NewGuid():N}",
            new DepletedProductContext(
                Guid.NewGuid(),
                "Depleted Product",
                "Electronics"),
            [
                new AlternativeCandidate(
                    candidateId,
                    "Alternative Product",
                    "Electronics",
                    10)
            ]);
    }

    private sealed class FakeEmbeddingGenerator
        : ISemanticRecommendationEmbeddingGenerator
    {
        public Task<SemanticRecommendationEmbedding>
            GenerateAsync(
                string text,
                CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                text);

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

    private sealed class
        CacheMissSemanticRecommendationCache
        : ISemanticRecommendationCache
    {
        public Task<SemanticRecommendationCacheMatch?>
            FindAsync(
                SemanticRecommendationCacheLookup lookup,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                lookup);

            cancellationToken.ThrowIfCancellationRequested();

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

    private sealed class BlockingChatCompletionService
        : IChatCompletionService
    {
        public IReadOnlyDictionary<string, object?>
            Attributes
        { get; } =
                new Dictionary<string, object?>();

        public int CallCount { get; private set; }

        public async Task<
            IReadOnlyList<ChatMessageContent>>
            GetChatMessageContentsAsync(
                ChatHistory chatHistory,
                PromptExecutionSettings? executionSettings = null,
                Kernel? kernel = null,
                CancellationToken cancellationToken = default)
        {
            CallCount++;

            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                cancellationToken);

            return Array.Empty<
                ChatMessageContent>();
        }

        public async IAsyncEnumerable<
            StreamingChatMessageContent>
            GetStreamingChatMessageContentsAsync(
                ChatHistory chatHistory,
                PromptExecutionSettings? executionSettings = null,
                Kernel? kernel = null,
                [EnumeratorCancellation]
                CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            yield break;
        }
    }
}