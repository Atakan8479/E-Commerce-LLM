using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Domain.Abstractions;
using ECommerce.FlashSaleOrchestrator.Domain.Carts;
using ECommerce.FlashSaleOrchestrator.Domain.FlashSales;
using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Inbox;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;

public sealed class FlashSaleOrchestratorDbContext
    : DbContext,
      IUnitOfWork
{
    public FlashSaleOrchestratorDbContext(
        DbContextOptions<FlashSaleOrchestratorDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products =>
        Set<Product>();

    public DbSet<InventoryItem> InventoryItems =>
        Set<InventoryItem>();

    public DbSet<Cart> Carts =>
        Set<Cart>();

    public DbSet<FlashSale> FlashSales =>
        Set<FlashSale>();

    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages =>
        Set<InboxMessage>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FlashSaleOrchestratorDbContext)
                .Assembly);
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var aggregatesWithDomainEvents =
            ChangeTracker
                .Entries<IHasDomainEvents>()
                .Select(entry => entry.Entity)
                .Where(aggregate =>
                    aggregate.DomainEvents.Count > 0)
                .ToArray();

        var domainEvents =
            aggregatesWithDomainEvents
                .SelectMany(aggregate =>
                    aggregate.DomainEvents)
                .ToArray();

        var outboxMessages =
            domainEvents
                .Select(CreateOutboxMessage)
                .ToArray();

        if (outboxMessages.Length > 0)
        {
            OutboxMessages.AddRange(
                outboxMessages);
        }

        try
        {
            var result =
                await base.SaveChangesAsync(
                    cancellationToken);

            foreach (var aggregate in
                     aggregatesWithDomainEvents)
            {
                aggregate.ClearDomainEvents();
            }

            return result;
        }
        catch
        {
            foreach (var outboxMessage in
                     outboxMessages)
            {
                Entry(outboxMessage).State =
                    EntityState.Detached;
            }

            throw;
        }
    }

    async Task IUnitOfWork.SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await SaveChangesAsync(
            cancellationToken);
    }

    private static OutboxMessage CreateOutboxMessage(
        IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(
            domainEvent);

        var eventType =
            domainEvent.GetType();

        var type =
            eventType.FullName
            ?? eventType.Name;

        var payload =
            JsonSerializer.Serialize(
                domainEvent,
                eventType);

        return new OutboxMessage(
            Guid.NewGuid(),
            DateTime.UtcNow,
            type,
            payload);
    }
}