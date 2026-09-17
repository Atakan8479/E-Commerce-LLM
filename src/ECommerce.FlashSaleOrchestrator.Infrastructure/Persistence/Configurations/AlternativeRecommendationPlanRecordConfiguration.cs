using ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.AlternativeRecommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure
    .Persistence.Configurations;

public sealed class AlternativeRecommendationPlanRecordConfiguration
    : IEntityTypeConfiguration<
        AlternativeRecommendationPlanRecord>
{
    public void Configure(
        EntityTypeBuilder<
            AlternativeRecommendationPlanRecord> builder)
    {
        builder.ToTable(
            "AlternativeRecommendationPlans");

        builder.HasKey(
            plan =>
                plan.EventId);

        builder.Property(
                plan =>
                    plan.EventId)
            .ValueGeneratedNever();

        builder.Property(
                plan =>
                    plan.OriginalProductId)
            .IsRequired();

        builder.Property(
                plan =>
                    plan.PayloadJson)
            .HasColumnType(
                "nvarchar(max)")
            .IsRequired();

        builder.Property(
                plan =>
                    plan.Source)
            .HasConversion<string>()
            .HasMaxLength(
                32)
            .IsRequired();

        builder.Property(
                plan =>
                    plan.CorrelationId)
            .HasMaxLength(
                128)
            .IsRequired();

        builder.Property(
                plan =>
                    plan.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(
            plan =>
                plan.OriginalProductId);

        builder.HasIndex(
            plan =>
                plan.CorrelationId);
    }
}