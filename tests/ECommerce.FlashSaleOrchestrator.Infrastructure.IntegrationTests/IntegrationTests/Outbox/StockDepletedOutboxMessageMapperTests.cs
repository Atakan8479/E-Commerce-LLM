using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory.Events;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.Outbox;

public sealed class StockDepletedOutboxMessageMapperTests
{
    [Fact]
    public void Map_ShouldPreserveOutboxIdentityAndOccurredAtUtc()
    {
        var eventId =
            Guid.NewGuid();

        var productId =
            Guid.NewGuid();

        var occurredAtUtc =
            new DateTime(
                2026,
                8,
                16,
                12,
                30,
                0,
                DateTimeKind.Utc);

        var payload =
            JsonSerializer.Serialize(
                new
                {
                    ProductId = new
                    {
                        Value = productId
                    }
                });

        var message =
            new OutboxMessage(
                eventId,
                occurredAtUtc,
                typeof(StockDepletedDomainEvent).FullName!,
                payload);

        var mapper =
            new StockDepletedOutboxMessageMapper();

        var integrationEvent =
            mapper.Map(message);

        Assert.Equal(
            eventId,
            integrationEvent.EventId);

        Assert.Equal(
            occurredAtUtc,
            integrationEvent.OccurredAtUtc);

        Assert.Equal(
            productId,
            integrationEvent.ProductId);
    }

    [Fact]
    public void CanMap_ShouldReturnTrue_ForStockDepletedDomainEvent()
    {
        var message =
            CreateMessage(
                typeof(StockDepletedDomainEvent).FullName!);

        var mapper =
            new StockDepletedOutboxMessageMapper();

        Assert.True(
            mapper.CanMap(message));
    }

    [Fact]
    public void CanMap_ShouldReturnFalse_ForUnsupportedEvent()
    {
        var message =
            CreateMessage(
                "Some.Other.DomainEvent");

        var mapper =
            new StockDepletedOutboxMessageMapper();

        Assert.False(
            mapper.CanMap(message));
    }

    [Fact]
    public void Map_ShouldRejectUnsupportedEvent()
    {
        var message =
            CreateMessage(
                "Some.Other.DomainEvent");

        var mapper =
            new StockDepletedOutboxMessageMapper();

        Assert.Throws<InvalidOperationException>(
            () => mapper.Map(message));
    }

    [Fact]
    public void Map_ShouldRejectPayloadWithoutProductId()
    {
        var message =
            new OutboxMessage(
                Guid.NewGuid(),
                DateTime.UtcNow,
                typeof(StockDepletedDomainEvent).FullName!,
                "{}");

        var mapper =
            new StockDepletedOutboxMessageMapper();

        Assert.Throws<InvalidOperationException>(
            () => mapper.Map(message));
    }

    private static OutboxMessage CreateMessage(
        string type)
    {
        var productId =
            Guid.NewGuid();

        var payload =
            JsonSerializer.Serialize(
                new
                {
                    ProductId = new
                    {
                        Value = productId
                    }
                });

        return new OutboxMessage(
            Guid.NewGuid(),
            DateTime.UtcNow,
            type,
            payload);
    }
}