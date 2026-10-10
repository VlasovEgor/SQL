# StoreAnalytics

Учебный проект по блоку «Базы данных».

## Стек

- .NET 8
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Dapper
- Redis

## Что реализовано

- схема магазина: `Customers`, `Products`, `Orders`, `OrderItems`;
- SQL-запросы с `JOIN`, `GROUP BY`, подзапросами и оконными функциями;
- индексы и `EXPLAIN ANALYZE`;
- EF Core migrations;
- демонстрация и исправление N+1;
- аналитический отчёт через Dapper;
- Redis cache-aside с TTL 5 минут;
- параметризованные SQL-запросы;
- транзакция создания заказа с rollback;
- `EXPLAIN ANALYZE` отчёта из приложения;
- покрывающие индексы `INCLUDE`;
- защита от cache stampede через Redis lock.

## SQL-скрипты

SQL-часть находится в папке:

```text
sql/
├── schema.sql
├── seed.sql
└── queries.sql
```

## Запуск

Указать строки подключения к PostgreSQL и Redis в конфигурации приложения:

```json
{
  "ConnectionStrings": {
    "StoreDbContext": "Host=localhost;Port=5432;Database=store_analytics;Username=postgres;Password=Egor2001",
    "Redis": "localhost:6379"
  }
}
```

Redis можно запустить через Docker:

```bash
docker run --name store-redis -p 6379:6379 -d redis:7
```

Применить миграции:

```bash
dotnet ef database update
```

Запустить приложение:

```bash
dotnet run
```

После запуска API доступен через Swagger.

## Основные endpoints

```http
GET /api/analytics/orders?customerId={customerId}
```

Возвращает заказы клиента с позициями и товарами.

```http
POST /api/analytics/orders
```

Создаёт заказ с позициями в одной транзакции.

```http
GET /api/analytics/top_customers_revenue?from={date}&to={date}
```

Возвращает отчёт по выручке клиентов. Отчёт реализован через Dapper и кэшируется в Redis.

## Индексы и EXPLAIN ANALYZE

Для запроса заказов клиента за период использовался составной индекс:

```sql
CREATE INDEX ix_orders_customer_created
ON orders (customers_id, created_at);
```

Результат замера:

| Вариант | План | Execution Time |
|---|---|---:|
| Без индекса | `Seq Scan` | 0.781 ms |
| С индексом | `Bitmap Heap Scan` + `Bitmap Index Scan` | 0.067 ms |

Дополнительно проверены:

| Проверка | До | После |
|---|---:|---:|
| `LOWER(email)` | 3.092 ms | 0.096 ms |
| `LIKE '%123%'` + `pg_trgm` | 0.162 ms | 0.062 ms |

## N+1

В наивной реализации сначала выполнялся один запрос к `Orders`, затем отдельный запрос к `OrderItems` для каждого заказа.

Исправленная версия использует `Include` / `ThenInclude`, поэтому связанные данные загружаются одним SQL-запросом с JOIN.

## Dapper

Тяжёлый аналитический отчёт реализован через Dapper, поскольку здесь нужен конкретный агрегирующий SQL и контроль плана выполнения.

SQL параметризован:

```sql
WHERE o."OrderDate" BETWEEN @from AND @to
```

Параметры передаются отдельно:

```csharp
new { from, to }
```

Значения пользователя не конкатенируются с текстом SQL.

## Redis

Для отчёта используется cache-aside с TTL 5 минут.

Проверено:

- первый запрос выполняется через PostgreSQL;
- повторный запрос с теми же параметрами берётся из Redis.

Защита от cache stampede реализована через Redis lock: один запрос пересчитывает отчёт, остальные ждут появления значения в кэше.

## Транзакция создания заказа

Проверен сценарий, в котором одна из позиций заказа содержит несуществующий товар.

До запроса:

```text
Orders: 10003
OrderItems: 30009
```

После `400 Bad Request`:

```text
Orders: 10003
OrderItems: 30009
```

Частично созданный заказ в базе не остаётся — транзакция откатывается полностью.

## Покрывающие индексы

Добавлены:

```sql
CREATE INDEX "IX_OrderItems_OrderId_Report"
ON "OrderItems" ("OrderId")
INCLUDE ("Quantity", "Price");
```

```sql
CREATE INDEX "IX_Orders_OrderDate_Report"
ON "Orders" ("OrderDate")
INCLUDE ("OrderId", "CustomerId");
```

Результат замера:

| Вариант | Execution Time |
|---|---:|
| Без covering indexes | 13.048 ms |
| С covering indexes | 15.226 ms |

На текущем объёме данных PostgreSQL в обоих случаях выбрал `Seq Scan`, поэтому измеримого выигрыша нет. Для выбранного широкого диапазона дат последовательное чтение оказалось дешевле использования индексов.

## Проверка миграций

Миграции проверены на новой пустой базе.

После:

```bash
dotnet ef database update
```

создаются таблицы:

- `Customers`
- `Orders`
- `OrderItems`
- `Products`
- `__EFMigrationsHistory`

Также применяются все миграции, включая `AddReportCoveringIndexes`.
