using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations.SemanticCaching;

namespace ECommerce.FlashSaleOrchestrator.Application.Tests
    .AlternativeRecommendations.SemanticCaching;

public sealed class SemanticRecommendationRepresentationBuilderTests
{
    [Fact]
    public void Build_ShouldBeStable_WhenCandidateOrderChanges()
    {
        var firstCandidate =
            new AlternativeCandidate(
                Guid.Parse(
                    "11111111-1111-1111-1111-111111111111"),
                "Candidate A",
                "Gaming Mouse",
                10);

        var secondCandidate =
            new AlternativeCandidate(
                Guid.Parse(
                    "22222222-2222-2222-2222-222222222222"),
                "Candidate B",
                "Gaming Mouse",
                5);

        var firstRequest =
            CreateRequest(
                "correlation-a",
                [
                    firstCandidate,
                    secondCandidate
                ]);

        var secondRequest =
            CreateRequest(
                "correlation-a",
                [
                    secondCandidate,
                    firstCandidate
                ]);

        var builder =
            new SemanticRecommendationRepresentationBuilder();

        var first =
            builder.Build(
                firstRequest);

        var second =
            builder.Build(
                secondRequest);

        Assert.Equal(
            first.Text,
            second.Text);

        Assert.Equal(
            first.CandidateFingerprint,
            second.CandidateFingerprint);
    }

    [Fact]
    public void Build_ShouldIgnoreCorrelationId()
    {
        var candidate =
            CreateCandidate(
                10);

        var firstRequest =
            CreateRequest(
                "correlation-a",
                [candidate]);

        var secondRequest =
            CreateRequest(
                "correlation-b",
                [candidate]);

        var builder =
            new SemanticRecommendationRepresentationBuilder();

        var first =
            builder.Build(
                firstRequest);

        var second =
            builder.Build(
                secondRequest);

        Assert.Equal(
            first.Text,
            second.Text);

        Assert.Equal(
            first.CandidateFingerprint,
            second.CandidateFingerprint);

        Assert.DoesNotContain(
            "correlation-a",
            first.Text,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_ShouldChangeFingerprint_WhenAvailabilityChanges()
    {
        var firstRequest =
            CreateRequest(
                "correlation",
                [
                    CreateCandidate(
                        10)
                ]);

        var secondRequest =
            CreateRequest(
                "correlation",
                [
                    CreateCandidate(
                        9)
                ]);

        var builder =
            new SemanticRecommendationRepresentationBuilder();

        var first =
            builder.Build(
                firstRequest);

        var second =
            builder.Build(
                secondRequest);

        Assert.NotEqual(
            first.CandidateFingerprint,
            second.CandidateFingerprint);
    }

    [Fact]
    public void Build_ShouldChangeFingerprint_WhenCandidateChanges()
    {
        var firstCandidate =
            new AlternativeCandidate(
                Guid.Parse(
                    "11111111-1111-1111-1111-111111111111"),
                "Candidate A",
                "Gaming Mouse",
                10);

        var secondCandidate =
            new AlternativeCandidate(
                Guid.Parse(
                    "22222222-2222-2222-2222-222222222222"),
                "Candidate B",
                "Gaming Mouse",
                10);

        var builder =
            new SemanticRecommendationRepresentationBuilder();

        var first =
            builder.Build(
                CreateRequest(
                    "correlation",
                    [firstCandidate]));

        var second =
            builder.Build(
                CreateRequest(
                    "correlation",
                    [secondCandidate]));

        Assert.NotEqual(
            first.CandidateFingerprint,
            second.CandidateFingerprint);
    }

    [Fact]
    public void Build_ShouldChangeSemanticTextButNotCandidateFingerprint_WhenDepletedProductChanges()
    {
        var candidate =
            CreateCandidate(
                10);

        var firstRequest =
            new AlternativeRecommendationRequest(
                "correlation-a",
                new DepletedProductContext(
                    Guid.Parse(
                        "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "Wireless Gaming Mouse",
                    "Gaming Mouse"),
                [candidate]);

        var secondRequest =
            new AlternativeRecommendationRequest(
                "correlation-b",
                new DepletedProductContext(
                    Guid.Parse(
                        "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "Ergonomic Gaming Mouse",
                    "Gaming Mouse"),
                [candidate]);

        var builder =
            new SemanticRecommendationRepresentationBuilder();

        var first =
            builder.Build(
                firstRequest);

        var second =
            builder.Build(
                secondRequest);

        Assert.NotEqual(
            first.Text,
            second.Text);

        Assert.Equal(
            first.CandidateFingerprint,
            second.CandidateFingerprint);
    }

    private static AlternativeRecommendationRequest CreateRequest(
        string correlationId,
        IReadOnlyList<AlternativeCandidate> candidates)
    {
        return new AlternativeRecommendationRequest(
            correlationId,
            new DepletedProductContext(
                Guid.Parse(
                    "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "Depleted Gaming Mouse",
                "Gaming Mouse"),
            candidates);
    }

    private static AlternativeCandidate CreateCandidate(
        int availableQuantity)
    {
        return new AlternativeCandidate(
            Guid.Parse(
                "11111111-1111-1111-1111-111111111111"),
            "Candidate Gaming Mouse",
            "Gaming Mouse",
            availableQuantity);
    }
}