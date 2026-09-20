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
            TimeSpan connectTimeout,
            TimeSpan operationTimeout,
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

        var redisConfiguration =
            CreateRedisConfiguration(
                redisEndpoint,
                redisPassword,
                connectTimeout,
                operationTimeout);

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

    internal static ConfigurationOptions
        CreateRedisConfiguration(
            string redisEndpoint,
            string redisPassword,
            TimeSpan connectTimeout,
            TimeSpan operationTimeout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            redisEndpoint);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            redisPassword);

        var connectTimeoutMilliseconds =
            ToTimeoutMilliseconds(
                connectTimeout,
                nameof(connectTimeout));

        var operationTimeoutMilliseconds =
            ToTimeoutMilliseconds(
                operationTimeout,
                nameof(operationTimeout));

        var redisConfiguration =
            new ConfigurationOptions
            {
                Password =
                    redisPassword,

                AbortOnConnectFail =
                    false,

                ConnectTimeout =
                    connectTimeoutMilliseconds,

                AsyncTimeout =
                    operationTimeoutMilliseconds,

                SyncTimeout =
                    operationTimeoutMilliseconds
            };

        redisConfiguration.EndPoints.Add(
            redisEndpoint);

        return redisConfiguration;
    }

    private static int ToTimeoutMilliseconds(
        TimeSpan timeout,
        string parameterName)
    {
        if (timeout <= TimeSpan.Zero ||
            timeout.TotalMilliseconds >
            int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                timeout,
                "Redis timeout must be greater than zero " +
                "and fit within the supported millisecond range.");
        }

        return checked(
            (int)Math.Ceiling(
                timeout.TotalMilliseconds));
    }
}