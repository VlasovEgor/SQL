using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreAnalytics.Models;

namespace StoreAnalytics.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItems>
{
    public void Configure(EntityTypeBuilder<OrderItems> builder)
    {
        builder.HasKey(oi => new { oi.OrderId, oi.ProductId });

        builder.Property(oi => oi.ProductId).IsRequired();

        builder.Property(oi => oi.Quantity).IsRequired();
        builder.Property(oi => oi.Price).IsRequired();
        builder.HasIndex(oi => oi.OrderId)
            .HasDatabaseName("IX_OrderItems_OrderId_Report")
            .IncludeProperties(oi => new { oi.Quantity, oi.Price });

        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId);

        builder.HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId);

        builder.ToTable(table => table.HasCheckConstraint("CK_Order_Items_Quantity_Min", "\"Quantity\" >= 0"));
        builder.ToTable(table => table.HasCheckConstraint("CK_Order_Items_Price_Min", "\"Price\">= 0"));
    }
}
