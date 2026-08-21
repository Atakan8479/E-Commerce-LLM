using System.Text.Json;
using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.Kafka;
using ECommerce.FlashSaleOrchestrator.Worker.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Worker.BackgroundServices;

public sealed class StockDepletedConsumerWorker
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly KafkaConsumerOptions _options;

    private readonly IServiceScopeFactory
        _serviceScopeFactory;

    private readonly IntegrationEventRetryExecutor
        _retryExecutor;

    private readonly ILogger<StockDepletedConsumerWorker>
        _logger;

    private readonly IConsumer<string, string>
        _consumer;

    public StockDepletedConsumerWorker(
        IOptions<KafkaConsumerOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        IntegrationEventRetryExecutor retryExecutor,
        ILogger<StockDepletedConsumerWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        ArgumentNullException.ThrowIfNull(
            serviceScopeFactory);

        ArgumentNullException.ThrowIfNull(
            retryExecutor);

        ArgumentNullException.ThrowIfNull(
            logger);

        _options =
            options.Value;

        _serviceScopeFactory =
            serviceScopeFactory;

        _retryExecutor =
            retryExecutor;

        _logger =
            logger;

        var consumerConfig =
            new ConsumerConfig
            {
                BootstrapServers =
                    _options.BootstrapServers,

                GroupId =
                    _options.ConsumerGroupId,

                ClientId =
                    "flashsale-stock-depleted-consumer",

                AutoOffsetReset =
                    AutoOffsetReset.Earliest,

                EnableAutoCommit =
                    false,

                EnableAutoOffsetStore =
                    false
            };

        _consumer =
            new ConsumerBuilder<string, string>(
                consumerConfig)
                .Build();

        _consumer.Subscribe(
            _options.StockDepletedTopic);
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Stock depleted consumer started. Topic: {Topic}, GroupId: {GroupId}",
            _options.StockDepletedTopic,
            _options.ConsumerGroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>
                    consumeResult;

                try
                {
                    consumeResult =
                        _consumer.Consume(
                            stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException exception)
                {
                    _logger.LogError(
                        exception,
                        "Kafka consume operation failed.");

                    continue;
                }

                try
                {
                    var integrationEvent =
                        Deserialize(
                            consumeResult.Message.Value);

                    var processingResult =
                        await _retryExecutor.ExecuteAsync(
                            cancellationToken =>
                                ProcessIntegrationEventAsync(
                                    integrationEvent,
                                    cancellationToken),
                            integrationEvent.EventId,
                            integrationEvent.EventType,
                            stoppingToken);

                    _consumer.Commit(
                        consumeResult);

                    _logger.LogInformation(
                        "Stock depleted event acknowledged. " +
                        "EventId: {EventId}, " +
                        "ProcessingResult: {ProcessingResult}, " +
                        "Topic: {Topic}, " +
                        "Partition: {Partition}, " +
                        "Offset: {Offset}",
                        integrationEvent.EventId,
                        processingResult,
                        consumeResult.Topic,
                        consumeResult.Partition,
                        consumeResult.Offset);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Stock depleted event processing failed. " +
                        "Offset was not committed. " +
                        "Topic: {Topic}, " +
                        "Partition: {Partition}, " +
                        "Offset: {Offset}",
                        consumeResult.Topic,
                        consumeResult.Partition,
                        consumeResult.Offset);

                    throw;
                }
            }
        }
        finally
        {
            _consumer.Close();

            _logger.LogInformation(
                "Stock depleted consumer stopped.");
        }
    }

    private async Task<IntegrationEventProcessingResult>
        ProcessIntegrationEventAsync(
        StockDepletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await using var scope =
            _serviceScopeFactory
                .CreateAsyncScope();

        var processor =
            scope.ServiceProvider
                .GetRequiredService<
                    IIntegrationEventProcessor<
                        StockDepletedIntegrationEvent>>();

        return await processor.ProcessAsync(
            integrationEvent,
            cancellationToken);
    }

    private static StockDepletedIntegrationEvent Deserialize(
        string? payload)
    {
        if (string.IsNullOrWhiteSpace(
            payload))
        {
            throw new InvalidOperationException(
                "Stock depleted event payload cannot be empty.");
        }

        var integrationEvent =
            JsonSerializer.Deserialize<
                StockDepletedIntegrationEvent>(
                    payload,
                    SerializerOptions)
            ?? throw new InvalidOperationException(
                "Stock depleted event payload could not be deserialized.");

        if (integrationEvent.EventId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Stock depleted event id cannot be empty.");
        }

        if (integrationEvent.ProductId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Stock depleted product id cannot be empty.");
        }

        if (integrationEvent.OccurredAtUtc == default)
        {
            throw new InvalidOperationException(
                "Stock depleted event occurrence time must be provided.");
        }

        return integrationEvent;
    }

    public override void Dispose()
    {
        _consumer.Dispose();

        base.Dispose();
    }
}