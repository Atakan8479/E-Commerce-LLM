using System.Text.Json;
using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Messaging.Kafka;

public sealed class KafkaEventPublisher
    : IEventPublisher,
      IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly KafkaPublisherOptions _options;
    private readonly IProducer<string, string> _producer;

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

        var payload =
            JsonSerializer.Serialize(
                integrationEvent,
                integrationEvent.GetType(),
                SerializerOptions);

        var message =
            new Message<string, string>
            {
                Key =
                    destination.Key,

                Value =
                    payload
            };

        await _producer.ProduceAsync(
            destination.Topic,
            message,
            cancellationToken);
    }

    private (string Topic, string Key) ResolveDestination<TEvent>(
        TEvent integrationEvent)
        where TEvent : class
    {
        return integrationEvent switch
        {
            StockDepletedIntegrationEvent stockDepletedEvent =>
                (
                    _options.StockDepletedTopic,
                    stockDepletedEvent.EventId.ToString("D")
                ),

            _ =>
                throw new InvalidOperationException(
                    $"Integration event type '{integrationEvent.GetType().FullName}' is not supported.")
        };
    }

    public void Dispose()
    {
        _producer.Flush(
            TimeSpan.FromSeconds(5));

        _producer.Dispose();
    }
}