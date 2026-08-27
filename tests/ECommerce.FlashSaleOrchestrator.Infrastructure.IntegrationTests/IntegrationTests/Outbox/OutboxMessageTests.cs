using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Outbox;

public sealed class OutboxMessageTests
{
    [Fact]
    public void MarkProcessed_ShouldSetProcessedAtUtc()
    {
        var occurredAtUtc =
            new DateTime(
                2026,
                8,
                16,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var processedAtUtc =
            occurredAtUtc.AddSeconds(5);

        var message =
            new OutboxMessage(
                Guid.NewGuid(),
                occurredAtUtc,
                "TestEvent",
                "{}",
                "correlation-test-123");

        message.MarkProcessed(processedAtUtc);

        Assert.Equal(
            processedAtUtc,
            message.ProcessedAtUtc);
    }

    [Fact]
    public void MarkProcessed_ShouldRejectNonUtcTimestamp()
    {
        var message =
            new OutboxMessage(
                Guid.NewGuid(),
                DateTime.UtcNow,
                "TestEvent",
                "{}",
                "correlation-test-123");

        var nonUtcTimestamp =
            DateTime.SpecifyKind(
                DateTime.Now,
                DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () => message.MarkProcessed(
                nonUtcTimestamp));

        Assert.Null(
            message.ProcessedAtUtc);
    }

    [Fact]
    public void MarkProcessed_ShouldRejectAlreadyProcessedMessage()
    {
        var message =
            new OutboxMessage(
                Guid.NewGuid(),
                DateTime.UtcNow,
                "TestEvent",
                "{}",
                "correlation-test-123");

        message.MarkProcessed(
            DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(
            () => message.MarkProcessed(
                DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_ShouldPreserveCorrelationId()
    {
        const string correlationId =
            "request-correlation-123";

        var message =
            new OutboxMessage(
                Guid.NewGuid(),
                DateTime.UtcNow,
                "TestEvent",
                "{}",
                correlationId);

        Assert.Equal(
            correlationId,
            message.CorrelationId);
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyCorrelationId()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new OutboxMessage(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    "TestEvent",
                    "{}",
                    string.Empty));
    }
}