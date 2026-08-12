using ECommerce.FlashSaleOrchestrator.Domain.FlashSales;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Configurations;

internal sealed class FlashSaleConfiguration
    : IEntityTypeConfiguration<FlashSale>
{
    public void Configure(
        EntityTypeBuilder<FlashSale> builder)
    {
        builder.ToTable(
            "FlashSales");

        builder.HasKey(
            flashSale =>
                flashSale.Id);

        builder.Property(
                flashSale =>
                    flashSale.Id)
            .HasConversion(
                new FlashSaleIdValueConverter())
            .ValueGeneratedNever();

        builder.Property(
                flashSale =>
                    flashSale.StartsAtUtc)
            .IsRequired();

        builder.Property(
                flashSale =>
                    flashSale.EndsAtUtc)
            .IsRequired();

        builder.Ignore(
            flashSale =>
                flashSale.EligibleProductIds);

        builder.OwnsMany(
            flashSale =>
                flashSale.EligibleProducts,
            eligibleBuilder =>
            {
                eligibleBuilder.ToTable(
                    "FlashSaleEligibleProducts");

                eligibleBuilder.WithOwner()
                    .HasForeignKey(
                        "FlashSaleId");

                eligibleBuilder.Property<FlashSaleId>(
                        "FlashSaleId")
                    .HasConversion(
                        new FlashSaleIdValueConverter());

                eligibleBuilder.Property(
                        eligible =>
                            eligible.ProductId)
                    .HasConversion(
                        new ProductIdValueConverter())
                    .ValueGeneratedNever();

                eligibleBuilder.HasKey(
                    "FlashSaleId",
                    nameof(
                        FlashSaleEligibleProduct.ProductId));
            });

        builder.Navigation(
                flashSale =>
                    flashSale.EligibleProducts)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}