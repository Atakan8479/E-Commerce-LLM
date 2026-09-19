using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Observability;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;

internal sealed class CachedSemanticAlternativeRecommendationGenerator
    : IAlternativeRecommendationGenerator
{
    private readonly
        IUncachedAlternativeRecommendationGenerator
        _primaryGenerator;

    private readonly ISemanticRecommendationEmbeddingGenerator
        _embeddingGenerator;

    private readonly ISemanticRecommendationCache
        _cache;

    private readonly SemanticRecommendationRepresentationBuilder
        _representationBuilder;

    private readonly SemanticRecommendationCacheProfile
        _cacheProfile;

    private readonly ILogger<
        CachedSemanticAlternativeRecommendationGenerator>
        _logger;

    public CachedSemanticAlternativeRecommendationGenerator(
        IUncachedAlternativeRecommendationGenerator primaryGenerator,
        ISemanticRecommendationEmbeddingGenerator embeddingGenerator,
        ISemanticRecommendationCache cache,
        SemanticRecommendationRepresentationBuilder representationBuilder,
        SemanticRecommendationCacheProfile cacheProfile,
        ILogger<
            CachedSemanticAlternativeRecommendationGenerator>
            logger)
    {
        ArgumentNullException.ThrowIfNull(
            primaryGenerator);

        ArgumentNullException.ThrowIfNull(
            embeddingGenerator);

        ArgumentNullException.ThrowIfNull(
            cache);

        ArgumentNullException.ThrowIfNull(
            representationBuilder);

        ArgumentNullException.ThrowIfNull(
            cacheProfile);

        ArgumentNullException.ThrowIfNull(
            logger);

        _primaryGenerator =
            primaryGenerator;

        _embeddingGenerator =
            embeddingGenerator;

        _cache =
            cache;

        _representationBuilder =
            representationBuilder;

        _cacheProfile =
            cacheProfile;

        _logger =
            logger;
    }

    public async Task<AlternativeRecommendationResult>
        GenerateAsync(
            AlternativeRecommendationRequest request,
            CancellationToken cancellationToken = default)
    {
        var outcome =
            await ExecuteAsync(
                request,
                cancellationToken);

        return outcome.Result;
    }

    public async Task<AlternativeRecommendationGenerationOutcome>
        ExecuteAsync(
            AlternativeRecommendationRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken.ThrowIfCancellationRequested();

        var representation =
            _representationBuilder.Build(
                request);

        var compatibility =
            _cacheProfile.CreateCompatibility(
                representation.CandidateFingerprint);

        var cacheLookupStartedAt =
            Stopwatch.GetTimestamp();

        SemanticRecommendationEmbedding embedding;
        SemanticRecommendationCacheMatch? match;

        try
        {
            embedding =
                await _embeddingGenerator.GenerateAsync(
                    representation.Text,
                    cancellationToken);

            var lookup =
                new SemanticRecommendationCacheLookup(
                    embedding,
                    compatibility);

            match =
                await _cache.FindAsync(
                    lookup,
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var durationMs =
                Stopwatch.GetElapsedTime(
                        cacheLookupStartedAt)
                    .TotalMilliseconds;

            InfrastructureMetrics.SemanticCacheLookups.Add(
                1,
                new KeyValuePair<string, object?>(
                    "status",
                    "bypassed"));

            _logger.LogWarning(
                exception,
                "Semantic recommendation cache lookup was bypassed. " +
                "CorrelationId: {CorrelationId}, " +
                "CacheStatus: {CacheStatus}, " +
                "DurationMs: {DurationMs}",
                request.CorrelationId,
                "Bypassed",
                durationMs);

            return await GeneratePrimaryOutcomeAsync(
                request,
                cancellationToken);
        }

        if (match is not null)
        {
            try
            {
                AlternativeRecommendationResultValidator
                    .Validate(
                        request,
                        match.Result);

                var durationMs =
                    Stopwatch.GetElapsedTime(
                            cacheLookupStartedAt)
                        .TotalMilliseconds;

                InfrastructureMetrics.SemanticCacheLookups.Add(
                    1,
                    new KeyValuePair<string, object?>(
                        "status",
                        "hit"));

                _logger.LogInformation(
                    "Semantic recommendation cache hit. " +
                    "CorrelationId: {CorrelationId}, " +
                    "CacheStatus: {CacheStatus}, " +
                    "EntryId: {EntryId}, " +
                    "SimilarityScore: {SimilarityScore}, " +
                    "DurationMs: {DurationMs}",
                    request.CorrelationId,
                    "Hit",
                    match.EntryId,
                    match.SimilarityScore,
                    durationMs);

                return new AlternativeRecommendationGenerationOutcome(
                    match.Result,
                    AlternativeRecommendationSource.Cache);
            }
            catch (
                AlternativeRecommendationValidationException
                exception)
            {
                var durationMs =
                    Stopwatch.GetElapsedTime(
                            cacheLookupStartedAt)
                        .TotalMilliseconds;

                InfrastructureMetrics.SemanticCacheLookups.Add(
                    1,
                    new KeyValuePair<string, object?>(
                        "status",
                        "invalid"));

                _logger.LogWarning(
                    exception,
                    "Semantic recommendation cache entry is no longer valid. " +
                    "CorrelationId: {CorrelationId}, " +
                    "CacheStatus: {CacheStatus}, " +
                    "EntryId: {EntryId}, " +
                    "DurationMs: {DurationMs}",
                    request.CorrelationId,
                    "Invalid",
                    match.EntryId,
                    durationMs);

                try
                {
                    await _cache.RemoveAsync(
                        match.EntryId,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception removeException)
                {
                    _logger.LogWarning(
                        removeException,
                        "Invalid semantic recommendation cache entry " +
                        "could not be removed. " +
                        "CorrelationId: {CorrelationId}, " +
                        "EntryId: {EntryId}",
                        request.CorrelationId,
                        match.EntryId);

                    return await GeneratePrimaryOutcomeAsync(
                        request,
                        cancellationToken);
                }
            }
        }
        else
        {
            var durationMs =
                Stopwatch.GetElapsedTime(
                        cacheLookupStartedAt)
                    .TotalMilliseconds;

            InfrastructureMetrics.SemanticCacheLookups.Add(
                1,
                new KeyValuePair<string, object?>(
                    "status",
                    "miss"));

            _logger.LogDebug(
                "Semantic recommendation cache miss. " +
                "CorrelationId: {CorrelationId}, " +
                "CacheStatus: {CacheStatus}, " +
                "DurationMs: {DurationMs}",
                request.CorrelationId,
                "Miss",
                durationMs);
        }

        var outcome =
            await GeneratePrimaryOutcomeAsync(
                request,
                cancellationToken);

        try
        {
            var entry =
                new SemanticRecommendationCacheEntry(
                    Guid.NewGuid().ToString("N"),
                    embedding,
                    compatibility,
                    outcome.Result,
                    DateTime.UtcNow);

            await _cache.StoreAsync(
                entry,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Semantic recommendation cache write failed. " +
                "The generated recommendation will still be used. " +
                "CorrelationId: {CorrelationId}",
                request.CorrelationId);
        }

        return outcome;
    }

    private async Task<AlternativeRecommendationGenerationOutcome>
        GeneratePrimaryOutcomeAsync(
            AlternativeRecommendationRequest request,
            CancellationToken cancellationToken)
    {
        var generationStartedAt =
            Stopwatch.GetTimestamp();

        InfrastructureMetrics.LlmRequests.Add(
            1);

        try
        {
            var result =
                await _primaryGenerator.GenerateAsync(
                    request,
                    cancellationToken);

            AlternativeRecommendationResultValidator
                .Validate(
                    request,
                    result);

            var durationMs =
                Stopwatch.GetElapsedTime(
                        generationStartedAt)
                    .TotalMilliseconds;

            InfrastructureMetrics.LlmDuration.Record(
                durationMs,
                new KeyValuePair<string, object?>(
                    "outcome",
                    "success"));

            _logger.LogInformation(
                "LLM alternative recommendation request completed. " +
                "CorrelationId: {CorrelationId}, " +
                "ProductId: {ProductId}, " +
                "RecommendationCount: {RecommendationCount}, " +
                "DurationMs: {DurationMs}",
                request.CorrelationId,
                request.DepletedProduct.ProductId,
                result.Recommendations.Count,
                durationMs);

            return new AlternativeRecommendationGenerationOutcome(
                result,
                AlternativeRecommendationSource.Llm);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            var durationMs =
                Stopwatch.GetElapsedTime(
                        generationStartedAt)
                    .TotalMilliseconds;

            InfrastructureMetrics.LlmDuration.Record(
                durationMs,
                new KeyValuePair<string, object?>(
                    "outcome",
                    "cancelled"));

            throw;
        }
        catch (Exception exception)
        {
            var durationMs =
                Stopwatch.GetElapsedTime(
                        generationStartedAt)
                    .TotalMilliseconds;

            InfrastructureMetrics.LlmFailures.Add(
                1);

            InfrastructureMetrics.LlmDuration.Record(
                durationMs,
                new KeyValuePair<string, object?>(
                    "outcome",
                    "failure"));

            _logger.LogWarning(
                exception,
                "LLM alternative recommendation request failed. " +
                "CorrelationId: {CorrelationId}, " +
                "ProductId: {ProductId}, " +
                "DurationMs: {DurationMs}",
                request.CorrelationId,
                request.DepletedProduct.ProductId,
                durationMs);

            throw;
        }
    }
}