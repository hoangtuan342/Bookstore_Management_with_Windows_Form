using TrangSach.DAO.Entities;
using BookStore.Shared.Models;

namespace TrangSach.BUS.Services;

public sealed class AdminReportsService(BookStoreDao dao)
{
    public async Task<ManagerReport> GetAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 7, 365);
        var today = DateTime.UtcNow.Date;
        var since = today.AddDays(1 - days);
        var previousSince = since.AddDays(-days);
        var periodEnd = today.AddDays(1);
        var allOrders = await dao.Orders.GetInPeriodAsync(since, periodEnd, cancellationToken);
        var orders = await dao.Orders.GetCompletedWithLinesForPeriodAsync(since, periodEnd, cancellationToken);
        var previousPeriodOrders = await dao.Orders.GetCompletedRevenueValuesAsync(previousSince, since, cancellationToken);
        var receiptLines = await dao.PurchaseReceiptLines.GetPostedSpendSinceAsync(since, cancellationToken);
        var averageCosts = await dao.PurchaseReceiptLines.GetAverageUnitCostsAsync(cancellationToken);
        var knownCostQuantity = orders.SelectMany(order => order.Lines).Where(line => averageCosts.ContainsKey(line.BookId)).Sum(line => line.Quantity);
        var soldQuantity = orders.SelectMany(order => order.Lines).Sum(line => line.Quantity);
        var estimatedCost = orders.SelectMany(order => order.Lines).Sum(line => averageCosts.GetValueOrDefault(line.BookId) * line.Quantity);
        var revenueByDay = Enumerable.Range(0, days).Select(offset =>
        {
            var day = since.AddDays(offset);
            return new RevenueByDay(
                day,
                orders.Where(order => order.CreatedAt.Date == day).Sum(order => order.TotalAmount),
                receiptLines.Where(line => line.CreatedAt.Date == day).Sum(line => line.LineTotal),
                allOrders.Count(order => order.CreatedAt.Date == day));
        }).ToArray();
        var bestSellers = orders.SelectMany(order => order.Lines)
            .GroupBy(line => new { line.BookId, line.Book.Title })
            .Select(group => new BestSeller(group.Key.BookId, group.Key.Title, group.Sum(line => line.Quantity), group.Sum(line => line.LineTotal)))
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
        }).GroupBy(line => line.Category)
            .Select(group => new CategorySales(group.Key, group.Sum(line => line.Quantity), group.Sum(line => line.Revenue)))
            .OrderByDescending(item => item.Revenue).ToArray();
        var topCustomers = orders.GroupBy(order => new { order.CustomerId, order.Customer.FullName })
            .Select(group => new CustomerSales(group.Key.CustomerId, group.Key.FullName, group.Count(),
                group.Sum(order => order.Lines.Sum(line => line.Quantity)), group.Sum(order => order.TotalAmount)))
            .OrderByDescending(item => item.Revenue).Take(8).ToArray();
        var inventory = await dao.Books.GetActiveInventoryAsync(cancellationToken);
        var stockSummary = inventory.GroupBy(book => book.CategoryName)
            .Select(group => new CategoryInventorySummary(group.Key, group.Count(), group.Sum(book => book.StockQuantity),
                group.Count(book => book.StockQuantity <= 5), group.Sum(book => book.StockQuantity * book.SalePrice)))
            .OrderBy(item => item.StockQuantity).ToArray();
        var revenue = orders.Sum(order => order.TotalAmount);
        var previousRevenue = previousPeriodOrders.Sum();
        var previousCompletedCount = previousPeriodOrders.Count;
        decimal? ChangePercent(decimal current, decimal previous) => previous == 0m ? null : decimal.Round((current - previous) * 100m / previous, 1);

        return new ManagerReport(days, allOrders.Count, orders.Count,
            allOrders.Count(order => order.OrderStatus is "Confirmed" or "Shipping"),
            allOrders.Count(order => order.OrderStatus == "Pending"), allOrders.Count(order => order.OrderStatus == "Cancelled"),
            revenue, orders.Count == 0 ? 0m : orders.Average(order => order.TotalAmount), orders.Sum(order => order.DiscountAmount),
            receiptLines.Sum(line => line.LineTotal), estimatedCost, revenue - estimatedCost,
            soldQuantity == 0 ? 0m : decimal.Round(knownCostQuantity * 100m / soldQuantity, 1), previousRevenue,
            previousCompletedCount, ChangePercent(revenue, previousRevenue), ChangePercent(orders.Count, previousCompletedCount),
            revenueByDay, bestSellers, revenueByCategory, topCustomers, stockSummary);
    }
}
