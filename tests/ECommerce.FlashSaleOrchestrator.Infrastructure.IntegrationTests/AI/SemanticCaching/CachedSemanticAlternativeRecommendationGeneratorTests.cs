using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;
using Microsoft.Extensions.Logging.Abstractions;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI;

namespace ECommerce.FlashSaleOrchestrator
    .Infrastructure.IntegrationTests.AI.SemanticCaching;

public sealed class
    CachedSemanticAlternativeRecommendationGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ShouldReturnCachedResultWithoutCallingPrimary_WhenCacheHitIsValid()
    {
        var request =
            CreateRequest();

        var cachedResult =
            new AlternativeRecommendationResult(
                [
                    new AlternativeRecommendation(
                        request.Candidates[0].ProductId,
                        "Cached recommendation")
                ]);

        var primary =
            new FakePrimaryGenerator(
                CreatePrimaryResult(
                    request));

        var cache =
            new FakeSemanticRecommendationCache
            {
                MatchToReturn =
                    new SemanticRecommendationCacheMatch(
                        "entry-1",
                        0.97,
                        cachedResult)
            };

        var generator =
            CreateGenerator(
                primary,
                cache);

        var result =
            await generator.GenerateAsync(
                request);

        Assert.Same(
            cachedResult,
            result);

        Assert.Equal(
            0,
            primary.CallCount);

        Assert.Equal(
            1,
            cache.FindCount);

        Assert.Equal(
            0,
            cache.StoreCount);
    }

    [Fact]
    public async Task GenerateAsync_ShouldCallPrimaryAndStoreResult_WhenCacheMisses()
    {
        var request =
            CreateRequest();

        var primaryResult =
            CreatePrimaryResult(
                request);

        var primary =
            new FakePrimaryGenerator(
                primaryResult);

        var cache =
            new FakeSemanticRecommendationCache();

        var generator =
            CreateGenerator(
                primary,
                cache);

        var result =
            await generator.GenerateAsync(
                request);

        Assert.Same(
            primaryResult,
            result);

        Assert.Equal(
            1,
            primary.CallCount);

        Assert.Equal(
            1,
            cache.FindCount);

        Assert.Equal(
            1,
            cache.StoreCount);

        Assert.NotNull(
            cache.StoredEntry);

        Assert.Same(
            primaryResult,
            cache.StoredEntry.Result);

        Assert.Equal(
            "prompt-v1",
            cache.StoredEntry
                .Compatibility
                .PromptVersion);

        Assert.Equal(
            "schema-v1",
            cache.StoredEntry
                .Compatibility
                .SchemaVersion);

        Assert.Equal(
            "cache-v1",
            cache.StoredEntry
                .Compatibility
                .SemanticCacheVersion);

        Assert.Equal(
            "qwen3-embedding-0.6b-1024-v1",
            cache.StoredEntry
                .Compatibility
                .EmbeddingProfileVersion);
    }

    [Fact]
    public async Task GenerateAsync_ShouldRemoveInvalidHitAndUsePrimary()
    {
        var request =
            CreateRequest();

        var invalidCachedResult =
            new AlternativeRecommendationResult(
                [
                    new AlternativeRecommendation(
                        Guid.NewGuid(),
                        "Stale recommendation")
                ]);

        var primaryResult =
            CreatePrimaryResult(
                request);

        var primary =
            new FakePrimaryGenerator(
                primaryResult);

        var cache =
            new FakeSemanticRecommendationCache
            {
                MatchToReturn =
                    new SemanticRecommendationCacheMatch(
                        "stale-entry",
                        0.99,
                        invalidCachedResult)
            };

        var generator =
            CreateGenerator(
                primary,
                cache);

        var result =
            await generator.GenerateAsync(
                request);

        Assert.Same(
            primaryResult,
            result);

        Assert.Equal(
            1,
            primary.CallCount);

        Assert.Equal(
            1,
            cache.RemoveCount);

        Assert.Equal(
            "stale-entry",
            cache.LastRemovedEntryId);

        Assert.Equal(
            1,
            cache.StoreCount);
    }

    [Fact]
    public async Task GenerateAsync_ShouldUsePrimary_WhenEmbeddingGenerationFails()
    {
        var request =
            CreateRequest();

        var primaryResult =
            CreatePrimaryResult(
                request);

        var primary =
            new FakePrimaryGenerator(
                primaryResult);

        var cache =
            new FakeSemanticRecommendationCache();

        var embeddingGenerator =
            new FakeEmbeddingGenerator
            {
                ExceptionToThrow =
                    new InvalidOperationException(
                        "Embedding provider unavailable.")
            };

        var generator =
            CreateGenerator(
                primary,
                cache,
                embeddingGenerator);

        var result =
            await generator.GenerateAsync(
                request);

        Assert.Same(
            primaryResult,
            result);

        Assert.Equal(
            1,
            primary.CallCount);

        Assert.Equal(
            1,
            embeddingGenerator.CallCount);

        Assert.Equal(
            0,
            cache.FindCount);

        Assert.Equal(
            0,
            cache.StoreCount);
    }

    [Fact]
    public async Task GenerateAsync_ShouldUsePrimary_WhenCacheLookupFails()
    {
        var request =
            CreateRequest();

        var primaryResult =
            CreatePrimaryResult(
                request);

        var primary =
            new FakePrimaryGenerator(
                primaryResult);

        var cache =
            new FakeSemanticRecommendationCache
            {
                FindException =
                    new InvalidOperationException(
                        "Cache unavailable.")
            };

        var generator =
            CreateGenerator(
                primary,
                cache);

        var result =
            await generator.GenerateAsync(
                request);

        Assert.Same(
            primaryResult,
            result);

        Assert.Equal(
            1,
            primary.CallCount);

        Assert.Equal(
            1,
            cache.FindCount);

        Assert.Equal(
            0,
            cache.StoreCount);
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturnPrimaryResult_WhenCacheStoreFails()
    {
        var request =
            CreateRequest();

        var primaryResult =
            CreatePrimaryResult(
                request);

        var primary =
            new FakePrimaryGenerator(
                primaryResult);

        var cache =
            new FakeSemanticRecommendationCache
            {
                StoreException =
                    new InvalidOperationException(
                        "Cache write failed.")
            };

        var generator =
            CreateGenerator(
                primary,
                cache);

        var result =
            await generator.GenerateAsync(
                request);

        Assert.Same(
            primaryResult,
            result);

        Assert.Equal(
            1,
            primary.CallCount);

        Assert.Equal(
            1,
            cache.FindCount);

        Assert.Equal(
            1,
            cache.StoreCount);

        Assert.Null(
            cache.StoredEntry);
    }

    [Fact]
    public async Task GenerateAsync_ShouldUsePrimary_WhenInvalidCacheEntryCannotBeRemoved()
    {
        var request =
            CreateRequest();

        var invalidCachedResult =
            new AlternativeRecommendationResult(
                [
                    new AlternativeRecommendation(
                        Guid.NewGuid(),
                        "Stale recommendation")
                ]);

        var primaryResult =
            CreatePrimaryResult(
                request);

        var primary =
            new FakePrimaryGenerator(
                primaryResult);

        var cache =
            new FakeSemanticRecommendationCache
            {
                MatchToReturn =
                    new SemanticRecommendationCacheMatch(
                        "stale-entry",
                        0.99,
                        invalidCachedResult),

                RemoveException =
                    new InvalidOperationException(
                        "Cache remove failed.")
            };

        var generator =
            CreateGenerator(
                primary,
                cache);

        var result =
            await generator.GenerateAsync(
                request);

        Assert.Same(
            primaryResult,
            result);

        Assert.Equal(
            1,
            primary.CallCount);

        Assert.Equal(
            1,
            cache.RemoveCount);

        Assert.Equal(
            0,
            cache.StoreCount);

        Assert.Null(
            cache.LastRemovedEntryId);
    }

    [Fact]
    public async Task GenerateAsync_ShouldPropagateCancellation()
    {
        var request =
            CreateRequest();

        var primary =
            new FakePrimaryGenerator(
                CreatePrimaryResult(
                    request));

        var cache =
            new FakeSemanticRecommendationCache();

        var embeddingGenerator =
            new FakeEmbeddingGenerator();

        var generator =
            CreateGenerator(
                primary,
                cache,
                embeddingGenerator);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            () =>
                generator.GenerateAsync(
                    request,
                    cancellationTokenSource.Token));

        Assert.Equal(
            0,
            primary.CallCount);

        Assert.Equal(
            0,
            embeddingGenerator.CallCount);

        Assert.Equal(
            0,
            cache.FindCount);

        Assert.Equal(
            0,
            cache.StoreCount);

        Assert.Equal(
            0,
            cache.RemoveCount);
    }

    private static
        CachedSemanticAlternativeRecommendationGenerator
        CreateGenerator(
            FakePrimaryGenerator primary,
            FakeSemanticRecommendationCache cache,
            ISemanticRecommendationEmbeddingGenerator?
                embeddingGenerator = null)
    {
        return new CachedSemanticAlternativeRecommendationGenerator(
            primary,
            embeddingGenerator ??
                new FakeEmbeddingGenerator(),
            cache,
            new SemanticRecommendationRepresentationBuilder(),
            new SemanticRecommendationCacheProfile(
                "prompt-v1",
                "schema-v1",
                "cache-v1",
                "qwen3-embedding-0.6b-1024-v1"),
            NullLogger<
                CachedSemanticAlternativeRecommendationGenerator>
                .Instance);
    }

    private static AlternativeRecommendationRequest
        CreateRequest()
    {
        return new AlternativeRecommendationRequest(
            "correlation-1",
            new DepletedProductContext(
                Guid.Parse(
                    "11111111-1111-1111-1111-111111111111"),
                "Wireless Gaming Mouse",
                "gaming-mouse"),
            [
                new AlternativeCandidate(
                    Guid.Parse(
                        "22222222-2222-2222-2222-222222222222"),
                    "Wireless Gaming Mouse Pro",
                    "gaming-mouse",
                    15),

                new AlternativeCandidate(
                    Guid.Parse(
                        "44444444-4444-4444-4444-444444444444"),
                    "RGB Gaming Mouse",
                    "gaming-mouse",
                    8)
            ]);
    }

    private static AlternativeRecommendationResult
        CreatePrimaryResult(
            AlternativeRecommendationRequest request)
    {
        return new AlternativeRecommendationResult(
            [
                new AlternativeRecommendation(
                    request.Candidates[0].ProductId,
                    "Primary recommendation")
            ]);
    }

    private sealed class FakePrimaryGenerator
        : IUncachedAlternativeRecommendationGenerator
    {
        private readonly AlternativeRecommendationResult
            _result;

        public FakePrimaryGenerator(
            AlternativeRecommendationResult result)
        {
            _result =
                result;
        }

        public int CallCount { get; private set; }

        public Task<AlternativeRecommendationResult>
            GenerateAsync(
                AlternativeRecommendationRequest request,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;

            return Task.FromResult(
                _result);
        }
    }

    private sealed class FakeEmbeddingGenerator
        : ISemanticRecommendationEmbeddingGenerator
    {
        public Exception?
            ExceptionToThrow
        { get; init; }

        public int CallCount { get; private set; }

        public Task<SemanticRecommendationEmbedding>
            GenerateAsync(
                string text,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CallCount++;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

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

    private sealed class FakeSemanticRecommendationCache
        : ISemanticRecommendationCache
    {
        public SemanticRecommendationCacheMatch?
            MatchToReturn
        { get; init; }

        public Exception?
            FindException
        { get; init; }

        public Exception?
            StoreException
        { get; init; }

        public Exception?
            RemoveException
        { get; init; }

        public SemanticRecommendationCacheEntry?
            StoredEntry
        { get; private set; }

        public string?
            LastRemovedEntryId
        { get; private set; }

        public int FindCount { get; private set; }

        public int StoreCount { get; private set; }

        public int RemoveCount { get; private set; }

        public Task<SemanticRecommendationCacheMatch?>
            FindAsync(
                SemanticRecommendationCacheLookup lookup,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FindCount++;

            if (FindException is not null)
            {
                throw FindException;
            }

            return Task.FromResult(
                MatchToReturn);
        }

        public Task StoreAsync(
            SemanticRecommendationCacheEntry entry,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            StoreCount++;

            if (StoreException is not null)
            {
                throw StoreException;
            }

            StoredEntry =
                entry;

            return Task.CompletedTask;
        }

        public Task RemoveAsync(
            string entryId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RemoveCount++;

            if (RemoveException is not null)
            {
                throw RemoveException;
            }

            LastRemovedEntryId =
                entryId;

            return Task.CompletedTask;
        }
    }
}