using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Configurations;

public sealed class InboxMessageConfiguration
    : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(
        EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable(
            "InboxMessages");

        builder.HasKey(
            inboxMessage => inboxMessage.Id);

        builder.Property(
                inboxMessage => inboxMessage.Id)
            .ValueGeneratedNever();

        builder.Property(
                inboxMessage => inboxMessage.OccurredAtUtc)
            .IsRequired();

        builder.Property(
                inboxMessage => inboxMessage.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(
            inboxMessage => inboxMessage.ProcessedAtUtc);

        builder.HasIndex(
            inboxMessage => inboxMessage.ProcessedAtUtc);
    }
}