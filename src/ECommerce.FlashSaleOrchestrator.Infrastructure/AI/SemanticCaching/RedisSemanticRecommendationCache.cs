using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations.SemanticCaching;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using StackExchange.Redis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using static NRedisStack.Search.Schema;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;

internal sealed class
    RedisSemanticRecommendationCache
    : ISemanticRecommendationCache
{
    private static readonly JsonSerializerOptions
        SerializerOptions =
            new(JsonSerializerDefaults.Web);

    private readonly IDatabase _database;

    private readonly
        RedisSemanticRecommendationCacheOptions
        _options;

    private readonly SemaphoreSlim
    _indexInitializationLock =
        new(
            1,
            1);

    private volatile bool
        _indexInitialized;

    public RedisSemanticRecommendationCache(
        IDatabase database,
        RedisSemanticRecommendationCacheOptions options)
    {
        _database =
            database ??
            throw new ArgumentNullException(
                nameof(database));

        _options =
            options ??
            throw new ArgumentNullException(
                nameof(options));
    }

    public async Task<SemanticRecommendationCacheMatch?>
        FindAsync(
            SemanticRecommendationCacheLookup lookup,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            lookup);

        ArgumentNullException.ThrowIfNull(
            lookup.Compatibility);

        cancellationToken.ThrowIfCancellationRequested();

        await EnsureIndexAsync(
            cancellationToken);

        var queryVector =
            RedisFloat32VectorSerializer.Serialize(
                lookup.Embedding,
                _options.VectorDimensions);

        var query =
            new Query(
                BuildSearchExpression(
                    lookup.Compatibility))
                .AddParam(
                    "queryVector",
                    queryVector)
                .ReturnFields(
                    RedisSemanticRecommendationCacheSchema
                        .EntryId,
                    RedisSemanticRecommendationCacheSchema
                        .ResultJson,
                    RedisSemanticRecommendationCacheSchema
                        .Distance)
                .SetSortBy(
                    RedisSemanticRecommendationCacheSchema
                        .Distance,
                    ascending: true)
                .Limit(
                    0,
                    1)
                .Dialect(
                    2);

        var searchResult =
            await _database
                .FT()
                .SearchAsync(
                    _options.IndexName,
                    query)
                .WaitAsync(
                    cancellationToken);

        if (searchResult.Documents.Count == 0)
        {
            return null;
        }

        var document =
            searchResult.Documents[0];

        var entryId =
            GetRequiredProperty(
                    document,
                    RedisSemanticRecommendationCacheSchema
                        .EntryId)
                .ToString();

        if (string.IsNullOrWhiteSpace(
                entryId))
        {
            throw new InvalidOperationException(
                "Redis semantic cache entry id is missing.");
        }

        var distanceValue =
            GetRequiredProperty(
                    document,
                    RedisSemanticRecommendationCacheSchema
                        .Distance)
                .ToString();

        if (!double.TryParse(
                distanceValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var distance) ||
            !double.IsFinite(
                distance))
        {
            throw new InvalidOperationException(
                "Redis semantic cache returned an invalid " +
                "vector distance.");
        }

        var similarity =
            1d -
            distance;

        if (similarity <
            _options.SimilarityThreshold)
        {
            return null;
        }

        var resultJson =
            GetRequiredProperty(
                    document,
                    RedisSemanticRecommendationCacheSchema
                        .ResultJson)
                .ToString();

        if (string.IsNullOrWhiteSpace(
                resultJson))
        {
            throw new InvalidOperationException(
                "Redis semantic cache result payload is missing.");
        }

        var result =
            JsonSerializer.Deserialize<
                AlternativeRecommendationResult>(
                resultJson,
                SerializerOptions);

        if (result is null)
        {
            throw new InvalidOperationException(
                "Redis semantic cache result payload " +
                "could not be deserialized.");
        }

        return new SemanticRecommendationCacheMatch(
            entryId,
            similarity,
            result);
    }

    public async Task StoreAsync(
        SemanticRecommendationCacheEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            entry);

        ArgumentNullException.ThrowIfNull(
            entry.Compatibility);

        ArgumentNullException.ThrowIfNull(
            entry.Result);

        cancellationToken.ThrowIfCancellationRequested();

        await EnsureIndexAsync(
            cancellationToken);

        var key =
            RedisSemanticRecommendationCacheSchema
                .BuildKey(
                    _options.KeyPrefix,
                    entry.EntryId);

        var embeddingBytes =
            RedisFloat32VectorSerializer.Serialize(
                entry.Embedding,
                _options.VectorDimensions);

        var resultJson =
            JsonSerializer.Serialize(
                entry.Result,
                SerializerOptions);

        var hashEntries =
            new HashEntry[]
            {
                new(
                    RedisSemanticRecommendationCacheSchema
                        .EntryId,
                    entry.EntryId),

                new(
                    RedisSemanticRecommendationCacheSchema
                        .CandidateFingerprint,
                    entry.Compatibility
                        .CandidateFingerprint),

                new(
                    RedisSemanticRecommendationCacheSchema
                        .PromptVersion,
                    entry.Compatibility
                        .PromptVersion),

                new(
                    RedisSemanticRecommendationCacheSchema
                        .SchemaVersion,
                    entry.Compatibility
                        .SchemaVersion),

                new(
                    RedisSemanticRecommendationCacheSchema
                        .SemanticCacheVersion,
                    entry.Compatibility
                        .SemanticCacheVersion),

                new(
                    RedisSemanticRecommendationCacheSchema
                        .EmbeddingProfileVersion,
                    entry.Compatibility
                        .EmbeddingProfileVersion),

                new(
                    RedisSemanticRecommendationCacheSchema
                        .ResultJson,
                    resultJson),

                new(
                    RedisSemanticRecommendationCacheSchema
                        .CreatedAtUtc,
                    entry.CreatedAtUtc
                        .ToUniversalTime()
                        .ToString(
                            "O",
                            CultureInfo.InvariantCulture)),

                new(
                    RedisSemanticRecommendationCacheSchema
                        .Embedding,
                    embeddingBytes)
            };

        var transaction =
            _database.CreateTransaction();

        var hashSetTask =
            transaction.HashSetAsync(
                key,
                hashEntries);

        var expiryTask =
            transaction.KeyExpireAsync(
                key,
                _options.EntryTimeToLive);

        var committed =
            await transaction
                .ExecuteAsync()
                .WaitAsync(
                    cancellationToken);

        if (!committed)
        {
            throw new InvalidOperationException(
                "Redis semantic cache transaction " +
                "was not committed.");
        }

        await Task
            .WhenAll(
                hashSetTask,
                expiryTask)
            .WaitAsync(
                cancellationToken);
    }

    public async Task RemoveAsync(
        string entryId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key =
            RedisSemanticRecommendationCacheSchema
                .BuildKey(
                    _options.KeyPrefix,
                    entryId);

        await _database
            .KeyDeleteAsync(
                key)
            .WaitAsync(
                cancellationToken);
    }

    private async Task EnsureIndexAsync(
    CancellationToken cancellationToken)
    {
        if (_indexInitialized)
        {
            return;
        }

        await _indexInitializationLock.WaitAsync(
            cancellationToken);

        try
        {
            if (_indexInitialized)
            {
                return;
            }

            var schema =
                BuildIndexSchema();

            var createParameters =
                new FTCreateParams()
                    .On(
                        IndexDataType.HASH)
                    .Prefix(
                        _options.KeyPrefix);

            try
            {
                var created =
                    await _database
                        .FT()
                        .CreateAsync(
                            _options.IndexName,
                            createParameters,
                            schema)
                        .WaitAsync(
                            cancellationToken);

                if (!created)
                {
                    throw new InvalidOperationException(
                        "Redis semantic cache index " +
                        "could not be created.");
                }
            }
            catch (RedisServerException exception)
                when (IsIndexAlreadyCreated(
                    exception))
            {
            }

            _indexInitialized =
                true;
        }
        finally
        {
            _indexInitializationLock.Release();
        }
    }

    private Schema BuildIndexSchema()
    {
        return new Schema()
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
    }

    private static bool IsIndexAlreadyCreated(
        RedisServerException exception)
    {
        return exception.Message.Contains(
            "already exists",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSearchExpression(
        SemanticRecommendationCacheCompatibility
            compatibility)
    {
        var builder =
            new StringBuilder();

        builder.Append('(');

        AppendTagFilter(
            builder,
            RedisSemanticRecommendationCacheSchema
                .CandidateFingerprint,
            compatibility.CandidateFingerprint);

        builder.Append(' ');

        AppendTagFilter(
            builder,
            RedisSemanticRecommendationCacheSchema
                .PromptVersion,
            compatibility.PromptVersion);

        builder.Append(' ');

        AppendTagFilter(
            builder,
            RedisSemanticRecommendationCacheSchema
                .SchemaVersion,
            compatibility.SchemaVersion);

        builder.Append(' ');

        AppendTagFilter(
            builder,
            RedisSemanticRecommendationCacheSchema
                .SemanticCacheVersion,
            compatibility.SemanticCacheVersion);

        builder.Append(' ');

        AppendTagFilter(
            builder,
            RedisSemanticRecommendationCacheSchema
                .EmbeddingProfileVersion,
            compatibility.EmbeddingProfileVersion);

        builder.Append(')');

        builder.Append(
            "=>[KNN 1 @");

        builder.Append(
            RedisSemanticRecommendationCacheSchema
                .Embedding);

        builder.Append(
            " $queryVector AS ");

        builder.Append(
            RedisSemanticRecommendationCacheSchema
                .Distance);

        builder.Append(']');

        return builder.ToString();
    }

    private static void AppendTagFilter(
        StringBuilder builder,
        string fieldName,
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            throw new ArgumentException(
                $"Redis semantic cache compatibility " +
                $"field '{fieldName}' is required.",
                nameof(value));
        }

        builder.Append('@');
        builder.Append(fieldName);
        builder.Append(":{");
        builder.Append(
            EscapeTagValue(
                value));
        builder.Append('}');
    }

    private static string EscapeTagValue(
        string value)
    {
        var builder =
            new StringBuilder(
                value.Length);

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(
                    character) ||
                character == '_')
            {
                builder.Append(
                    character);

                continue;
            }

            builder.Append('\\');
            builder.Append(
                character);
        }

        return builder.ToString();
    }

    private static RedisValue GetRequiredProperty(
        Document document,
        string propertyName)
    {
        foreach (var property in
                 document.GetProperties())
        {
            if (string.Equals(
                    property.Key,
                    propertyName,
                    StringComparison.Ordinal))
            {
                return property.Value;
            }
        }

        throw new InvalidOperationException(
            $"Redis semantic cache search result " +
            $"does not contain '{propertyName}'.");
    }
}