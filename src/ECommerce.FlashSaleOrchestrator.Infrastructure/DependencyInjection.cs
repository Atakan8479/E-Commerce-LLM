using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Inbox;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);

        services.AddDbContext<FlashSaleOrchestratorDbContext>(
            options =>
                options.UseSqlServer(
                    connectionString));

        services.AddSingleton<
            StockDepletedOutboxMessageMapper>();

        services.AddScoped<
            OutboxProcessor>();

        services.AddScoped(
            typeof(IIntegrationEventProcessor<>),
            typeof(InboxIntegrationEventProcessor<>));

        return services;
    }
}