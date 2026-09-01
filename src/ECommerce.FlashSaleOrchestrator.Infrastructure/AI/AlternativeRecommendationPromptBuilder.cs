using System.Text.Json;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Infrastructure.AI.Models;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.AI;

internal static class AlternativeRecommendationPromptBuilder
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

    public static string Build(AlternativeRecommendationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var input = new AlternativeRecommendationPromptInput(
            new PromptProduct(
                request.DepletedProduct.ProductId,
                request.DepletedProduct.Name,
                request.DepletedProduct.Category),
            request.Candidates
                .Select(candidate => new PromptCandidate(
                    candidate.ProductId,
                    candidate.Name,
                    candidate.Category,
                    candidate.AvailableQuantity))
                .ToArray());

        var groundedInput = JsonSerializer.Serialize(input, JsonOptions);

        return $$"""
            You generate alternative product recommendations when a requested product is out of stock.

            Follow these rules strictly:
            - Select products only from the supplied candidates.
            - Never invent a product.
            - Never modify ProductId values.
            - Never return a ProductId that is not present in candidates.
            - Use the supplied product facts only.
            - Do not assume stock, category, or product information that is not provided.
            - It is valid to return no recommendations if none of the candidates is a meaningful alternative.
            - For every selected product, provide a short reason explaining why it is a suitable alternative.
            - Return structured output only.

            Grounded product data:

            {{groundedInput}}
            """;
    }
}