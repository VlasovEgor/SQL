using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using StoreAnalytics;
using StoreAnalytics.Repositories;
using StoreAnalytics.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    string redisConnectionString = builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("Redis connection string is not configured.");

    ConfigurationOptions options = ConfigurationOptions.Parse(redisConnectionString);
    options.AbortOnConnectFail = false;

    return ConnectionMultiplexer.Connect(options);
});

builder.Services.AddDbContext<StoreDbContext>();

builder.Services.AddScoped<StoreRepository>();
builder.Services.AddScoped<AnalyticsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (IServiceScope scope = app.Services.CreateScope())
{
    StoreDbContext dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

    await DbSeeder.SeedAsync(dbContext);
}

app.Run();
