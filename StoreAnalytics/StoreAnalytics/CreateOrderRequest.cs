namespace StoreAnalytics;

public class CreateOrderRequest
{
    public Guid CustomerId { get; set; }
    public List<CreateOrderItemRequest> OrderItems { get; set; } = [];
}

public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
