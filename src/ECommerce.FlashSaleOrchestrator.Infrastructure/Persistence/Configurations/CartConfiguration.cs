using ECommerce.FlashSaleOrchestrator.Domain.Carts;
using ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.FlashSaleOrchestrator.Infrastructure.Persistence.Configurations;

internal sealed class CartConfiguration
    : IEntityTypeConfiguration<Cart>
{
    public void Configure(
        EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(
            cart => cart.Id);

        builder.Property(
                cart => cart.Id)
            .HasConversion(
                new CartIdValueConverter())
            .ValueGeneratedNever();

        builder.Ignore(
            cart => cart.IsEmpty);

        builder.OwnsMany(
            cart => cart.Items,
            itemBuilder =>
            {
                itemBuilder.ToTable(
                    "CartItems");

                itemBuilder.WithOwner()
                    .HasForeignKey(
                        "CartId");

                itemBuilder.Property<CartId>(
                        "CartId")
                    .HasConversion(
                        new CartIdValueConverter());

                itemBuilder.Property(
                        item => item.ProductId)
                    .HasConversion(
                        new ProductIdValueConverter())
                    .ValueGeneratedNever();

                itemBuilder.Property(
                        item => item.Quantity)
                    .IsRequired();

                itemBuilder.HasKey(
                    "CartId",
                    nameof(CartItem.ProductId));
            });

        builder.Navigation(
                cart => cart.Items)
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}