using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Observability;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.DeadLetter;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.Kafka;
using ECommerce.FlashSaleOrchestrator.Worker.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ECommerce.FlashSaleOrchestrator.Worker.BackgroundServices;

public sealed class StockDepletedConsumerWorker
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly KafkaConsumerOptions
        _options;

    private readonly IServiceScopeFactory
        _serviceScopeFactory;

    private readonly IntegrationEventRetryExecutor
        _retryExecutor;

    private readonly IDeadLetterPublisher
        _deadLetterPublisher;

    private readonly ILogger<StockDepletedConsumerWorker>
        _logger;

    private readonly IConsumer<string, string>
        _consumer;

    public StockDepletedConsumerWorker(
        IOptions<KafkaConsumerOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        IntegrationEventRetryExecutor retryExecutor,
        IDeadLetterPublisher deadLetterPublisher,
        ILogger<StockDepletedConsumerWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        ArgumentNullException.ThrowIfNull(
            serviceScopeFactory);

        ArgumentNullException.ThrowIfNull(
            retryExecutor);

        ArgumentNullException.ThrowIfNull(
            deadLetterPublisher);

        ArgumentNullException.ThrowIfNull(
            logger);

        _options =
            options.Value;

        _serviceScopeFactory =
            serviceScopeFactory;

        _retryExecutor =
            retryExecutor;

        _deadLetterPublisher =
            deadLetterPublisher;

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
            "Stock depleted consumer started. " +
            "Topic: {Topic}, " +
            "GroupId: {GroupId}",
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
                    StockDepletedIntegrationEvent
                        integrationEvent;

                    string correlationId;

                    try
                    {
                        integrationEvent =
                            Deserialize(
                                consumeResult.Message.Value);

                        correlationId =
                            ResolveCorrelationId(
                                consumeResult,
                                integrationEvent);
                    }
                    catch (Exception messageValidationException)
                    {
                        await PublishPoisonMessageToDeadLetterAsync(
                            consumeResult,
                            messageValidationException,
                            stoppingToken);

                        _consumer.Commit(
                            consumeResult);

                        _logger.LogWarning(
                            "Invalid stock depleted message moved " +
                            "to dead-letter topic and original " +
                            "offset committed. " +
                            "Topic: {Topic}, " +
                            "Partition: {Partition}, " +
                            "Offset: {Offset}",
                            consumeResult.Topic,
                            consumeResult.Partition,
                            consumeResult.Offset);

                        continue;
                    }

                    using (_logger.BeginScope(
                               new Dictionary<string, object>
                               {
                                   ["CorrelationId"] =
                                       correlationId,

                                   ["EventId"] =
                                       integrationEvent.EventId,

                                   ["ProductId"] =
                                       integrationEvent.ProductId,

                                   ["EventType"] =
                                       integrationEvent.EventType
                               }))
                    {
                        var processingStartedAt =
                            Stopwatch.GetTimestamp();

                        try
                        {
                            var processingResult =
                                await _retryExecutor.ExecuteAsync(
                                    cancellationToken =>
                                        ProcessIntegrationEventAsync(
                                            integrationEvent,
                                            correlationId,
                                            cancellationToken),
                                    integrationEvent.EventId,
                                    integrationEvent.EventType,
                                    stoppingToken);

                            _consumer.Commit(
                                consumeResult);

                            var durationMs =
                                Stopwatch.GetElapsedTime(
                                        processingStartedAt)
                                    .TotalMilliseconds;

                            _logger.LogInformation(
                                "Stock depleted event acknowledged. " +
                                "ProcessingResult: {ProcessingResult}, " +
                                "Topic: {Topic}, " +
                                "Partition: {Partition}, " +
                                "Offset: {Offset}, " +
                                "DurationMs: {DurationMs}",
                                processingResult,
                                consumeResult.Topic,
                                consumeResult.Partition,
                                consumeResult.Offset,
                                durationMs);
                        }
                        catch (OperationCanceledException)
                            when (stoppingToken.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (Exception processingException)
                        {
                            await PublishToDeadLetterAsync(
                                consumeResult,
                                integrationEvent,
                                correlationId,
                                processingException,
                                stoppingToken);

                            _consumer.Commit(
                                consumeResult);

                            var durationMs =
                                Stopwatch.GetElapsedTime(
                                        processingStartedAt)
                                    .TotalMilliseconds;

                            _logger.LogWarning(
                                processingException,
                                "Stock depleted event moved to " +
                                "dead-letter topic and original " +
                                "offset committed. " +
                                "Topic: {Topic}, " +
                                "Partition: {Partition}, " +
                                "Offset: {Offset}, " +
                                "DurationMs: {DurationMs}",
                                consumeResult.Topic,
                                consumeResult.Partition,
                                consumeResult.Offset,
                                durationMs);
                        }
                    }
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
        string correlationId,
        CancellationToken cancellationToken)
    {
        await using var scope =
            _serviceScopeFactory
                .CreateAsyncScope();

        var correlationContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ICorrelationContext>();

        correlationContext.SetCorrelationId(
            correlationId);

        var processor =
            scope.ServiceProvider
                .GetRequiredService<
                    IIntegrationEventProcessor<
                        StockDepletedIntegrationEvent>>();

        return await processor.ProcessAsync(
            integrationEvent,
            cancellationToken);
    }

    private async Task PublishToDeadLetterAsync(
        ConsumeResult<string, string> consumeResult,
        StockDepletedIntegrationEvent integrationEvent,
        string correlationId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var deadLetterMessage =
            new DeadLetterMessage(
                integrationEvent.EventId,
                integrationEvent.EventType,
                correlationId,
                consumeResult.Message.Key,
                consumeResult.Topic,
                consumeResult.Partition.Value,
                consumeResult.Offset.Value,
                consumeResult.Message.Value,
                exception.GetType().FullName
                    ?? exception.GetType().Name,
                exception.Message,
                DateTime.UtcNow);

        await _deadLetterPublisher.PublishAsync(
            deadLetterMessage,
            cancellationToken);
    }


    private async Task PublishPoisonMessageToDeadLetterAsync(
        ConsumeResult<string, string> consumeResult,
        Exception exception,
        CancellationToken cancellationToken)
    {
        Guid? eventId =
            Guid.TryParse(
                consumeResult.Message.Key,
                out var parsedEventId)
                    ? parsedEventId
                    : null;

        var correlationId =
            TryResolveCorrelationIdFromHeader(
                consumeResult);

        var deadLetterMessage =
            new DeadLetterMessage(
                eventId,
                null,
                correlationId,
                consumeResult.Message.Key,
                consumeResult.Topic,
                consumeResult.Partition.Value,
                consumeResult.Offset.Value,
                consumeResult.Message.Value,
                exception.GetType().FullName
                    ?? exception.GetType().Name,
                exception.Message,
                DateTime.UtcNow);

        await _deadLetterPublisher.PublishAsync(
            deadLetterMessage,
            cancellationToken);
    }

    private static string? TryResolveCorrelationIdFromHeader(
    ConsumeResult<string, string> consumeResult)
    {
        var headers =
            consumeResult.Message.Headers;

        if (headers is null ||
            !headers.TryGetLastBytes(
                CorrelationMetadata.HeaderName,
                out var correlationHeader))
        {
            return null;
        }

        if (correlationHeader is null ||
            correlationHeader.Length == 0)
        {
            return null;
        }

        try
        {
            var correlationId =
                Encoding.UTF8.GetString(
                    correlationHeader);

            if (string.IsNullOrWhiteSpace(
                correlationId))
            {
                return null;
            }

            var normalizedCorrelationId =
                correlationId.Trim();

            if (normalizedCorrelationId.Length >
                CorrelationMetadata.MaxLength)
            {
                return null;
            }

            return normalizedCorrelationId;
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveCorrelationId(
        ConsumeResult<string, string> consumeResult,
        StockDepletedIntegrationEvent integrationEvent)
    {
        var payloadCorrelationId =
            NormalizeCorrelationId(
                integrationEvent.CorrelationId,
                "event payload");

        var headers =
            consumeResult.Message.Headers;

        if (headers is null ||
            !headers.TryGetLastBytes(
                CorrelationMetadata.HeaderName,
                out var correlationHeader))
        {
            return payloadCorrelationId;
        }

        if (correlationHeader is null ||
            correlationHeader.Length == 0)
        {
            throw new InvalidOperationException(
                "Kafka correlation id header cannot be empty.");
        }

        var headerCorrelationId =
            NormalizeCorrelationId(
                Encoding.UTF8.GetString(
                    correlationHeader),
                "Kafka header");

        if (!string.Equals(
                headerCorrelationId,
                payloadCorrelationId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Kafka correlation id header does not match " +
                "the integration event correlation id.");
        }

        return headerCorrelationId;
    }

    private static string NormalizeCorrelationId(
        string? correlationId,
        string source)
    {
        if (string.IsNullOrWhiteSpace(
            correlationId))
        {
            throw new InvalidOperationException(
                $"Correlation id from {source} cannot be empty.");
        }

        var normalizedCorrelationId =
            correlationId.Trim();

        if (normalizedCorrelationId.Length >
            CorrelationMetadata.MaxLength)
        {
            throw new InvalidOperationException(
                $"Correlation id from {source} cannot exceed " +
                $"{CorrelationMetadata.MaxLength} characters.");
        }

        return normalizedCorrelationId;
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