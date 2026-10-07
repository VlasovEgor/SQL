namespace StoreAnalytics.Dtos;

public record CreateOrderResult(bool IsSuccess, OrdersDto? Order = null, string? Error = null);
