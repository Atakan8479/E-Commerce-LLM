using ECommerce.FlashSaleOrchestrator.Domain.Products;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration
    : IEntityTypeConfiguration<Product>
{
    public void Configure(
        EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(
            product => product.Id);

        builder.Property(
                product => product.Id)
            .HasConversion(
                new ProductIdValueConverter())
            .ValueGeneratedNever();

        builder.Property(
                product => product.Name)
            .HasConversion(
                new ProductNameValueConverter())
            .HasMaxLength(ProductName.MaxLength)
            .IsRequired();
    }
}