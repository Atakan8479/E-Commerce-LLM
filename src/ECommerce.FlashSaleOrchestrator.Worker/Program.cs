using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application
    .IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Infrastructure;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.Embeddings;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;
using ECommerce.FlashSaleOrchestrator.Worker
    .BackgroundServices;
using ECommerce.FlashSaleOrchestrator.Worker
    .IntegrationEvents.Inventory;
using ECommerce.FlashSaleOrchestrator.Worker
    .Messaging.DeadLetter;
using ECommerce.FlashSaleOrchestrator.Worker
    .Messaging.Kafka;
using ECommerce.FlashSaleOrchestrator.Worker
    .Resilience;

var builder =
    Host.CreateApplicationBuilder(args);

var sqlConnectionString =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_SQL_CONNECTION");

if (string.IsNullOrWhiteSpace(
        sqlConnectionString))
{
    throw new InvalidOperationException(
        "Environment variable " +
        "'FLASHSALE_SQL_CONNECTION' " +
        "must be configured.");
}

var openAiModelId =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_OPENAI_MODEL_ID");

if (string.IsNullOrWhiteSpace(
        openAiModelId))
{
    throw new InvalidOperationException(
        "Environment variable " +
        "'FLASHSALE_OPENAI_MODEL_ID' " +
        "must be configured.");
}

var openAiEndpointValue =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_OPENAI_ENDPOINT");

if (string.IsNullOrWhiteSpace(
        openAiEndpointValue))
{
    throw new InvalidOperationException(
        "Environment variable " +
        "'FLASHSALE_OPENAI_ENDPOINT' " +
        "must be configured.");
}

if (!Uri.TryCreate(
        openAiEndpointValue,
        UriKind.Absolute,
        out var openAiEndpoint) ||
    (openAiEndpoint.Scheme != Uri.UriSchemeHttp &&
     openAiEndpoint.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException(
        "Environment variable " +
        "'FLASHSALE_OPENAI_ENDPOINT' " +
        "must contain a valid absolute " +
        "HTTP or HTTPS URI.");
}

var llmRequestTimeoutSecondsValue =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_LLM_REQUEST_TIMEOUT_SECONDS");

if (!int.TryParse(
        llmRequestTimeoutSecondsValue,
        out var llmRequestTimeoutSeconds) ||
    llmRequestTimeoutSeconds <= 0)
{
    throw new InvalidOperationException(
        "Environment variable " +
        "'FLASHSALE_LLM_REQUEST_TIMEOUT_SECONDS' " +
        "must contain a positive integer number of seconds.");
}

var llmRequestTimeout =
    TimeSpan.FromSeconds(
        llmRequestTimeoutSeconds);

var openAiApiKey =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_OPENAI_API_KEY");

if (string.IsNullOrWhiteSpace(
        openAiApiKey))
{
    throw new InvalidOperationException(
        "Environment variable " +
        "'FLASHSALE_OPENAI_API_KEY' " +
        "must be configured.");
}

var embeddingModelId =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_EMBEDDING_MODEL_ID");

if (string.IsNullOrWhiteSpace(
        embeddingModelId))
{
    throw new InvalidOperationException(
        "Environment variable " +
        "'FLASHSALE_EMBEDDING_MODEL_ID' " +
        "must be configured.");
}

var redisEndpoint =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_REDIS_ENDPOINT");

if (string.IsNullOrWhiteSpace(
        redisEndpoint))
{
    redisEndpoint =
        "localhost:6379";
}

var redisConnectTimeoutSecondsValue =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_REDIS_CONNECT_TIMEOUT_SECONDS");

if (!int.TryParse(
        redisConnectTimeoutSecondsValue,
        out var redisConnectTimeoutSeconds) ||
    redisConnectTimeoutSeconds <= 0)
{
    throw new InvalidOperationException(
        "Environment variable " +
        "'FLASHSALE_REDIS_CONNECT_TIMEOUT_SECONDS' " +
        "must contain a positive integer number of seconds.");
}

var redisConnectTimeout =
    TimeSpan.FromSeconds(
        redisConnectTimeoutSeconds);

var redisOperationTimeoutSecondsValue =
    Environment.GetEnvironmentVariable(
        "FLASHSALE_REDIS_OPERATION_TIMEOUT_SECONDS");

if (!int.TryParse(
        redisOperationTimeoutSecondsValue,
        out var redisOperationTimeoutSeconds) ||
    redisOperationTimeoutSeconds <= 0)
{
    throw new InvalidOperationException(
        "Environment variable " +
        "'FLASHSALE_REDIS_OPERATION_TIMEOUT_SECONDS' " +
        "must contain a positive integer number of seconds.");
}

var redisOperationTimeout =
    TimeSpan.FromSeconds(
        redisOperationTimeoutSeconds);

var redisPassword =
    Environment.GetEnvironmentVariable(
        "REDIS_PASSWORD");

if (string.IsNullOrWhiteSpace(
        redisPassword))
{
    throw new InvalidOperationException(
        "Environment variable " +
        "'REDIS_PASSWORD' " +
        "must be configured.");
}

const int semanticEmbeddingDimensions =
    1024;

const double semanticCacheSimilarityThreshold =
    0.90;

var semanticCacheEntryTimeToLive =
    TimeSpan.FromHours(
        6);

const string semanticCacheIndexName =
    "flashsale:semantic-recommendations:idx";

const string semanticCacheKeyPrefix =
    "flashsale:semantic-recommendation:";

const string recommendationPromptVersion =
    "alternative-recommendation-prompt-v1";

const string recommendationSchemaVersion =
    "alternative-recommendation-result-v1";

const string semanticCacheVersion =
    "semantic-recommendation-cache-v1";

const string embeddingProfileVersion =
    "qwen3-embedding-0.6b-1024-v1";

builder.Services.AddInfrastructure(
    sqlConnectionString);

builder.Services.AddSemanticRecommendationEmbedding(
    embeddingModelId,
    openAiEndpoint,
    openAiApiKey,
    semanticEmbeddingDimensions);

builder.Services.AddSemanticRecommendationCache(
    redisEndpoint,
    redisPassword,
    redisConnectTimeout,
    redisOperationTimeout,
    semanticCacheIndexName,
    semanticCacheKeyPrefix,
    semanticEmbeddingDimensions,
    semanticCacheSimilarityThreshold,
    semanticCacheEntryTimeToLive,
    recommendationPromptVersion,
    recommendationSchemaVersion,
    semanticCacheVersion,
    embeddingProfileVersion);

builder.Services.AddAlternativeRecommendationAi(
    openAiModelId,
    openAiEndpoint,
    openAiApiKey,
    llmRequestTimeout);

builder.Services.AddSingleton(
    TimeProvider.System);

builder.Services.AddScoped<
    IStockDepletedRecommendationOrchestrator,
    StockDepletedRecommendationOrchestrator>();

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
        "Stock depleted dead-letter Kafka topic " +
        "must be configured.")
    .ValidateOnStart();

builder.Services
    .AddOptions<EventProcessingRetryOptions>()
    .Bind(
        builder.Configuration.GetSection(
            EventProcessingRetryOptions.SectionName))
    .Validate(
        options =>
            options.MaxAttempts is >= 1 and <= 10,
        "Event processing retry attempts " +
        "must be between 1 and 10.")
    .Validate(
        options =>
            options.InitialDelay >= TimeSpan.Zero,
        "Event processing retry initial delay " +
        "cannot be negative.")
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