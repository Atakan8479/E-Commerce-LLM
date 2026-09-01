using System.Text.Json.Serialization;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.AI.Models;

internal sealed record AlternativeRecommendationModelResponse(
    [property: JsonPropertyName("recommendations")]
    AlternativeRecommendationModelRecommendation[] Recommendations);

internal sealed record AlternativeRecommendationModelRecommendation(
    [property: JsonPropertyName("productId")]
    string ProductId,

    [property: JsonPropertyName("reason")]
    string Reason);