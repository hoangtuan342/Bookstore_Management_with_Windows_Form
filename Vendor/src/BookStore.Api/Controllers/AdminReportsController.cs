using BookStore.Infrastructure.Persistence;
using BookStore.Shared.Models;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize(Roles = StaffRoles.ManagerOrAccountant)]
[Route("api/admin/reports")]
public sealed class AdminReportsController(BookStoreDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 7, 365);
        var today = DateTime.UtcNow.Date;
        var since = today.AddDays(1 - days);
        var previousSince = since.AddDays(-days);
        var periodEnd = today.AddDays(1);
        var allOrders = await db.Orders.AsNoTracking()
            .Where(order => order.CreatedAt >= since && order.CreatedAt < periodEnd)
            .ToListAsync(cancellationToken);
        var orders = await db.Orders.AsNoTracking()
            .Where(order => order.OrderStatus == "Completed" && order.CreatedAt >= since && order.CreatedAt < periodEnd)
            .Include(order => order.Customer)
            .Include(order => order.Lines).ThenInclude(line => line.Book).ThenInclude(book => book.Category)
            .ToListAsync(cancellationToken);
        var previousPeriodOrders = await db.Orders.AsNoTracking()
            .Where(order => order.OrderStatus == "Completed" && order.CreatedAt >= previousSince && order.CreatedAt < since)
            .Select(order => new { order.TotalAmount })
            .ToListAsync(cancellationToken);
        var receiptLines = await db.PurchaseReceiptLines.AsNoTracking()
            .Where(line => line.Receipt.Status == "Posted" && line.Receipt.CreatedAt >= since)
            .Select(line => new { line.Receipt.CreatedAt, line.LineTotal })
            .ToListAsync(cancellationToken);
        var averageCosts = await db.PurchaseReceiptLines.AsNoTracking()
            .Where(line => line.Receipt.Status == "Posted")
            .GroupBy(line => line.BookId)
            .Select(group => new { BookId = group.Key, Quantity = group.Sum(line => line.Quantity), Total = group.Sum(line => line.LineTotal) })
            .ToDictionaryAsync(item => item.BookId, item => item.Quantity == 0 ? 0m : item.Total / item.Quantity, cancellationToken);
        var knownCostQuantity = orders.SelectMany(order => order.Lines)
            .Where(line => averageCosts.ContainsKey(line.BookId)).Sum(line => line.Quantity);
        var soldQuantity = orders.SelectMany(order => order.Lines).Sum(line => line.Quantity);
        var estimatedCost = orders.SelectMany(order => order.Lines)
            .Sum(line => averageCosts.GetValueOrDefault(line.BookId) * line.Quantity);
        var revenueByDay = Enumerable.Range(0, days).Select(offset =>
        {
            var day = since.AddDays(offset);
            return new
            {
                Date = day,
                Revenue = orders.Where(order => order.CreatedAt.Date == day).Sum(order => order.TotalAmount),
                PurchaseSpend = receiptLines.Where(line => line.CreatedAt.Date == day).Sum(line => line.LineTotal),
                Orders = allOrders.Count(order => order.CreatedAt.Date == day)
            };
        }).ToArray();
        var bestSellers = orders.SelectMany(order => order.Lines)
            .GroupBy(line => new { line.BookId, line.Book.Title })
            .Select(group => new { group.Key.BookId, group.Key.Title, Quantity = group.Sum(line => line.Quantity), Revenue = group.Sum(line => line.LineTotal) })
            .OrderByDescending(item => item.Quantity).Take(10).ToArray();

        var revenueByCategory = orders.SelectMany(order =>
        {
            var lineSubtotal = order.Lines.Sum(line => line.LineTotal);
            return order.Lines.Select(line => new
            {
                Category = line.Book.Category.Name,
                line.Quantity,
                Revenue = lineSubtotal == 0m ? 0m : order.TotalAmount * line.LineTotal / lineSubtotal
            });
        })
            .GroupBy(line => line.Category)
            .Select(group => new CategorySales(group.Key, group.Sum(line => line.Quantity), group.Sum(line => line.Revenue)))
            .OrderByDescending(item => item.Revenue).ToArray();

        var topCustomers = orders.GroupBy(order => new { order.CustomerId, order.Customer.FullName })
            .Select(group => new CustomerSales(group.Key.CustomerId, group.Key.FullName, group.Count(),
                group.Sum(order => order.Lines.Sum(line => line.Quantity)), group.Sum(order => order.TotalAmount)))
            .OrderByDescending(item => item.Revenue).Take(8).ToArray();

        var inventoryByCategory = await db.Books.AsNoTracking().Where(book => book.IsActive)
            .Select(book => new { Category = book.Category.Name, book.StockQuantity, book.SalePrice })
            .ToListAsync(cancellationToken);
        var stockSummary = inventoryByCategory.GroupBy(book => book.Category)
            .Select(group => new CategoryInventorySummary(group.Key, group.Count(), group.Sum(book => book.StockQuantity),
                group.Count(book => book.StockQuantity <= 5), group.Sum(book => book.StockQuantity * book.SalePrice)))
            .OrderBy(item => item.StockQuantity).ToArray();

        var revenue = orders.Sum(order => order.TotalAmount);
        var previousRevenue = previousPeriodOrders.Sum(order => order.TotalAmount);
        var previousCompletedCount = previousPeriodOrders.Count;
        decimal? ChangePercent(decimal current, decimal previous) => previous == 0m
            ? null
            : decimal.Round((current - previous) * 100m / previous, 1);

        return Ok(new
        {
            Days = days,
            TotalOrders = allOrders.Count,
            CompletedOrders = orders.Count,
            ProcessingOrders = allOrders.Count(order => order.OrderStatus is "Confirmed" or "Shipping"),
            PendingOrders = allOrders.Count(order => order.OrderStatus == "Pending"),
            CancelledOrders = allOrders.Count(order => order.OrderStatus == "Cancelled"),
            Revenue = revenue,
            AverageOrderValue = orders.Count == 0 ? 0m : orders.Average(order => order.TotalAmount),
            Discounts = orders.Sum(order => order.DiscountAmount),
            PurchaseSpend = receiptLines.Sum(line => line.LineTotal),
            EstimatedCostOfGoods = estimatedCost,
            EstimatedGrossProfit = revenue - estimatedCost,
            CostCoveragePercent = soldQuantity == 0 ? 0m : decimal.Round(knownCostQuantity * 100m / soldQuantity, 1),
            PreviousPeriodRevenue = previousRevenue,
            PreviousPeriodCompletedOrders = previousCompletedCount,
            RevenueChangePercent = ChangePercent(revenue, previousRevenue),
            CompletedOrdersChangePercent = ChangePercent(orders.Count, previousCompletedCount),
            RevenueByDay = revenueByDay,
            BestSellers = bestSellers,
            RevenueByCategory = revenueByCategory,
            TopCustomers = topCustomers,
            InventoryByCategory = stockSummary
        });
    }
}
