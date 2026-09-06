using ECommerce.FlashSaleOrchestrator.Application.Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Infrastructure;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI;
using ECommerce.FlashSaleOrchestrator.Worker.BackgroundServices;
using ECommerce.FlashSaleOrchestrator.Worker.IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.DeadLetter;
using ECommerce.FlashSaleOrchestrator.Worker.Messaging.Kafka;
using ECommerce.FlashSaleOrchestrator.Worker.Resilience;

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

var openAiModelId =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_OPENAI_MODEL_ID");

if (string.IsNullOrWhiteSpace(
    openAiModelId))
{
    throw new InvalidOperationException(
        "Environment variable 'FLASHSALE_OPENAI_MODEL_ID' must be configured.");
}

var openAiEndpointValue =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_OPENAI_ENDPOINT");

if (string.IsNullOrWhiteSpace(
    openAiEndpointValue))
{
    throw new InvalidOperationException(
        "Environment variable 'FLASHSALE_OPENAI_ENDPOINT' must be configured.");
}

if (!Uri.TryCreate(
        openAiEndpointValue,
        UriKind.Absolute,
        out var openAiEndpoint) ||
    (openAiEndpoint.Scheme != Uri.UriSchemeHttp &&
     openAiEndpoint.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException(
        "Environment variable 'FLASHSALE_OPENAI_ENDPOINT' must contain a valid absolute HTTP or HTTPS URI.");
}

var openAiApiKey =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_OPENAI_API_KEY");

if (string.IsNullOrWhiteSpace(
    openAiApiKey))
{
    throw new InvalidOperationException(
        "Environment variable 'FLASHSALE_OPENAI_API_KEY' must be configured.");
}

builder.Services.AddInfrastructure(
    sqlConnectionString);

builder.Services.AddAlternativeRecommendationAi(
    openAiModelId,
    openAiEndpoint,
    openAiApiKey);

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
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(
                options.StockDepletedDeadLetterTopic),
        "Stock depleted dead-letter Kafka topic must be configured.")
    .ValidateOnStart();

builder.Services
    .AddOptions<EventProcessingRetryOptions>()
    .Bind(
        builder.Configuration.GetSection(
            EventProcessingRetryOptions.SectionName))
    .Validate(
        options =>
            options.MaxAttempts is >= 1 and <= 10,
        "Event processing retry attempts must be between 1 and 10.")
    .Validate(
        options =>
            options.InitialDelay >= TimeSpan.Zero,
        "Event processing retry initial delay cannot be negative.")
    .ValidateOnStart();

builder.Services.AddSingleton<
    IntegrationEventRetryExecutor>();

builder.Services.AddSingleton<
    IDeadLetterPublisher,
    KafkaDeadLetterPublisher>();

builder.Services.AddScoped<
    IIntegrationEventHandler<
        StockDepletedIntegrationEvent>,
    StockDepletedIntegrationEventHandler>();

builder.Services.AddHostedService<
    StockDepletedConsumerWorker>();

var host =
    builder.Build();

await host.RunAsync();