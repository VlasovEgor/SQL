using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using StoreAnalytics.Dtos;
using StoreAnalytics.Models;

namespace StoreAnalytics.Repositories;

public class StoreRepository
{
    private readonly StoreDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StoreRepository> _logger;

    private const string TopCustomersRevenueSql = """
                                                  SELECT c."CustomerId",c."FirstName",c."LastName", SUM(oi."Quantity" * oi."Price") AS "Revenue"
                                                  FROM "Customers" c
                                                  JOIN "Orders" o USING ("CustomerId")
                                                  JOIN "OrderItems" oi USING ("OrderId")
                                                  WHERE o."OrderDate" BETWEEN @from AND @to
                                                  GROUP BY c."CustomerId", c."FirstName", c."LastName"
                                                  ORDER BY "Revenue" DESC
                                                  """;

    private const string ExplainTopCustomersRevenueSql = $"""
                                                         EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                                                         {TopCustomersRevenueSql}
                                                         """;

    public StoreRepository(StoreDbContext dbContext, IConfiguration configuration, ILogger<StoreRepository> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<Orders>> GetAllOrdersAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Orders.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<List<OrdersDto>> GetCustomerOrdersNaively(Guid customerId, CancellationToken cancellationToken)
    {
        List<OrdersDto> ordersDtos = new List<OrdersDto>();

        List<Orders> ordersList = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .ToListAsync(cancellationToken);

        foreach (Orders order in ordersList)
        {
            IQueryable<OrderItems> items = _dbContext.OrderItems
                .AsNoTracking()
                .Where(oi => oi.OrderId == order.OrderId);

            OrderItemDto[] orderItemDto = await items
                .Select(i => new OrderItemDto(i.Product.Name, i.Quantity, i.Price))
                .ToArrayAsync(cancellationToken);

            ordersDtos.Add(new OrdersDto(order.OrderId, order.CustomerId, order.OrderDate, orderItemDto));
        }

        return ordersDtos;
    }

    public async Task<List<OrdersDto>> GetCustomerOrders(Guid customerId, CancellationToken cancellationToken)
    {
        List<OrdersDto> ordersDtos = new List<OrdersDto>();

        List<Orders> orders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .Include(oi => oi.OrderItems)
            .ThenInclude(oi => oi.Product)
            .ToListAsync(cancellationToken);

        foreach (Orders order in orders)
        {
            OrderItemDto[] orderItemDtos = order.OrderItems
                .Select(i => new OrderItemDto(i.Product.Name, i.Quantity, i.Price))
                .ToArray();

            ordersDtos.Add(new OrdersDto(order.OrderId, order.CustomerId, order.OrderDate, orderItemDtos));
        }

        return ordersDtos;
    }

    public async Task<IEnumerable<TopCustomerRevenue>> GetTopCustomersRevenue(DateTimeOffset from, DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        using (NpgsqlConnection connection =
               new NpgsqlConnection(_configuration.GetConnectionString(nameof(StoreDbContext))))
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Successfully connected to PostgreSQL.");

                var parameters = new { from, to };
                CommandDefinition explainCommand = new CommandDefinition(
                    ExplainTopCustomersRevenueSql,
                    parameters,
                    cancellationToken: cancellationToken);
                IEnumerable<string> plan = await connection.QueryAsync<string>(explainCommand);

                _logger.LogInformation(
                    "Top customers revenue EXPLAIN ANALYZE plan:{NewLine}{Plan}",
                    Environment.NewLine,
                    string.Join(Environment.NewLine, plan));

                Stopwatch stopwatch = Stopwatch.StartNew();
                CommandDefinition command = new CommandDefinition(
                    TopCustomersRevenueSql,
                    parameters,
                    cancellationToken: cancellationToken);
                var response = await connection.QueryAsync<TopCustomerRevenue>(command);
                stopwatch.Stop();

                _logger.LogInformation(
                    "Top customers revenue query completed in {ElapsedMilliseconds} ms.",
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Top customers revenue query failed.");
                throw;
            }
        }
    }

    // НЕПРАВИЛЬНО:
    //
    // string sql = $"""
    //     SELECT c."CustomerId", c."FirstName", c."LastName",
    //            SUM(oi."Quantity" * oi."Price") AS "Revenue"
    //     FROM "Customers" c
    //     JOIN "Orders" o USING ("CustomerId")
    //     JOIN "OrderItems" oi USING ("OrderId")
    //     WHERE o."OrderDate" BETWEEN '{from}' AND '{to}'
    //     GROUP BY c."CustomerId", c."FirstName", c."LastName"
    //     ORDER BY "Revenue" DESC
    //     """;
    //
    // Здесь значения пользователя вставляются прямо в текст SQL.
    //
    // Если бы `to` был строкой и пользователь передал:
    //
    // 2026-01-01' OR 1=1 --
    //
    // итоговый SQL стал бы:
    //
    // WHERE o."OrderDate" BETWEEN '2025-01-01'
    //                         AND '2026-01-01' OR 1=1 --'
    //
    // OR 1=1 всегда TRUE, поэтому условие фильтрации фактически обходится.

    public async Task<CreateOrderResult> CreateOrder(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidationResult? validationError = request.Validate(new ValidationContext(request)).FirstOrDefault();
        if (validationError is not null)
            return new CreateOrderResult(false, Error: validationError.ErrorMessage,
                ErrorCode: CreateOrderError.InvalidRequest);

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await _dbContext.Customers.AnyAsync(c => c.CustomerId == request.CustomerId, cancellationToken))
                return new CreateOrderResult(false, Error: "No customer with such an ID was found.",
                    ErrorCode: CreateOrderError.CustomerNotFound);

            Orders order = new Orders
            {
                OrderId = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                OrderDate = DateTimeOffset.UtcNow
            };

            List<OrderItemDto> createdOrderItems = [];
            
            foreach (CreateOrderItemRequest item in request.OrderItems.OrderBy(i => i.ProductId))
            {
                int updated = await _dbContext.Products
                    .Where(p => p.ProductId == item.ProductId && p.Quantity >= item.Quantity)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Quantity, p => p.Quantity - item.Quantity),
                        cancellationToken);

                if (updated == 0)
                {
                    bool exists = await _dbContext.Products.AnyAsync(p => p.ProductId == item.ProductId, cancellationToken);
                    return exists
                        ? new CreateOrderResult(false, Error: "Quantity of the product has been exceeded.",
                            ErrorCode: CreateOrderError.InsufficientStock)
                        : new CreateOrderResult(false, Error: "The product with that ID was not found.",
                            ErrorCode: CreateOrderError.ProductNotFound);
                }
                
                var product = await _dbContext.Products.AsNoTracking()
                    .Where(p => p.ProductId == item.ProductId)
                    .Select(p => new { p.Name, p.Price })
                    .SingleAsync(cancellationToken);

                OrderItems orderItem = new OrderItems
                {
                    OrderId = order.OrderId,
                    Order = order,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = product.Price
                };

                order.OrderItems.Add(orderItem);
                createdOrderItems.Add(new OrderItemDto(product.Name, orderItem.Quantity, orderItem.Price));
            }

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            OrdersDto createdOrder = new OrdersDto(
                order.OrderId,
                order.CustomerId,
                order.OrderDate,
                createdOrderItems.ToArray());

            return new CreateOrderResult(true, createdOrder);
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                _logger.LogWarning(rollbackException, "Order rollback failed.");
            }

            throw;
        }
    }
}
