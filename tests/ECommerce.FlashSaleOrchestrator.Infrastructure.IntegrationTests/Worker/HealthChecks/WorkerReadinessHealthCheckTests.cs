using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Api;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Kafka;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence;
using ECommerce.FlashSaleOrchestrator.Worker
    .HealthChecks;
using ECommerce.FlashSaleOrchestrator.Worker
    .Messaging.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .IntegrationTests.Worker.HealthChecks;

public sealed class WorkerReadinessHealthCheckTests
{
    private const string UnavailableSqlConnectionString =
        "Server=127.0.0.1,65534;" +
        "Database=Unavailable;" +
        "User Id=sa;" +
        "Password=integration-test-password;" +
        "Encrypt=False;" +
        "TrustServerCertificate=True;" +
        "Connect Timeout=1;";

    [Fact]
    public async Task
        SqlServerReadinessHealthCheck_ShouldReturnHealthy_WhenSqlServerIsAvailable()
    {
        await using var database =
            await ApiTestDatabase.CreateAsync();

        await using var dbContext =
            database.CreateContext();

        var healthCheck =
            new SqlServerReadinessHealthCheck(
                dbContext);

        var result =
            await healthCheck.CheckHealthAsync(
                new HealthCheckContext());

        Assert.Equal(
            HealthStatus.Healthy,
            result.Status);
    }

    [Fact]
    public async Task
        SqlServerReadinessHealthCheck_ShouldReturnUnhealthy_WhenSqlServerIsUnavailable()
    {
        var options =
            new DbContextOptionsBuilder<
                FlashSaleOrchestratorDbContext>()
                .UseSqlServer(
                    UnavailableSqlConnectionString)
                .Options;

        await using var dbContext =
            new FlashSaleOrchestratorDbContext(
                options);

        var healthCheck =
            new SqlServerReadinessHealthCheck(
                dbContext);

        var result =
            await healthCheck.CheckHealthAsync(
                new HealthCheckContext());

        Assert.Equal(
            HealthStatus.Unhealthy,
            result.Status);
    }

    [Fact]
    public async Task
        KafkaReadinessHealthCheck_ShouldReturnHealthy_WhenBrokerAndTopicsAreAvailable()
    {
        await using var sourceTopic =
            await KafkaTestTopic.CreateAsync();

        await using var deadLetterTopic =
            await KafkaTestTopic.CreateAsync();

        using var adminClient =
            new AdminClientBuilder(
                new AdminClientConfig
                {
                    BootstrapServers =
                        sourceTopic.BootstrapServers
                })
                .Build();

        var options =
            Options.Create(
                new KafkaConsumerOptions
                {
                    BootstrapServers =
                        sourceTopic.BootstrapServers,

                    StockDepletedTopic =
                        sourceTopic.Name,

                    StockDepletedDeadLetterTopic =
                        deadLetterTopic.Name,

                    ConsumerGroupId =
                        "health-check-test"
                });

        var healthCheck =
            new KafkaReadinessHealthCheck(
                adminClient,
                options);

        var result =
            await healthCheck.CheckHealthAsync(
                new HealthCheckContext());

        Assert.Equal(
            HealthStatus.Healthy,
            result.Status);
    }

    [Fact]
    public async Task
        KafkaReadinessHealthCheck_ShouldReturnUnhealthy_WhenRequiredTopicIsUnavailable()
    {
        await using var sourceTopic =
            await KafkaTestTopic.CreateAsync();

        using var adminClient =
            new AdminClientBuilder(
                new AdminClientConfig
                {
                    BootstrapServers =
                        sourceTopic.BootstrapServers
                })
                .Build();

        var missingDeadLetterTopic =
            $"inventory.stock-depleted.dlq.missing." +
            $"{Guid.NewGuid():N}";

        var options =
            Options.Create(
                new KafkaConsumerOptions
                {
                    BootstrapServers =
                        sourceTopic.BootstrapServers,

                    StockDepletedTopic =
                        sourceTopic.Name,

                    StockDepletedDeadLetterTopic =
                        missingDeadLetterTopic,

                    ConsumerGroupId =
                        "health-check-test"
                });

        var healthCheck =
            new KafkaReadinessHealthCheck(
                adminClient,
                options);

        var result =
            await healthCheck.CheckHealthAsync(
                new HealthCheckContext());

        Assert.Equal(
            HealthStatus.Unhealthy,
            result.Status);
    }
}