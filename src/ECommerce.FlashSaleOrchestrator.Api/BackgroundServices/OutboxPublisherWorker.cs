using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Api.BackgroundServices;

public sealed class OutboxPublisherWorker
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxPublisherOptions _options;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxPublisherOptions> options,
        ILogger<OutboxPublisherWorker> logger)
    {
        _scopeFactory =
            scopeFactory
            ?? throw new ArgumentNullException(
                nameof(scopeFactory));

        ArgumentNullException.ThrowIfNull(
            options);

        _options =
            options.Value;

        _logger =
            logger
            ?? throw new ArgumentNullException(
                nameof(logger));
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingMessagesAsync(
                stoppingToken);

            await Task.Delay(
                _options.PollingInterval,
                stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var processor =
                scope.ServiceProvider
                    .GetRequiredService<OutboxProcessor>();

            var processedCount =
                await processor.ProcessPendingAsync(
                    _options.BatchSize,
                    cancellationToken);

            if (processedCount > 0)
            {
                _logger.LogInformation(
                    "Processed {ProcessedCount} outbox message(s).",
                    processedCount);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An error occurred while processing pending outbox messages.");
        }
    }
}