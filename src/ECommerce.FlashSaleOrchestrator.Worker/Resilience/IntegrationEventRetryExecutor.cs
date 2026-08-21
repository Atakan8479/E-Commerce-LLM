using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Worker.Resilience;

public sealed class IntegrationEventRetryExecutor
{
    private readonly EventProcessingRetryOptions
        _options;

    private readonly ILogger<IntegrationEventRetryExecutor>
        _logger;

    public IntegrationEventRetryExecutor(
        IOptions<EventProcessingRetryOptions> options,
        ILogger<IntegrationEventRetryExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        ArgumentNullException.ThrowIfNull(
            logger);

        _options =
            options.Value;

        _logger =
            logger;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        Guid eventId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            operation);

        if (eventId == Guid.Empty)
        {
            throw new ArgumentException(
                "Integration event id cannot be empty.",
                nameof(eventId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            eventType);

        for (var attempt = 1;
             attempt <= _options.MaxAttempts;
             attempt++)
        {
            try
            {
                return await operation(
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
                when (attempt < _options.MaxAttempts)
            {
                var delay =
                    CalculateDelay(
                        attempt);

                _logger.LogWarning(
                    exception,
                    "Integration event processing failed. " +
                    "Retry scheduled. " +
                    "EventId: {EventId}, " +
                    "EventType: {EventType}, " +
                    "Attempt: {Attempt}, " +
                    "MaxAttempts: {MaxAttempts}, " +
                    "RetryDelay: {RetryDelay}",
                    eventId,
                    eventType,
                    attempt,
                    _options.MaxAttempts,
                    delay);

                await Task.Delay(
                    delay,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Integration event processing exhausted all retry attempts. " +
                    "EventId: {EventId}, " +
                    "EventType: {EventType}, " +
                    "Attempts: {Attempts}",
                    eventId,
                    eventType,
                    _options.MaxAttempts);

                throw;
            }
        }

        throw new InvalidOperationException(
            "Integration event retry execution reached an invalid state.");
    }

    private TimeSpan CalculateDelay(
        int failedAttempt)
    {
        var multiplier =
            Math.Pow(
                2,
                failedAttempt - 1);

        return TimeSpan.FromMilliseconds(
            _options.InitialDelay.TotalMilliseconds
            * multiplier);
    }
}