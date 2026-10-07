using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StoreAnalytics.Models;

namespace StoreAnalytics.Configurations;

public class CustomersConfiguration : IEntityTypeConfiguration<Customers>
{
    public void Configure(EntityTypeBuilder<Customers> builder)
    {
        builder.HasKey(c => c.CustomerId);

        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(50);
        builder.Property(c => c.LastName).IsRequired().HasMaxLength(50);

        builder.Property(c => c.Email).IsRequired().HasMaxLength(50);

        builder.ToTable(table => table.HasCheckConstraint("CK_Customers_Balance_Min", "\"Balance\" >= 0"));
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
