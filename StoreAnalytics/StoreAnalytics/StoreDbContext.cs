using Microsoft.EntityFrameworkCore;
using StoreAnalytics.Configurations;
using StoreAnalytics.Models;

namespace StoreAnalytics;

public class StoreDbContext : DbContext
{
    public DbSet<Customers> Customers { get; set; }
    public DbSet<Orders> Orders { get; set; }
    public DbSet<Products> Products { get; set; }
    public DbSet<OrderItems> OrderItems { get; set; }

    private readonly IConfiguration _configuration;

    public StoreDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_configuration.GetConnectionString(nameof(StoreDbContext)))
            .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new CustomersConfiguration());
        builder.ApplyConfiguration(new OrdersConfiguration());
        builder.ApplyConfiguration(new ProductsConfiguration());
        builder.ApplyConfiguration(new OrderItemConfiguration());

        base.OnModelCreating(builder);
    }
}
