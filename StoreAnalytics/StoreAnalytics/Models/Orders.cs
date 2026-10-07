namespace StoreAnalytics.Models;

public class Orders
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public DateTimeOffset OrderDate { get; init; }

    public List<OrderItems> OrderItems { get; set; } = [];

    public Customers Customer { get; init; }
}