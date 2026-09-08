using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using StackExchange.Redis;
using static NRedisStack.Search.Schema;

namespace ECommerce.FlashSaleOrchestrator
    .Infrastructure.IntegrationTests.AI.SemanticCaching;

public sealed class
    RedisSemanticRecommendationCacheTests
    : IDisposable
{
    private readonly ConnectionMultiplexer
        _connection;

    private readonly IDatabase
        _database;

    private readonly
        RedisSemanticRecommendationCacheOptions
        _options;

    private readonly
        RedisSemanticRecommendationCache
        _cache;

    public RedisSemanticRecommendationCacheTests()
    {
        var redisPassword =
            Environment.GetEnvironmentVariable(
                "REDIS_PASSWORD");

        if (string.IsNullOrWhiteSpace(
                redisPassword))
        {
            throw new InvalidOperationException(
                "REDIS_PASSWORD must be configured " +
                "for Redis integration tests.");
        }

        var configuration =
            new ConfigurationOptions
            {
                Password =
                    redisPassword,

                AbortOnConnectFail =
                    false
            };

        configuration.EndPoints.Add(
            "localhost",
            6379);

        _connection =
            ConnectionMultiplexer.Connect(
                configuration);

        _database =
            _connection.GetDatabase();

        var testId =
            Guid.NewGuid()
                .ToString("N");

        _options =
            new RedisSemanticRecommendationCacheOptions(
                $"test:semantic-cache:{testId}:idx",
                $"test:semantic-cache:{testId}:",
                1024,
                0.90,
                TimeSpan.FromMinutes(10));

        CreateIndex();

        _cache =
            new RedisSemanticRecommendationCache(
                _database,
                _options);
    }

    [Fact]
    public async Task StoreAsync_ShouldPersistHashAndTimeToLive()
    {
        var entry =
            CreateEntry(
                "entry-1",
                CreateAxisEmbedding(
                    0),
                CreateCompatibility(),
                Guid.Parse(
                    "22222222-2222-2222-2222-222222222222"));

        await _cache.StoreAsync(
            entry);

        var key =
            RedisSemanticRecommendationCacheSchema
                .BuildKey(
                    _options.KeyPrefix,
                    entry.EntryId);

        Assert.True(
            await _database.KeyExistsAsync(
                key));

        var embeddingValue =
            await _database.HashGetAsync(
                key,
                RedisSemanticRecommendationCacheSchema
                    .Embedding);

        Assert.False(
            embeddingValue.IsNull);

        var embeddingBytes =
            (byte[])embeddingValue!;

        Assert.Equal(
            4096,
            embeddingBytes.Length);

        var ttl =
            await _database.KeyTimeToLiveAsync(
                key);

        Assert.NotNull(
            ttl);

        Assert.True(
            ttl > TimeSpan.Zero);

        Assert.True(
            ttl <= _options.EntryTimeToLive);
    }

    [Fact]
    public async Task FindAsync_ShouldReturnNearestCompatibleEntry()
    {
        var compatibility =
            CreateCompatibility();

        var nearestEntry =
            CreateEntry(
                "entry-nearest",
                CreateAxisEmbedding(
                    0),
                compatibility,
                Guid.Parse(
                    "22222222-2222-2222-2222-222222222222"));

        var fartherEntry =
            CreateEntry(
                "entry-farther",
                CreateAxisEmbedding(
                    1),
                compatibility,
                Guid.Parse(
                    "44444444-4444-4444-4444-444444444444"));

        await _cache.StoreAsync(
            nearestEntry);

        await _cache.StoreAsync(
            fartherEntry);

        var lookup =
            new SemanticRecommendationCacheLookup(
                CreateAxisEmbedding(
                    0),
                compatibility);

        var match =
            await _cache.FindAsync(
                lookup);

        Assert.NotNull(
            match);

        Assert.Equal(
            "entry-nearest",
            match.EntryId);

        Assert.Equal(
            1d,
            match.SimilarityScore,
            precision: 6);

        Assert.Single(
            match.Result.Recommendations);

        Assert.Equal(
            Guid.Parse(
                "22222222-2222-2222-2222-222222222222"),
            match.Result
                .Recommendations[0]
                .ProductId);
    }

    [Fact]
    public async Task FindAsync_ShouldReturnNull_WhenCompatibilityDoesNotMatch()
    {
        var storedCompatibility =
            CreateCompatibility();

        var entry =
            CreateEntry(
                "entry-1",
                CreateAxisEmbedding(
                    0),
                storedCompatibility,
                Guid.Parse(
                    "22222222-2222-2222-2222-222222222222"));

        await _cache.StoreAsync(
            entry);

        var incompatibleLookup =
            new SemanticRecommendationCacheLookup(
                CreateAxisEmbedding(
                    0),
                storedCompatibility with
                {
                    CandidateFingerprint =
                        "different-fingerprint"
                });

        var match =
            await _cache.FindAsync(
                incompatibleLookup);

        Assert.Null(
            match);
    }

    [Fact]
    public async Task FindAsync_ShouldReturnNull_WhenSimilarityIsBelowThreshold()
    {
        var compatibility =
            CreateCompatibility();

        var entry =
            CreateEntry(
                "entry-orthogonal",
                CreateAxisEmbedding(
                    1),
                compatibility,
                Guid.Parse(
                    "22222222-2222-2222-2222-222222222222"));

        await _cache.StoreAsync(
            entry);

        var lookup =
            new SemanticRecommendationCacheLookup(
                CreateAxisEmbedding(
                    0),
                compatibility);

        var match =
            await _cache.FindAsync(
                lookup);

        Assert.Null(
            match);
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteStoredEntry()
    {
        var entry =
            CreateEntry(
                "entry-remove",
                CreateAxisEmbedding(
                    0),
                CreateCompatibility(),
                Guid.Parse(
                    "22222222-2222-2222-2222-222222222222"));

        await _cache.StoreAsync(
            entry);

        var key =
            RedisSemanticRecommendationCacheSchema
                .BuildKey(
                    _options.KeyPrefix,
                    entry.EntryId);

        Assert.True(
            await _database.KeyExistsAsync(
                key));

        await _cache.RemoveAsync(
            entry.EntryId);

        Assert.False(
            await _database.KeyExistsAsync(
                key));
    }

    public void Dispose()
    {
        try
        {
            _database
                .FT()
                .DropIndex(
                    _options.IndexName,
                    dd: true);
        }
        catch (RedisServerException exception)
            when (
                exception.Message.Contains(
                    "not found",
                    StringComparison.OrdinalIgnoreCase))
        {
        }

        _connection.Dispose();
    }

    private void CreateIndex()
    {
        var schema =
            new Schema()
                .AddTagField(
                    new FieldName(
                        RedisSemanticRecommendationCacheSchema
                            .CandidateFingerprint,
                        RedisSemanticRecommendationCacheSchema
                            .CandidateFingerprint))
                .AddTagField(
                    new FieldName(
                        RedisSemanticRecommendationCacheSchema
                            .PromptVersion,
                        RedisSemanticRecommendationCacheSchema
                            .PromptVersion))
                .AddTagField(
                    new FieldName(
                        RedisSemanticRecommendationCacheSchema
                            .SchemaVersion,
                        RedisSemanticRecommendationCacheSchema
                            .SchemaVersion))
                .AddTagField(
                    new FieldName(
                        RedisSemanticRecommendationCacheSchema
                            .SemanticCacheVersion,
                        RedisSemanticRecommendationCacheSchema
                            .SemanticCacheVersion))
                .AddTagField(
                    new FieldName(
                        RedisSemanticRecommendationCacheSchema
                            .EmbeddingProfileVersion,
                        RedisSemanticRecommendationCacheSchema
                            .EmbeddingProfileVersion))
                .AddVectorField(
                    RedisSemanticRecommendationCacheSchema
                        .Embedding,
                    VectorField.VectorAlgo.FLAT,
                    new Dictionary<string, object>
                    {
                        ["TYPE"] =
                            "FLOAT32",

                        ["DIM"] =
                            _options.VectorDimensions
                                .ToString(),

                        ["DISTANCE_METRIC"] =
                            "COSINE"
                    });

        _database
            .FT()
            .Create(
                _options.IndexName,
                new FTCreateParams()
                    .On(
                        IndexDataType.HASH)
                    .Prefix(
                        _options.KeyPrefix),
                schema);
    }

    private static
        SemanticRecommendationCacheCompatibility
        CreateCompatibility()
    {
        return new SemanticRecommendationCacheCompatibility(
            "candidate-fingerprint-v1",
            "prompt-v1.2",
            "schema-v1",
            "cache-v1",
            "qwen3-embedding-0.6b-1024-v1");
    }

    private static
        SemanticRecommendationCacheEntry
        CreateEntry(
            string entryId,
            SemanticRecommendationEmbedding embedding,
            SemanticRecommendationCacheCompatibility
                compatibility,
            Guid productId)
    {
        return new SemanticRecommendationCacheEntry(
            entryId,
            embedding,
            compatibility,
            new AlternativeRecommendationResult(
                [
                    new AlternativeRecommendation(
                        productId,
                        $"Recommendation for {entryId}")
                ]),
            DateTime.UtcNow);
    }

    private static
        SemanticRecommendationEmbedding
        CreateAxisEmbedding(
            int axis)
    {
        var vector =
            new float[1024];

        vector[axis] =
            1f;

        return new SemanticRecommendationEmbedding(
            vector);
    }
}