namespace StoreAnalytics.Models;

public class OrderItems
{
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public Orders Order { get; init; }
    public Products Product { get; init; }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be less than or equal to 0");
            }

            _quantity = value;
        }
    }

    public decimal Price
    {
        get => _price;
        init
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be less than 0");
            }

            _price = value;
        }
    }

    private int _quantity;
    private readonly decimal _price;
}
