namespace StoreAnalytics.Models;

public class Customers
{
    public Guid CustomerId { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string Email { get; init; }

    public List<Orders> Orders { get; set; } = [];

    public decimal Balance
    {
        get => _balance;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be less than 0");
            }

            _balance = value;
        }
    }

    private decimal _balance;
}
