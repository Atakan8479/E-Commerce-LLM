using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.FlashSaleOrchestrator
    .Infrastructure.IntegrationTests.AI;

public sealed class
    AlternativeRecommendationAiDependencyInjectionTests
{
    [Fact]
    public void
        ProductionComposition_ShouldResolveRecommendationGeneratorGraph()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddSingleton<
            ISemanticRecommendationEmbeddingGenerator,
            StubSemanticRecommendationEmbeddingGenerator>();

        services.AddSemanticRecommendationCache(
            redisEndpoint:
                "127.0.0.1:6379",
            redisPassword:
                "integration-test-password",
            connectTimeout:
                TimeSpan.FromSeconds(
                    3),
            operationTimeout:
                TimeSpan.FromSeconds(
                    2),
            indexName:
                "integration-test:semantic-cache:idx",
                    keyPrefix:
                "integration-test:semantic-cache:",
            vectorDimensions:
                3,
            similarityThreshold:
                0.90,
            entryTimeToLive:
                TimeSpan.FromMinutes(5),
            promptVersion:
                "integration-test-prompt-v1",
            schemaVersion:
                "integration-test-schema-v1",
            semanticCacheVersion:
                "integration-test-cache-v1",
            embeddingProfileVersion:
                "integration-test-embedding-v1");

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType ==
                    typeof(
                        ISemanticRecommendationCache)
                && descriptor.ImplementationType ==
                    typeof(
                        RedisSemanticRecommendationCache)
                && descriptor.Lifetime ==
                    ServiceLifetime.Singleton);

        services.AddSingleton<
            ISemanticRecommendationCache,
            StubSemanticRecommendationCache>();

        services.AddAlternativeRecommendationAi(
            modelId:
                "integration-test-model",
            endpoint:
                new Uri(
                    "http://localhost:11434/v1"),
            apiKey:
                "integration-test-api-key",
            requestTimeout:
                TimeSpan.FromSeconds(
                    30));

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType ==
                    typeof(
                        SemanticKernelAlternativeRecommendationGenerator)
                && descriptor.Lifetime ==
                    ServiceLifetime.Scoped);

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType ==
                    typeof(
                        IUncachedAlternativeRecommendationGenerator)
                && descriptor.Lifetime ==
                    ServiceLifetime.Scoped);

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType ==
                    typeof(
                        CachedSemanticAlternativeRecommendationGenerator)
                && descriptor.Lifetime ==
                    ServiceLifetime.Scoped);

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType ==
                    typeof(
                        DeterministicAlternativeRecommendationGenerator)
                && descriptor.Lifetime ==
                    ServiceLifetime.Scoped);

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType ==
                    typeof(
                        IAlternativeRecommendationGenerator)
                && descriptor.ImplementationType ==
                    typeof(
                        ResilientAlternativeRecommendationGenerator)
                && descriptor.Lifetime ==
                    ServiceLifetime.Scoped);

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType ==
                    typeof(
                        IAlternativeRecommendationExecutor)
                && descriptor.Lifetime ==
                    ServiceLifetime.Scoped);

        using var serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild =
                        true,

                    ValidateScopes =
                        true
                });

        using var scope =
            serviceProvider.CreateScope();

        var generator =
            scope.ServiceProvider
                .GetRequiredService<
                    IAlternativeRecommendationGenerator>();

        var executor =
            scope.ServiceProvider
                .GetRequiredService<
                    IAlternativeRecommendationExecutor>();

        var uncachedGenerator =
            scope.ServiceProvider
                .GetRequiredService<
                    IUncachedAlternativeRecommendationGenerator>();

        var cachedGenerator =
            scope.ServiceProvider
                .GetRequiredService<
                    CachedSemanticAlternativeRecommendationGenerator>();

        Assert.IsType<
            ResilientAlternativeRecommendationGenerator>(
            generator);

        Assert.Same(
            generator,
            executor);

        Assert.IsType<
            TimeoutUncachedAlternativeRecommendationGenerator>(
            uncachedGenerator);

        Assert.NotNull(
            cachedGenerator);
    }

    private sealed class
        StubSemanticRecommendationEmbeddingGenerator
        : ISemanticRecommendationEmbeddingGenerator
    {
        public Task<
            SemanticRecommendationEmbedding>
            GenerateAsync(
                string text,
                CancellationToken
                    cancellationToken = default)
        {
            ArgumentException
                .ThrowIfNullOrWhiteSpace(
                    text);

            cancellationToken
                .ThrowIfCancellationRequested();

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
        StubSemanticRecommendationCache
        : ISemanticRecommendationCache
    {
        public Task<
            SemanticRecommendationCacheMatch?>
            FindAsync(
                SemanticRecommendationCacheLookup
                    lookup,
                CancellationToken
                    cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                lookup);

            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.FromResult<
                SemanticRecommendationCacheMatch?>(
                null);
        }

        public Task StoreAsync(
            SemanticRecommendationCacheEntry entry,
            CancellationToken
                cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                entry);

            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }

        public Task RemoveAsync(
            string entryId,
            CancellationToken
                cancellationToken = default)
        {
            ArgumentException
                .ThrowIfNullOrWhiteSpace(
                    entryId);

            cancellationToken
                .ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }
}