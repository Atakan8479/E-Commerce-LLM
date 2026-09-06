using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Resilience;
using ECommerce.FlashSaleOrchestrator.Worker.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.FlashSaleOrchestrator.Worker.Tests.Resilience;

public sealed class IntegrationEventRetryExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnImmediately_WhenFirstAttemptSucceeds()
    {
        var executor =
            CreateExecutor(
                maxAttempts: 3);

        var attempts =
            0;

        var eventId =
            Guid.NewGuid();

        var result =
            await executor.ExecuteAsync(
                cancellationToken =>
                {
                    attempts++;

                    return Task.FromResult(
                        42);
                },
                eventId,
                "stock-depleted");

        Assert.Equal(
            42,
            result);

        Assert.Equal(
            1,
            attempts);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetryUntilSuccess_WhenTransientFailuresOccur()
    {
        var executor =
            CreateExecutor(
                maxAttempts: 3);

        var attempts =
            0;

        var eventId =
            Guid.NewGuid();

        var result =
            await executor.ExecuteAsync(
                cancellationToken =>
                {
                    attempts++;

                    if (attempts < 3)
                    {
                        throw new InvalidOperationException(
                            $"Transient failure on attempt {attempts}.");
                    }

                    return Task.FromResult(
                        42);
                },
                eventId,
                "stock-depleted");

        Assert.Equal(
            42,
            result);

        Assert.Equal(
            3,
            attempts);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRethrowException_WhenAllAttemptsFail()
    {
        var executor =
            CreateExecutor(
                maxAttempts: 3);

        var attempts =
            0;

        var eventId =
            Guid.NewGuid();

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    executor.ExecuteAsync<int>(
                        cancellationToken =>
                        {
                            attempts++;

                            throw new InvalidOperationException(
                                $"Failure on attempt {attempts}.");
                        },
                        eventId,
                        "stock-depleted"));

        Assert.Equal(
            3,
            attempts);

        Assert.Equal(
            "Failure on attempt 3.",
            exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotRetry_WhenFailureIsNonRetryable()
    {
        var executor =
            CreateExecutor(
                maxAttempts: 3);

        var attempts =
            0;

        var exception =
            await Assert.ThrowsAsync<
                NonRetryableTestException>(
                () =>
                    executor.ExecuteAsync<int>(
                        cancellationToken =>
                        {
                            attempts++;

                            throw new NonRetryableTestException(
                                "Permanent failure.");
                        },
                        Guid.NewGuid(),
                        "stock-depleted"));

        Assert.Equal(
            "Permanent failure.",
            exception.Message);

        Assert.Equal(
            1,
            attempts);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotRetry_WhenCancellationWasRequested()
    {
        var executor =
            CreateExecutor(
                maxAttempts: 3);

        var attempts =
            0;

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            () =>
                executor.ExecuteAsync<int>(
                    cancellationToken =>
                    {
                        attempts++;

                        throw new OperationCanceledException(
                            cancellationToken);
                    },
                    Guid.NewGuid(),
                    "stock-depleted",
                    cancellationTokenSource.Token));

        Assert.Equal(
            1,
            attempts);
    }

    private static IntegrationEventRetryExecutor CreateExecutor(
        int maxAttempts)
    {
        return new IntegrationEventRetryExecutor(
            Options.Create(
                new EventProcessingRetryOptions
                {
                    MaxAttempts =
                        maxAttempts,

                    InitialDelay =
                        TimeSpan.Zero
                }),
            NullLogger<
                IntegrationEventRetryExecutor>.Instance);
    }

    private sealed class NonRetryableTestException
        : Exception,
          INonRetryableException
    {
        public NonRetryableTestException(
            string message)
            : base(message)
        {
        }
    }
}