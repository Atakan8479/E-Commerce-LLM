using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.IntegrationTests.AI;

public sealed class SemanticKernelAlternativeRecommendationGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ShouldReturnRecommendation_WhenModelReturnsCandidateProduct()
    {
        var candidateProductId = Guid.NewGuid();

        var chatCompletionService = new FakeChatCompletionService(
            $$"""
            {
              "recommendations": [
                {
                  "productId": "{{candidateProductId}}",
                  "reason": "Suitable gaming mouse alternative."
                }
              ]
            }
            """);

        var generator =
            new SemanticKernelAlternativeRecommendationGenerator(
                chatCompletionService);

        var request = CreateRequest(candidateProductId);

        var result = await generator.GenerateAsync(request);

        var recommendation = Assert.Single(result.Recommendations);

        Assert.Equal(candidateProductId, recommendation.ProductId);
        Assert.Equal(
            "Suitable gaming mouse alternative.",
            recommendation.Reason);

        Assert.Equal(1, chatCompletionService.CallCount);
    }

    [Fact]
    public async Task GenerateAsync_ShouldRejectProductOutsideCandidateSet()
    {
        var candidateProductId = Guid.NewGuid();
        var hallucinatedProductId = Guid.NewGuid();

        var chatCompletionService = new FakeChatCompletionService(
            $$"""
            {
              "recommendations": [
                {
                  "productId": "{{hallucinatedProductId}}",
                  "reason": "Invented alternative."
                }
              ]
            }
            """);

        var generator =
            new SemanticKernelAlternativeRecommendationGenerator(
                chatCompletionService);

        var request = CreateRequest(candidateProductId);

        await Assert.ThrowsAsync<AlternativeRecommendationValidationException>(
            () => generator.GenerateAsync(request));
    }

    [Fact]
    public async Task GenerateAsync_ShouldRejectMalformedJson()
    {
        var candidateProductId = Guid.NewGuid();

        var chatCompletionService = new FakeChatCompletionService(
            """
            {
              "recommendations":
            """);

        var generator =
            new SemanticKernelAlternativeRecommendationGenerator(
                chatCompletionService);

        var request = CreateRequest(candidateProductId);

        await Assert.ThrowsAsync<AlternativeRecommendationValidationException>(
            () => generator.GenerateAsync(request));
    }

    [Fact]
    public async Task GenerateAsync_ShouldNotCallModel_WhenCandidateSetIsEmpty()
    {
        var chatCompletionService =
            new FakeChatCompletionService(
                """{"recommendations":[]}""");

        var generator =
            new SemanticKernelAlternativeRecommendationGenerator(
                chatCompletionService);

        var request = new AlternativeRecommendationRequest(
            $"correlation-{Guid.NewGuid():N}",
                    new DepletedProductContext(
                Guid.NewGuid(),
                "Logitech Gaming Mouse",
                "mouse"),
            []);

        var result = await generator.GenerateAsync(request);

        Assert.Empty(result.Recommendations);
        Assert.Equal(0, chatCompletionService.CallCount);
    }

    [Fact]
    public async Task GenerateAsync_ShouldPropagateCancellationToken()
    {
        var candidateProductId = Guid.NewGuid();

        var chatCompletionService = new FakeChatCompletionService(
            $$"""
            {
              "recommendations": [
                {
                  "productId": "{{candidateProductId}}",
                  "reason": "Suitable alternative."
                }
              ]
            }
            """);

        var generator =
            new SemanticKernelAlternativeRecommendationGenerator(
                chatCompletionService);

        var request = CreateRequest(candidateProductId);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken = cancellationTokenSource.Token;

        await generator.GenerateAsync(
            request,
            cancellationToken);

        Assert.Equal(
            cancellationToken,
            chatCompletionService.ReceivedCancellationToken);
    }

    private static AlternativeRecommendationRequest CreateRequest(
        Guid candidateProductId)
    {
        return new AlternativeRecommendationRequest(
            $"correlation-{Guid.NewGuid():N}",
                    new DepletedProductContext(
                Guid.NewGuid(),
                "Logitech Gaming Mouse",
                "mouse"),
            [
                new AlternativeCandidate(
                    candidateProductId,
                    "Razer Viper",
                    "mouse",
                    20)
            ]);
    }
}