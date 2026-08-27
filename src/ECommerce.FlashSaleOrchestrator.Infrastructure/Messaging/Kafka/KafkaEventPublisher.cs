using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Observability;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Messaging.Kafka;

public sealed class KafkaEventPublisher
    : IEventPublisher,
      IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly KafkaPublisherOptions
        _options;

    private readonly IProducer<string, string>
        _producer;

    public KafkaEventPublisher(
        IOptions<KafkaPublisherOptions> options)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        _options =
            options.Value;

        var producerConfig =
            new ProducerConfig
            {
                BootstrapServers =
                    _options.BootstrapServers,

                Acks =
                    Acks.All,

                EnableIdempotence =
                    true,

                ClientId =
                    "flashsale-outbox-publisher"
            };

        _producer =
            new ProducerBuilder<string, string>(
                producerConfig)
                .Build();
    }

    public async Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(
            integrationEvent);

        var destination =
            ResolveDestination(
                integrationEvent);

        var correlationId =
            ResolveCorrelationId(
                integrationEvent);

        var payload =
            JsonSerializer.Serialize(
                integrationEvent,
                integrationEvent.GetType(),
                SerializerOptions);

        var headers =
            new Headers();

        headers.Add(
            CorrelationMetadata.HeaderName,
            Encoding.UTF8.GetBytes(
                correlationId));

        var message =
            new Message<string, string>
            {
                Key =
                    destination.Key,

                Value =
                    payload,

                Headers =
                    headers
            };

        await _producer.ProduceAsync(
            destination.Topic,
            message,
            cancellationToken);
    }

    private static string ResolveCorrelationId<TEvent>(
        TEvent integrationEvent)
        where TEvent : class
    {
        if (integrationEvent is not
            IIntegrationEvent integrationMessage)
        {
            throw new InvalidOperationException(
                $"Integration event type " +
                $"'{integrationEvent.GetType().FullName}' " +
                $"does not implement {nameof(IIntegrationEvent)}.");
        }

        if (string.IsNullOrWhiteSpace(
            integrationMessage.CorrelationId))
        {
            throw new InvalidOperationException(
                "Integration event correlation id cannot be empty.");
        }

        if (integrationMessage.CorrelationId.Length >
            CorrelationMetadata.MaxLength)
        {
            throw new InvalidOperationException(
                $"Integration event correlation id cannot exceed " +
                $"{CorrelationMetadata.MaxLength} characters.");
        }

        return integrationMessage.CorrelationId;
    }

    private (string Topic, string Key)
        ResolveDestination<TEvent>(
        TEvent integrationEvent)
        where TEvent : class
    {
        return integrationEvent switch
        {
            StockDepletedIntegrationEvent
                stockDepletedEvent =>
                (
                    _options.StockDepletedTopic,
                    stockDepletedEvent.EventId
                        .ToString("D")
                ),

            _ =>
                throw new InvalidOperationException(
                    $"Integration event type " +
                    $"'{integrationEvent.GetType().FullName}' " +
                    $"is not supported.")
        };
    }

    public void Dispose()
    {
        _producer.Flush(
            TimeSpan.FromSeconds(5));

        _producer.Dispose();
    }
}