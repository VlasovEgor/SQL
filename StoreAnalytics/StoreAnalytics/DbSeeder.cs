using Microsoft.EntityFrameworkCore;
using StoreAnalytics.Models;

namespace StoreAnalytics;

public class DbSeeder
{
    public static async Task SeedAsync(StoreDbContext dbContext)
    {
        if (await dbContext.Customers.AnyAsync())
            return;

        Random random = new Random();

        List<Customers> customers = Enumerable.Range(1, 1000)
            .Select(i => new Customers
            {
                CustomerId = Guid.NewGuid(),
                FirstName = $"Name_{i}",
                LastName = $"LastName_{i}",
                Email = $"user{i}@mail.com",
                Balance = random.Next(0, 100001)
            })
            .ToList();

        string[] categories = ["Electronics",
            "Books",
            "Clothes",
            "Home",
            "Sport",
            "Food",
            "Beauty",
            "Auto",
            "Toys",
            "Other"];

        List<Products> products = Enumerable.Range(1, 200)
            .Select(i => new Products
            {
                ProductId = Guid.NewGuid(),
                Name = $"Product_{i}",
                Category = categories[(i - 1) % categories.Length],
                Quantity = random.Next(1, 1001),
                Price = random.Next(100, 20001)
            })
            .ToList();
        DateTimeOffset startDate = new DateTimeOffset(2020, 1, 1,
            0, 0, 0, TimeSpan.Zero);

        DateTimeOffset endDate = new DateTimeOffset(2026, 9, 23,
            0, 0, 0, TimeSpan.Zero);

        long totalSeconds = (long)(endDate - startDate).TotalSeconds;

        List<Orders> orders = new List<Orders>(10000);

        for (var i = 0; i < 10000; i++)
        {
            Customers customer = customers[random.Next(customers.Count)];

            Orders order = new Orders
            {
                OrderId = Guid.NewGuid(),
                CustomerId = customer.CustomerId,
                OrderDate = startDate.AddSeconds(
                    random.NextInt64(totalSeconds))
            };

            orders.Add(order);
        }

        List<OrderItems> orderItems = new List<OrderItems>(30000);

        foreach (Orders order in orders)
        {
            HashSet<int> usedProductIndexes = new HashSet<int>();

            while (usedProductIndexes.Count < 3)
            {
                usedProductIndexes.Add(random.Next(products.Count));
            }

            foreach (int productIndex in usedProductIndexes)
            {
                Products product = products[productIndex];

                OrderItems orderItem = new OrderItems
                {
                    OrderId = order.OrderId,
                    ProductId = product.ProductId,
                    Quantity = random.Next(1, 10),
                    Price = product.Price
                };

                orderItems.Add(orderItem);
            }
        }

        await dbContext.Customers.AddRangeAsync(customers);
        await dbContext.Products.AddRangeAsync(products);
        await dbContext.Orders.AddRangeAsync(orders);
        await dbContext.OrderItems.AddRangeAsync(orderItems);

        await dbContext.SaveChangesAsync();
    }
}
