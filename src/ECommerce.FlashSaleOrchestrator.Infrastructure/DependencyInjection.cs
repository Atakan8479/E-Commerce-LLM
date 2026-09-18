using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Observability;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Observability;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Inbox;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AlternativeCandidates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Persistence;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Repositories;

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

        services.AddScoped<
            ICorrelationContext,
            CorrelationContext>();

        services.AddDbContext<
            FlashSaleOrchestratorDbContext>(
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

        services.AddScoped<
            IAlternativeCandidateProvider,
            SqlAlternativeCandidateProvider>();

        services.AddScoped<
            IAlternativeRecommendationPlanRepository,
            AlternativeRecommendationPlanRepository>();

        services.AddScoped<IUnitOfWork>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    FlashSaleOrchestratorDbContext>());

        services.AddScoped<
            ICartRepository,
            CartRepository>();

        services.AddScoped<
            IInventoryRepository,
            InventoryRepository>();

        services.AddScoped<
            IProductRepository,
            ProductRepository>();

        return services;
    }
}