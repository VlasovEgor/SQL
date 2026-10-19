using System.ComponentModel.DataAnnotations;

namespace StoreAnalytics;

public class CreateOrderRequest : IValidatableObject
{
    public Guid CustomerId { get; set; }
    public List<CreateOrderItemRequest> OrderItems { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CustomerId == Guid.Empty)
            yield return new ValidationResult("CustomerId can't be empty.", [nameof(CustomerId)]);

        if (OrderItems is null || OrderItems.Count == 0)
        {
            yield return new ValidationResult("Order must contain at least one item.", [nameof(OrderItems)]);
            yield break;
        }

        HashSet<Guid> productIds = [];
        for (int i = 0; i < OrderItems.Count; i++)
        {
            CreateOrderItemRequest? item = OrderItems[i];
            string member = $"{nameof(OrderItems)}[{i}]";
            if (item is null)
            {
                yield return new ValidationResult("Order item can't be null.", [member]);
                continue;
            }

            if (item.ProductId == Guid.Empty)
                yield return new ValidationResult("ProductId can't be empty.", [$"{member}.ProductId"]);
            if (item.Quantity <= 0)
                yield return new ValidationResult("Quantity must be greater than zero.", [$"{member}.Quantity"]);
            if (!productIds.Add(item.ProductId))
                yield return new ValidationResult("A product can only occur once in an order.", [$"{member}.ProductId"]);
        }
    }
}

public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
