using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker.BackgroundServices;
using ECommerce.FlashSaleOrchestrator.Worker.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.Kafka;
using ECommerce.FlashSaleOrchestrator.Infrastructure;

var builder =
    Host.CreateApplicationBuilder(args);

var sqlConnectionString =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_SQL_CONNECTION");

if (string.IsNullOrWhiteSpace(
    sqlConnectionString))
{
    throw new InvalidOperationException(
        "Environment variable 'FLASHSALE_SQL_CONNECTION' must be configured.");
}

builder.Services.AddInfrastructure(
    sqlConnectionString);

builder.Services
    .AddOptions<KafkaConsumerOptions>()
    .Bind(
        builder.Configuration.GetSection(
            KafkaConsumerOptions.SectionName))
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(
                options.BootstrapServers),
        "Kafka bootstrap servers must be configured.")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(
                options.StockDepletedTopic),
        "Stock depleted Kafka topic must be configured.")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(
                options.ConsumerGroupId),
        "Kafka consumer group id must be configured.")
    .ValidateOnStart();

builder.Services.AddScoped<
    IIntegrationEventHandler<
        StockDepletedIntegrationEvent>,
    StockDepletedIntegrationEventHandler>();

builder.Services.AddHostedService<
    StockDepletedConsumerWorker>();

var host =
    builder.Build();

await host.RunAsync();