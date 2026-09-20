using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;

namespace ECommerce.FlashSaleOrchestrator
    .Infrastructure.IntegrationTests.AI.SemanticCaching;

public sealed class
    SemanticRecommendationCacheDependencyInjectionTests
{
    [Fact]
    public void
        CreateRedisConfiguration_ShouldApplyBoundedTimeouts()
    {
        var configuration =
            SemanticRecommendationCacheDependencyInjection
                .CreateRedisConfiguration(
                    redisEndpoint:
                        "127.0.0.1:6379",
                    redisPassword:
                        "integration-test-password",
                    connectTimeout:
                        TimeSpan.FromSeconds(
                            3),
                    operationTimeout:
                        TimeSpan.FromSeconds(
                            2));

        Assert.Equal(
            3000,
            configuration.ConnectTimeout);

        Assert.Equal(
            2000,
            configuration.AsyncTimeout);

        Assert.Equal(
            2000,
            configuration.SyncTimeout);

        Assert.False(
            configuration.AbortOnConnectFail);
    }

    [Fact]
    public void
        CreateRedisConfiguration_ShouldRejectNonPositiveTimeouts()
    {
        Assert.Throws<
            ArgumentOutOfRangeException>(
            () =>
                SemanticRecommendationCacheDependencyInjection
                    .CreateRedisConfiguration(
                        redisEndpoint:
                            "127.0.0.1:6379",
                        redisPassword:
                            "integration-test-password",
                        connectTimeout:
                            TimeSpan.Zero,
                        operationTimeout:
                            TimeSpan.FromSeconds(
                                2)));

        Assert.Throws<
            ArgumentOutOfRangeException>(
            () =>
                SemanticRecommendationCacheDependencyInjection
                    .CreateRedisConfiguration(
                        redisEndpoint:
                            "127.0.0.1:6379",
                        redisPassword:
                            "integration-test-password",
                        connectTimeout:
                            TimeSpan.FromSeconds(
                                3),
                        operationTimeout:
                            TimeSpan.Zero));
    }
}