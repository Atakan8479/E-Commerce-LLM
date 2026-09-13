using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;

public static class
    SemanticRecommendationCacheDependencyInjection
{
    public static IServiceCollection
        AddSemanticRecommendationCache(
            this IServiceCollection services,
            string redisEndpoint,
            string redisPassword,
            string indexName,
            string keyPrefix,
            int vectorDimensions,
            double similarityThreshold,
            TimeSpan entryTimeToLive,
            string promptVersion,
            string schemaVersion,
            string semanticCacheVersion,
            string embeddingProfileVersion)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            redisEndpoint);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            redisPassword);

        var redisOptions =
            new RedisSemanticRecommendationCacheOptions(
                indexName,
                keyPrefix,
                vectorDimensions,
                similarityThreshold,
                entryTimeToLive);

        var cacheProfile =
            new SemanticRecommendationCacheProfile(
                promptVersion,
                schemaVersion,
                semanticCacheVersion,
                embeddingProfileVersion);

        var redisConfiguration =
            new ConfigurationOptions
            {
                Password =
                    redisPassword,

                AbortOnConnectFail =
                    false
            };

        redisConfiguration.EndPoints.Add(
            redisEndpoint);

        services.AddSingleton(
            redisOptions);

        services.AddSingleton(
            cacheProfile);

        services.AddSingleton<
            SemanticRecommendationRepresentationBuilder>();

        services.AddSingleton<
            IConnectionMultiplexer>(
            _ =>
                ConnectionMultiplexer.Connect(
                    redisConfiguration));

        services.AddSingleton(
            serviceProvider =>
                serviceProvider
                    .GetRequiredService<
                        IConnectionMultiplexer>()
                    .GetDatabase());

        services.AddSingleton<
            ISemanticRecommendationCache,
            RedisSemanticRecommendationCache>();

        return services;
    }
}