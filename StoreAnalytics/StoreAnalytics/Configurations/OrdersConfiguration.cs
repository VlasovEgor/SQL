using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreAnalytics.Models;

namespace StoreAnalytics.Configurations;

public class OrdersConfiguration : IEntityTypeConfiguration<Orders>
{
    public void Configure(EntityTypeBuilder<Orders> builder)
    {
        builder.HasKey(o => o.OrderId);

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.OrderDate).IsRequired();
        builder.HasIndex(o => o.OrderDate)
            .HasDatabaseName("IX_Orders_OrderDate_Report")
            .IncludeProperties(o => new { o.OrderId, o.CustomerId });

        builder.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId);
    }
}
