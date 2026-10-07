using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using StoreAnalytics.Dtos;
using StoreAnalytics.Models;
using StoreAnalytics.Repositories;

namespace StoreAnalytics.Services;

public class AnalyticsService
{
    private readonly StoreRepository _storeRepository;
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AnalyticsService> _logger;

    private static readonly TimeSpan ReportCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ReportLockDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ReportLockWaitTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ReportLockPollDelay = TimeSpan.FromMilliseconds(200);

    public AnalyticsService(StoreRepository storeRepository, IDistributedCache cache,
        IConnectionMultiplexer redis, ILogger<AnalyticsService> logger)
    {
        _storeRepository = storeRepository;
        _cache = cache;
        _redis = redis;
        _logger = logger;
    }

    public async Task<List<Orders>> GetAllOrders(CancellationToken cancellationToken)
    {
        return await _storeRepository.GetAllOrdersAsync(cancellationToken);
    }

    public async Task<List<OrdersDto>> GetCustomerOrders(Guid customerId, CancellationToken cancellationToken)
    {
        //return await _storeRepository.GetCustomerOrdersNaively(customerId, cancellationToken);
        return await _storeRepository.GetCustomerOrders(customerId, cancellationToken);
    }

    public async Task<TopCustomerRevenue[]> GetTopCustomersRevenue(DateTimeOffset from, DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        string key = $"top-customers:{from.UtcDateTime:yyyyMMddHHmmss}:{to.UtcDateTime:yyyyMMddHHmmss}";
        string? cached = await _cache.GetStringAsync(key, cancellationToken);

        if (cached != null)
        {
            _logger.LogInformation("Retrieved top customers revenue from Redis.");
            return DeserializeTopCustomersRevenue(cached);
        }

        string lockKey = $"{key}:lock";
        string lockValue = Guid.NewGuid().ToString();
        IDatabase database = _redis.GetDatabase();
        bool lockAcquired = await database.LockTakeAsync(lockKey, lockValue, ReportLockDuration);

        if (!lockAcquired)
        {
            TopCustomerRevenue[]? lockedCustomers = await WaitForCachedTopCustomersRevenue(key, cancellationToken);

            if (lockedCustomers is not null)
            {
                return lockedCustomers;
            }

            _logger.LogWarning("Timed out waiting for top customers revenue cache lock. Recomputing report.");
        }

        try
        {
            cached = await _cache.GetStringAsync(key, cancellationToken);

            if (cached != null)
            {
                _logger.LogInformation("Retrieved top customers revenue from Redis after lock acquisition.");
                return DeserializeTopCustomersRevenue(cached);
            }

            return await RefreshTopCustomersRevenueCache(key, from, to, cancellationToken);
        }
        finally
        {
            if (lockAcquired)
            {
                await database.LockReleaseAsync(lockKey, lockValue);
            }
        }
    }

    private async Task<TopCustomerRevenue[]> RefreshTopCustomersRevenueCache(
        string key,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var customerRevenues = await _storeRepository.GetTopCustomersRevenue(from, to, cancellationToken);
        TopCustomerRevenue[] customers = customerRevenues.ToArray();

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(customers),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ReportCacheDuration
            },
            cancellationToken);

        _logger.LogInformation("Retrieved top customers revenue from PostgreSQL.");
        return customers;
    }

    private async Task<TopCustomerRevenue[]?> WaitForCachedTopCustomersRevenue(
        string key,
        CancellationToken cancellationToken)
    {
        DateTimeOffset waitUntil = DateTimeOffset.UtcNow.Add(ReportLockWaitTimeout);

        while (DateTimeOffset.UtcNow < waitUntil)
        {
            await Task.Delay(ReportLockPollDelay, cancellationToken);

            string? cached = await _cache.GetStringAsync(key, cancellationToken);

            if (cached != null)
            {
                _logger.LogInformation("Retrieved top customers revenue from Redis after waiting for refresh.");
                return DeserializeTopCustomersRevenue(cached);
            }
        }

        return null;
    }

    private TopCustomerRevenue[] DeserializeTopCustomersRevenue(string cached)
    {
        return JsonSerializer.Deserialize<TopCustomerRevenue[]>(cached)!;
    }

    public async Task<CreateOrderResult> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        return await _storeRepository.CreateOrder(request, cancellationToken);
    }
}
