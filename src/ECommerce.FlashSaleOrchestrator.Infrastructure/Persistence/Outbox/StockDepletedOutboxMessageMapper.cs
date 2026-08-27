using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory.Events;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;

public sealed class StockDepletedOutboxMessageMapper
{
    private static readonly string DomainEventType =
        typeof(StockDepletedDomainEvent).FullName
        ?? nameof(StockDepletedDomainEvent);

    public string SourceEventType =>
        DomainEventType;

    public bool CanMap(
        OutboxMessage outboxMessage)
    {
        ArgumentNullException.ThrowIfNull(
            outboxMessage);

        return string.Equals(
            outboxMessage.Type,
            DomainEventType,
            StringComparison.Ordinal);
    }

    public StockDepletedIntegrationEvent Map(
        OutboxMessage outboxMessage)
    {
        ArgumentNullException.ThrowIfNull(
            outboxMessage);

        if (!CanMap(
            outboxMessage))
        {
            throw new InvalidOperationException(
                $"Outbox message type '{outboxMessage.Type}' cannot be mapped " +
                $"to {nameof(StockDepletedIntegrationEvent)}.");
        }

        using var payload =
            JsonDocument.Parse(
                outboxMessage.Payload);

        if (!payload.RootElement.TryGetProperty(
                "ProductId",
                out var productIdElement)
            || !productIdElement.TryGetProperty(
                "Value",
                out var productIdValueElement)
            || !productIdValueElement.TryGetGuid(
                out var productId)
            || productId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Outbox message '{outboxMessage.Id}' contains an invalid ProductId.");
        }

        var occurredAtUtc =
            NormalizeUtc(
                outboxMessage.OccurredAtUtc);

        return new StockDepletedIntegrationEvent(
            outboxMessage.Id,
            occurredAtUtc,
            productId,
            outboxMessage.CorrelationId);
    }

    private static DateTime NormalizeUtc(
        DateTime timestamp)
    {
        return timestamp.Kind switch
        {
            DateTimeKind.Utc =>
                timestamp,

            DateTimeKind.Unspecified =>
                DateTime.SpecifyKind(
                    timestamp,
                    DateTimeKind.Utc),

            DateTimeKind.Local =>
                timestamp.ToUniversalTime(),

            _ =>
                throw new InvalidOperationException(
                    "Unsupported DateTime kind.")
        };
    }
}