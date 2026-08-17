using ECommerce.FlashSaleOrchestrator.Api.BackgroundServices;
using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Infrastructure;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Messaging.Kafka;

var builder =
    WebApplication.CreateBuilder(args);

var sqlConnectionString =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_SQL_CONNECTION");

if (string.IsNullOrWhiteSpace(
    sqlConnectionString))
{
    throw new InvalidOperationException(
        "Environment variable 'FLASHSALE_SQL_CONNECTION' must be configured.");
}

builder.Services.AddControllers();

builder.Services.AddHealthChecks();

builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(
    sqlConnectionString);

builder.Services
    .AddOptions<OutboxPublisherOptions>()
    .Bind(
        builder.Configuration.GetSection(
            OutboxPublisherOptions.SectionName))
    .Validate(
        options =>
            options.BatchSize > 0,
        "Outbox publisher batch size must be greater than zero.")
    .Validate(
        options =>
            options.PollingInterval > TimeSpan.Zero,
        "Outbox publisher polling interval must be greater than zero.")
    .ValidateOnStart();

builder.Services
    .AddOptions<KafkaPublisherOptions>()
    .Bind(
        builder.Configuration.GetSection(
            KafkaPublisherOptions.SectionName))
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
    .ValidateOnStart();

builder.Services.AddSingleton<
    IEventPublisher,
    KafkaEventPublisher>();

builder.Services.AddHostedService<
    OutboxPublisherWorker>();

var app =
    builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHealthChecks(
    "/health");

app.MapControllers();

app.Run();