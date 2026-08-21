using System.Text.Json;
using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Worker.Messaging.DeadLetter;

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

        var payload =
            JsonSerializer.Serialize(
                message,
                SerializerOptions);

        var messageKey =
            ResolveMessageKey(
                message);

        var result =
            await _producer.ProduceAsync(
                _options.StockDepletedDeadLetterTopic,
                new Message<string, string>
                {
                    Key =
                        messageKey,

                    Value =
                        payload
                },
                cancellationToken);

        _logger.LogWarning(
            "Message published to dead-letter topic. " +
            "EventId: {EventId}, " +
            "EventType: {EventType}, " +
            "OriginalTopic: {OriginalTopic}, " +
            "DeadLetterTopic: {DeadLetterTopic}, " +
            "Partition: {Partition}, " +
            "Offset: {Offset}",
            message.EventId,
            message.EventType,
            message.OriginalTopic,
            _options.StockDepletedDeadLetterTopic,
            result.Partition,
            result.Offset);
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