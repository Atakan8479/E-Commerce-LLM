using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Worker
    .Messaging.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Worker
    .HealthChecks;

public sealed class KafkaReadinessHealthCheck
    : IHealthCheck
{
    private static readonly TimeSpan MetadataTimeout =
        TimeSpan.FromSeconds(
            2);

    private readonly IAdminClient
        _adminClient;

    private readonly KafkaConsumerOptions
        _options;

    public KafkaReadinessHealthCheck(
        IAdminClient adminClient,
        IOptions<KafkaConsumerOptions> options)
    {
        _adminClient =
            adminClient
            ?? throw new ArgumentNullException(
                nameof(adminClient));

        ArgumentNullException.ThrowIfNull(
            options);

        _options =
            options.Value;
    }

    public async Task<HealthCheckResult>
        CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var metadata =
                await Task.Run(
                        () =>
                            _adminClient.GetMetadata(
                                MetadataTimeout),
                        CancellationToken.None)
                    .WaitAsync(
                        cancellationToken);

            if (metadata.Brokers.Count == 0)
            {
                return HealthCheckResult.Unhealthy(
                    "Kafka broker metadata is unavailable.");
            }

            var sourceTopicCheck =
                ValidateTopic(
                    metadata,
                    _options.StockDepletedTopic);

            if (sourceTopicCheck.HasValue)
            {
                return sourceTopicCheck.Value;
            }

            var deadLetterTopicCheck =
                ValidateTopic(
                    metadata,
                    _options.StockDepletedDeadLetterTopic);

            if (deadLetterTopicCheck.HasValue)
            {
                return deadLetterTopicCheck.Value;
            }

            return HealthCheckResult.Healthy(
                "Kafka broker and required topics are reachable.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Kafka readiness check failed.",
                exception);
        }
    }

    private static HealthCheckResult?
        ValidateTopic(
            Metadata metadata,
            string topicName)
    {
        var topicMetadata =
            metadata.Topics.FirstOrDefault(
                topic =>
                    string.Equals(
                        topic.Topic,
                        topicName,
                        StringComparison.Ordinal));

        if (topicMetadata is null)
        {
            return HealthCheckResult.Unhealthy(
                $"Required Kafka topic '{topicName}' " +
                "is not available.");
        }

        if (topicMetadata.Error.IsError)
        {
            return HealthCheckResult.Unhealthy(
                $"Required Kafka topic '{topicName}' " +
                $"reported error '{topicMetadata.Error.Code}'.");
        }

        return null;
    }
}