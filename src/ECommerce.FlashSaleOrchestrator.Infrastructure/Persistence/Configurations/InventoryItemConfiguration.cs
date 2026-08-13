using ECommerce.FlashSaleOrchestrator.Domain.Inventory;
using ECommerce.FlashSaleOrchestrator.Domain.Products;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Configurations;

internal sealed class InventoryItemConfiguration
    : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(
        EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");

        builder.HasKey(
            inventoryItem =>
                inventoryItem.ProductId);

        builder.Property(
                inventoryItem =>
                    inventoryItem.ProductId)
            .HasConversion(
                new ProductIdValueConverter())
            .ValueGeneratedNever();

        builder.Property(
                inventoryItem =>
                    inventoryItem.AvailableQuantity)
            .HasConversion(
                new StockQuantityValueConverter())
            .HasColumnName("AvailableQuantity")
            .IsRequired();

        builder.Ignore(
            inventoryItem =>
                inventoryItem.IsDepleted);

        builder.Ignore(
            inventoryItem =>
                inventoryItem.DomainEvents);

        builder.Property<byte[]>(
                "RowVersion")
            .IsRowVersion()
            .IsRequired();

        builder.HasOne<Product>()
            .WithOne()
            .HasForeignKey<InventoryItem>(
                inventoryItem =>
                    inventoryItem.ProductId)
            .IsRequired()
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}