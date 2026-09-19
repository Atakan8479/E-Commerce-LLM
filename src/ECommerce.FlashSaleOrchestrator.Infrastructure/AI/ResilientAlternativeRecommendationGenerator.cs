using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Infrastructure
    .AI.SemanticCaching;
using Microsoft.Extensions.Logging;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.AI;

internal sealed class
    ResilientAlternativeRecommendationGenerator
    : IAlternativeRecommendationGenerator,
      IAlternativeRecommendationExecutor
{
    private const int MaxAttempts =
        2;

    private static readonly TimeSpan RetryDelay =
        TimeSpan.FromMilliseconds(
            250);

    private readonly
        CachedSemanticAlternativeRecommendationGenerator
        _primaryGenerator;

    private readonly
        DeterministicAlternativeRecommendationGenerator
        _fallbackGenerator;

    private readonly ILogger<
        ResilientAlternativeRecommendationGenerator>
        _logger;

    public ResilientAlternativeRecommendationGenerator(
        CachedSemanticAlternativeRecommendationGenerator
            primaryGenerator,
        DeterministicAlternativeRecommendationGenerator
            fallbackGenerator,
        ILogger<
            ResilientAlternativeRecommendationGenerator>
            logger)
    {
        ArgumentNullException.ThrowIfNull(
            primaryGenerator);

        ArgumentNullException.ThrowIfNull(
            fallbackGenerator);

        ArgumentNullException.ThrowIfNull(
            logger);

        _primaryGenerator =
            primaryGenerator;

        _fallbackGenerator =
            fallbackGenerator;

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

        if (request.Candidates.Count == 0)
        {
            return await GenerateFallbackOutcomeAsync(
                request,
                cancellationToken);
        }

        for (var attempt = 1;
             attempt <= MaxAttempts;
             attempt++)
        {
            try
            {
                return await _primaryGenerator.ExecuteAsync(
                    request,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (
                    cancellationToken
                        .IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
                when (attempt < MaxAttempts)
            {
                _logger.LogWarning(
                    exception,
                    "Alternative recommendation primary pipeline failed. " +
                    "Retry scheduled. " +
                    "CorrelationId: {CorrelationId}, " +
                    "ProductId: {ProductId}, " +
                    "RetryCount: {RetryCount}, " +
                    "MaxAttempts: {MaxAttempts}",
                    request.CorrelationId,
                    request.DepletedProduct.ProductId,
                    attempt,
                    MaxAttempts);

                await Task.Delay(
                    RetryDelay,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Alternative recommendation primary pipeline exhausted " +
                    "all attempts. Deterministic fallback will be used. " +
                    "CorrelationId: {CorrelationId}, " +
                    "ProductId: {ProductId}, " +
                    "RetryCount: {RetryCount}, " +
                    "Attempts: {Attempts}",
                    request.CorrelationId,
                    request.DepletedProduct.ProductId,
                    MaxAttempts - 1,
                    MaxAttempts);
            }
        }

        return await GenerateFallbackOutcomeAsync(
            request,
            cancellationToken);
    }

    private async Task<AlternativeRecommendationGenerationOutcome>
        GenerateFallbackOutcomeAsync(
            AlternativeRecommendationRequest request,
            CancellationToken cancellationToken)
    {
        var result =
            await _fallbackGenerator.GenerateAsync(
                request,
                cancellationToken);

        return new AlternativeRecommendationGenerationOutcome(
            result,
            AlternativeRecommendationSource.Deterministic);
    }
}