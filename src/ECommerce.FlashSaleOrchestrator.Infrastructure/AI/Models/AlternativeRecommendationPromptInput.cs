namespace ECommerce.FlashSaleOrchestrator.Infrastructure.AI.Models;

internal sealed record AlternativeRecommendationPromptInput(
    PromptProduct DepletedProduct,
    IReadOnlyList<PromptCandidate> Candidates);

internal sealed record PromptProduct(
    Guid ProductId,
    string Name,
    string Category);

internal sealed record PromptCandidate(
    Guid ProductId,
    string Name,
    string Category,
    int AvailableQuantity);