namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

public sealed record SemanticRecommendationRepresentation(
    string Text,
    string CandidateFingerprint);