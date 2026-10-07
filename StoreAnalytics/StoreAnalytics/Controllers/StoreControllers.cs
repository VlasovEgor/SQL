using Microsoft.AspNetCore.Mvc;
using StoreAnalytics.Dtos;
using StoreAnalytics.Models;
using StoreAnalytics.Services;

namespace StoreAnalytics.Controllers;

[ApiController]
[Route("api/analytics")]
public class StoreControllers : ControllerBase
{
    private readonly AnalyticsService _analyticsService;

    public StoreControllers(AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        if (customerId.HasValue)
        {
            List<OrdersDto> orders = await _analyticsService.GetCustomerOrders(customerId.Value, cancellationToken);
            return Ok(orders);
        }

        List<Orders> allOrders = await _analyticsService.GetAllOrders(cancellationToken);
        return Ok(allOrders);
    }

    [HttpGet("top_customers_revenue")]
    public async Task<IActionResult> GetTopCustomersRevenue(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        return Ok(await _analyticsService.GetTopCustomersRevenue(from, to, cancellationToken));
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        CreateOrderResult result = await _analyticsService.CreateOrder(request, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Order);
        }

        return BadRequest(new { error = result.Error });
    }
}
