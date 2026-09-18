using System.ComponentModel.DataAnnotations;

namespace ECommerce.FlashSaleOrchestrator.Api
    .Contracts.RecommendationPlans;

public sealed record GetRecommendationPlansRequest
{
    [Required(
        ErrorMessage =
            "CorrelationId is required.")]
    [StringLength(
        128,
        ErrorMessage =
            "CorrelationId must not exceed 128 characters.")]
    public string CorrelationId
    {
        get;
        init;
    } = string.Empty;
}