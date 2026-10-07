namespace StoreAnalytics.Dtos;

public record TopCustomerRevenue(Guid CustomerId, string FirstName, string LastName, decimal Revenue);

