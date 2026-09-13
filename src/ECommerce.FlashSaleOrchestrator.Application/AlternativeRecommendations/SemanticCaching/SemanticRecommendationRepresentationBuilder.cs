using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeCandidates;
using ECommerce.FlashSaleOrchestrator.Application.AlternativeRecommendations;

namespace ECommerce.FlashSaleOrchestrator.Application
    .AlternativeRecommendations.SemanticCaching;

public sealed class SemanticRecommendationRepresentationBuilder
{
    public SemanticRecommendationRepresentation Build(
        AlternativeRecommendationRequest request)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        ArgumentNullException.ThrowIfNull(
            request.DepletedProduct);

        ArgumentNullException.ThrowIfNull(
            request.Candidates);

        var orderedCandidates =
            request.Candidates
                .OrderBy(
                    candidate =>
                        candidate.ProductId)
                .ToArray();

        var semanticText =
            BuildSemanticText(
                request.DepletedProduct,
                orderedCandidates);

        var candidateFingerprint =
            BuildCandidateFingerprint(
                orderedCandidates);

        return new SemanticRecommendationRepresentation(
            semanticText,
            candidateFingerprint);
    }

    private static string BuildSemanticText(
        DepletedProductContext depletedProduct,
        IReadOnlyList<AlternativeCandidate> candidates)
    {
        var builder =
            new StringBuilder();

        builder.AppendLine(
            "depleted_product:");

        builder.Append(
            "id=");

        builder.AppendLine(
            depletedProduct.ProductId.ToString(
                "D"));

        builder.Append(
            "name=");

        builder.AppendLine(
            NormalizeText(
                depletedProduct.Name));

        builder.Append(
            "category=");

        builder.AppendLine(
            NormalizeText(
                depletedProduct.Category));

        builder.AppendLine(
            "candidates:");

        foreach (var candidate in candidates)
        {
            builder.Append(
                "id=");

            builder.Append(
                candidate.ProductId.ToString(
                    "D"));

            builder.Append(
                ";name=");

            builder.Append(
                NormalizeText(
                    candidate.Name));

            builder.Append(
                ";category=");

            builder.Append(
                NormalizeText(
                    candidate.Category));

            builder.Append(
                ";available_quantity=");

            builder.AppendLine(
                candidate.AvailableQuantity.ToString(
                    CultureInfo.InvariantCulture));
        }

        return builder
            .ToString()
            .TrimEnd();
    }

    private static string BuildCandidateFingerprint(
        IReadOnlyList<AlternativeCandidate> candidates)
    {
        var canonicalBuilder =
            new StringBuilder();

        AppendFingerprintField(
            canonicalBuilder,
            candidates.Count.ToString(
                CultureInfo.InvariantCulture));

        foreach (var candidate in candidates)
        {
            AppendFingerprintField(
                canonicalBuilder,
                candidate.ProductId.ToString(
                    "N"));

            AppendFingerprintField(
                canonicalBuilder,
                NormalizeText(
                    candidate.Name));

            AppendFingerprintField(
                canonicalBuilder,
                NormalizeText(
                    candidate.Category));

            AppendFingerprintField(
                canonicalBuilder,
                candidate.AvailableQuantity.ToString(
                    CultureInfo.InvariantCulture));
        }

        var bytes =
            Encoding.UTF8.GetBytes(
                canonicalBuilder.ToString());

        var hash =
            SHA256.HashData(
                bytes);

        return Convert
            .ToHexString(
                hash)
            .ToLowerInvariant();
    }

    private static void AppendFingerprintField(
        StringBuilder builder,
        string value)
    {
        builder.Append(
            value.Length.ToString(
                CultureInfo.InvariantCulture));

        builder.Append(
            ':');

        builder.Append(
            value);

        builder.Append(
            '|');
    }

    private static string NormalizeText(
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value);

        return value
            .Trim()
            .ToLowerInvariant();
    }
}