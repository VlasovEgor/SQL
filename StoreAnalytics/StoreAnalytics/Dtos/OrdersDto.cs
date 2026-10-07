namespace StoreAnalytics.Dtos;

public record OrdersDto(Guid OrdersId, Guid CustomerId, DateTimeOffset OrderDate, params OrderItemDto[] OrderItems);
