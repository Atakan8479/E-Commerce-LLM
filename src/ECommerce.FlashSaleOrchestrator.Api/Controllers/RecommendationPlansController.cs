using ECommerce.FlashSaleOrchestrator.Api
    .Contracts.RecommendationPlans;
using ECommerce.FlashSaleOrchestrator.Application
    .Abstractions.Messaging;
using ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.GetRecommendationPlans;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.FlashSaleOrchestrator.Api
    .Controllers;

[ApiController]
[Route("api/recommendation-plans")]
public sealed class RecommendationPlansController
    : ControllerBase
{
    private readonly IQueryHandler<
        GetRecommendationPlansByCorrelationIdQuery,
        IReadOnlyList<RecommendationPlanResult>>
        _getRecommendationPlansHandler;

    public RecommendationPlansController(
        IQueryHandler<
            GetRecommendationPlansByCorrelationIdQuery,
            IReadOnlyList<RecommendationPlanResult>>
            getRecommendationPlansHandler)
    {
        _getRecommendationPlansHandler =
            getRecommendationPlansHandler
            ?? throw new ArgumentNullException(
                nameof(getRecommendationPlansHandler));
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(RecommendationPlansResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<
        ActionResult<RecommendationPlansResponse>>
        GetByCorrelationIdAsync(
            [FromQuery]
            GetRecommendationPlansRequest request,
            CancellationToken cancellationToken)
    {
        var correlationId =
            request.CorrelationId.Trim();

        var results =
            await _getRecommendationPlansHandler
                .HandleAsync(
                    new GetRecommendationPlansByCorrelationIdQuery(
                        correlationId),
                    cancellationToken);

        var plans =
            results
                .Select(
                    plan =>
                        new RecommendationPlanResponse(
                            plan.EventId,
                            plan.OriginalProductId,
                            plan.Recommendations
                                .Select(
                                    recommendation =>
                                        new RecommendationPlanItemResponse(
                                            recommendation.ProductId,
                                            recommendation.Reason))
                                .ToArray(),
                            plan.Source.ToString(),
                            plan.CreatedAtUtc))
                .ToArray();

        return Ok(
            new RecommendationPlansResponse(
                correlationId,
                plans));
    }
}