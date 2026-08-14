using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(
        EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(
            outboxMessage => outboxMessage.Id);

        builder.Property(
                outboxMessage => outboxMessage.Id)
            .ValueGeneratedNever();

        builder.Property(
                outboxMessage => outboxMessage.OccurredAtUtc)
            .IsRequired();

        builder.Property(
                outboxMessage => outboxMessage.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(
                outboxMessage => outboxMessage.Payload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(
            outboxMessage => outboxMessage.ProcessedAtUtc);

        builder.HasIndex(
            outboxMessage => outboxMessage.ProcessedAtUtc);
    }
}