namespace StoreAnalytics.Dtos;

public record CreateOrderResult(bool IsSuccess, OrdersDto? Order = null, string? Error = null,
    CreateOrderError ErrorCode = CreateOrderError.None);

public enum CreateOrderError
{
    None,
    InvalidRequest,
    CustomerNotFound,
    ProductNotFound,
    InsufficientStock
}
