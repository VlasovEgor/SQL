namespace StoreAnalytics.Models;

public class Products
{
    public Guid ProductId { get; init; }
    public string Name { get; set; }
    public string Category { get; set; }

    public List<OrderItems> OrderItems { get; set; }

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
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be less than or equal to 0");
            }

            _price = value;
        }
    }

    private int _quantity;
    private decimal _price;
}
