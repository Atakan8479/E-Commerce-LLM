using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI.Models;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.AI;

internal sealed class SemanticKernelAlternativeRecommendationGenerator
    : IAlternativeRecommendationGenerator
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IChatCompletionService _chatCompletionService;

    public SemanticKernelAlternativeRecommendationGenerator(
        IChatCompletionService chatCompletionService)
    {
        _chatCompletionService = chatCompletionService;
    }

    public async Task<AlternativeRecommendationResult> GenerateAsync(
        AlternativeRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Candidates.Count == 0)
        {
            return new AlternativeRecommendationResult([]);
        }

        var prompt = AlternativeRecommendationPromptBuilder.Build(request);

        var executionSettings =
            new OpenAIPromptExecutionSettings
            {
                Temperature = 0,
                ResponseFormat =
                    typeof(
                        AlternativeRecommendationModelResponse)
            };

        var response = await _chatCompletionService.GetChatMessageContentAsync(
            prompt,
            executionSettings,
            kernel: null,
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(response.Content))
        {
            throw new AlternativeRecommendationValidationException(
                "The LLM returned an empty alternative recommendation response.");
        }

        AlternativeRecommendationModelResponse? modelResponse;

        try
        {
            modelResponse =
                JsonSerializer.Deserialize<AlternativeRecommendationModelResponse>(
                    response.Content,
                    JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new AlternativeRecommendationValidationException(
                "The LLM returned a malformed alternative recommendation response.",
                exception);
        }

        if (modelResponse?.Recommendations is null)
        {
            throw new AlternativeRecommendationValidationException(
                "The LLM response does not contain a recommendations collection.");
        }

        var recommendations = modelResponse.Recommendations
            .Select(MapRecommendation)
            .ToArray();

        var result = new AlternativeRecommendationResult(recommendations);

        AlternativeRecommendationResultValidator.Validate(request, result);

        return result;
    }

    private static AlternativeRecommendation MapRecommendation(
        AlternativeRecommendationModelRecommendation recommendation)
    {
        if (!Guid.TryParse(recommendation.ProductId, out var productId))
        {
            throw new AlternativeRecommendationValidationException(
                $"The LLM returned an invalid ProductId '{recommendation.ProductId}'.");
        }

        return new AlternativeRecommendation(
            productId,
            recommendation.Reason);
    }
}