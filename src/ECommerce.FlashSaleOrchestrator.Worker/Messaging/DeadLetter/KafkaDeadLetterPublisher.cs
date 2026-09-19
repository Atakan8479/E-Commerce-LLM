using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Observability;
using ECommerce.FlashSaleOrchestrator.Worker
    .Messaging.Kafka;
using ECommerce.FlashSaleOrchestrator.Worker
    .Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Worker
    .Messaging.DeadLetter;

public sealed class KafkaDeadLetterPublisher
    : IDeadLetterPublisher,
      IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly KafkaConsumerOptions
        _options;

    private readonly ILogger<KafkaDeadLetterPublisher>
        _logger;

    private readonly IProducer<string, string>
        _producer;

    public KafkaDeadLetterPublisher(
        IOptions<KafkaConsumerOptions> options,
        ILogger<KafkaDeadLetterPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        ArgumentNullException.ThrowIfNull(
            logger);

        _options =
            options.Value;

        _logger =
            logger;

        var producerConfig =
            new ProducerConfig
            {
                BootstrapServers =
                    _options.BootstrapServers,

                ClientId =
                    "flashsale-dead-letter-publisher",

                Acks =
                    Acks.All,

                EnableIdempotence =
                    true
            };

        _producer =
            new ProducerBuilder<string, string>(
                producerConfig)
                .Build();
    }

    public async Task PublishAsync(
        DeadLetterMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            message);

        var correlationId =
            NormalizeCorrelationId(
                message.CorrelationId);

        var messageToPublish =
            message with
            {
                CorrelationId =
                    correlationId
            };

        var payload =
            JsonSerializer.Serialize(
                messageToPublish,
                SerializerOptions);

        var messageKey =
            ResolveMessageKey(
                messageToPublish);

        var kafkaMessage =
            new Message<string, string>
            {
                Key =
                    messageKey,

                Value =
                    payload
            };

        if (correlationId is not null)
        {
            kafkaMessage.Headers =
                new Headers();

            kafkaMessage.Headers.Add(
                CorrelationMetadata.HeaderName,
                Encoding.UTF8.GetBytes(
                    correlationId));
        }

        var result =
            await _producer.ProduceAsync(
                _options.StockDepletedDeadLetterTopic,
                kafkaMessage,
                cancellationToken);

        WorkerMetrics.ConsumerDeadLetters.Add(
            1);

        _logger.LogWarning(
            "Message published to dead-letter topic. " +
            "EventId: {EventId}, " +
            "EventType: {EventType}, " +
            "CorrelationId: {CorrelationId}, " +
            "OriginalTopic: {OriginalTopic}, " +
            "DeadLetterTopic: {DeadLetterTopic}, " +
            "Partition: {Partition}, " +
            "Offset: {Offset}",
            messageToPublish.EventId,
            messageToPublish.EventType,
            messageToPublish.CorrelationId,
            messageToPublish.OriginalTopic,
            _options.StockDepletedDeadLetterTopic,
            result.Partition,
            result.Offset);
    }

    private static string? NormalizeCorrelationId(
        string? correlationId)
    {
        if (correlationId is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(
            correlationId))
        {
            throw new InvalidOperationException(
                "Dead-letter correlation id cannot be empty.");
        }

        var normalizedCorrelationId =
            correlationId.Trim();

        if (normalizedCorrelationId.Length >
            CorrelationMetadata.MaxLength)
        {
            throw new InvalidOperationException(
                "Dead-letter correlation id cannot exceed " +
                $"{CorrelationMetadata.MaxLength} characters.");
        }

        return normalizedCorrelationId;
    }

    private static string ResolveMessageKey(
        DeadLetterMessage message)
    {
        if (!string.IsNullOrWhiteSpace(
            message.OriginalKey))
        {
            return message.OriginalKey;
        }

        if (message.EventId.HasValue)
        {
            return message.EventId.Value.ToString(
                "D");
        }

        return string.Join(
            ":",
            message.OriginalTopic,
            message.OriginalPartition,
            message.OriginalOffset);
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}