using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreAnalytics.Models;

namespace StoreAnalytics.Configurations;

public class ProductsConfiguration : IEntityTypeConfiguration<Products>
{
    public void Configure(EntityTypeBuilder<Products> builder)
    {
        builder.HasKey(p => p.ProductId);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Category).IsRequired().HasMaxLength(50);

        builder.Property(p => p.Quantity).IsRequired();
        builder.Property(p => p.Price).IsRequired();

        builder.ToTable(table => table.HasCheckConstraint("CK_Products_Quantity_Min", "\"Quantity\" >= 0"));
        builder.ToTable(table => table.HasCheckConstraint("CK_Products_Price_Min", "\"Price\" >= 0"));
    }
}
