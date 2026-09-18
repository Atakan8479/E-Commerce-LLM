using ECommerce.FlashSaleOrchestrator.Api.BackgroundServices;
using ECommerce.FlashSaleOrchestrator.Api.ExceptionHandling;
using ECommerce.FlashSaleOrchestrator.Api.Middleware;
using ECommerce.FlashSaleOrchestrator.Api.Validation;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .Carts.GetCart;
using ECommerce.FlashSaleOrchestrator.Application
    .Inventory.DecreaseStock;
using ECommerce.FlashSaleOrchestrator.Application
    .Inventory.GetInventory;
using ECommerce.FlashSaleOrchestrator.Application
    .Products.GetProduct;
using ECommerce.FlashSaleOrchestrator.Infrastructure;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Messaging.Kafka;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.GetRecommendationPlans;

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

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(
        options =>
        {
            options.InvalidModelStateResponseFactory =
                ApiValidationProblemDetailsFactory
                    .Create;
        });

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddHealthChecks();

builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(
    sqlConnectionString);

builder.Services.AddScoped<
    IQueryHandler<
        GetCartQuery,
        CartResult?>,
    GetCartQueryHandler>();

builder.Services.AddScoped<
    IQueryHandler<
        GetProductQuery,
        ProductResult?>,
    GetProductQueryHandler>();

builder.Services.AddScoped<
    IQueryHandler<
        GetInventoryQuery,
        InventoryResult?>,
    GetInventoryQueryHandler>();

builder.Services.AddScoped<
    ICommandHandler<
        DecreaseStockCommand,
        DecreaseStockResult>,
    DecreaseStockCommandHandler>();

builder.Services.AddScoped<
    IQueryHandler<
        GetRecommendationPlansByCorrelationIdQuery,
        IReadOnlyList<RecommendationPlanResult>>,
    GetRecommendationPlansByCorrelationIdQueryHandler>();

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

app.UseMiddleware<
    CorrelationIdMiddleware>();

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHealthChecks(
    "/health");

app.MapControllers();

app.Run();