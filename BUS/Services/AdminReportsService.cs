using TrangSach.DAO.Entities;
using BookStore.Shared.Models;

namespace TrangSach.BUS.Services;

public sealed class AdminReportsService(BookStoreDao dao)
{
    public Task<ManagerReport> GetAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 3650);
        var to = DateOnly.FromDateTime(DateTime.Now);
        return GetAsync(to.AddDays(1 - days), to, cancellationToken);
    }

    public async Task<ManagerReport> GetAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        if (toDate < fromDate) throw new ArgumentException("Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.", nameof(toDate));
        var days = toDate.DayNumber - fromDate.DayNumber + 1;
        if (days > 3650) throw new ArgumentOutOfRangeException(nameof(toDate), "Khoảng thống kê tối đa là 10 năm.");

        var fromUtc = ToUtc(fromDate);
        var untilUtc = ToUtc(toDate.AddDays(1));
        var previousFromUtc = ToUtc(fromDate.AddDays(-days));
        var allOrders = await dao.Orders.GetInPeriodAsync(fromUtc, untilUtc, cancellationToken);
        var orders = await dao.Orders.GetCompletedWithLinesForPeriodAsync(fromUtc, untilUtc, cancellationToken);
        var previousPeriodOrders = await dao.Orders.GetCompletedRevenueValuesAsync(previousFromUtc, fromUtc, cancellationToken);
        var receiptLines = await dao.PurchaseReceiptLines.GetPostedSpendSinceAsync(fromUtc, cancellationToken);
        receiptLines = receiptLines.Where(line => line.CreatedAt < untilUtc).ToList();
        var averageCosts = await dao.PurchaseReceiptLines.GetAverageUnitCostsAsync(cancellationToken);
        var knownCostQuantity = orders.SelectMany(order => order.Lines).Where(line => averageCosts.ContainsKey(line.BookId)).Sum(line => line.Quantity);
        var soldQuantity = orders.SelectMany(order => order.Lines).Sum(line => line.Quantity);
        var estimatedCost = orders.SelectMany(order => order.Lines).Sum(line => averageCosts.GetValueOrDefault(line.BookId) * line.Quantity);

        var revenueByDay = Enumerable.Range(0, days).Select(offset =>
        {
            var day = fromDate.AddDays(offset);
            var localDay = day.ToDateTime(TimeOnly.MinValue);
            return new RevenueByDay(localDay,
                orders.Where(order => order.CreatedAt.ToLocalTime().Date == localDay).Sum(order => order.TotalAmount),
                receiptLines.Where(line => line.CreatedAt.ToLocalTime().Date == localDay).Sum(line => line.LineTotal),
                allOrders.Count(order => order.CreatedAt.ToLocalTime().Date == localDay));
        }).ToArray();

        var netLines = orders.SelectMany(order =>
        {
            var subtotal = order.Lines.Sum(line => line.LineTotal);
            return order.Lines.Select(line => new
            {
                Order = order,
                Line = line,
                NetRevenue = subtotal == 0m ? 0m : order.TotalAmount * line.LineTotal / subtotal
            });
        }).ToArray();
        var bestSellers = netLines.GroupBy(row => new { row.Line.BookId, row.Line.Book.Title })
            .Select(group => new BestSeller(group.Key.BookId, group.Key.Title,
                group.Sum(row => row.Line.Quantity), group.Sum(row => row.NetRevenue)))
            .OrderByDescending(item => item.Quantity).Take(10).ToArray();
        var revenueByCategory = netLines.GroupBy(row => row.Line.Book.Category.Name)
            .Select(group => new CategorySales(group.Key, group.Sum(row => row.Line.Quantity), group.Sum(row => row.NetRevenue)))
            .OrderByDescending(item => item.Revenue).ToArray();
        var topCustomers = orders.GroupBy(order => new { order.CustomerId, order.Customer.FullName })
            .Select(group => new CustomerSales(group.Key.CustomerId, group.Key.FullName, group.Count(),
                group.Sum(order => order.Lines.Sum(line => line.Quantity)), group.Sum(order => order.TotalAmount)))
            .OrderByDescending(item => item.Revenue).Take(8).ToArray();
        var monthlySales = orders.GroupBy(order =>
                (Year: order.CreatedAt.ToLocalTime().Year, Month: order.CreatedAt.ToLocalTime().Month))
            .ToDictionary(group => group.Key, group => (Revenue: group.Sum(order => order.TotalAmount), Orders: group.Count()));
        var monthlyPurchases = receiptLines.GroupBy(line =>
                (Year: line.CreatedAt.ToLocalTime().Year, Month: line.CreatedAt.ToLocalTime().Month))
            .ToDictionary(group => group.Key, group => group.Sum(line => line.LineTotal));
        var monthlyProfit = netLines.GroupBy(row =>
                (Year: row.Order.CreatedAt.ToLocalTime().Year, Month: row.Order.CreatedAt.ToLocalTime().Month))
            .ToDictionary(group => group.Key, group => group.Sum(row => row.NetRevenue - averageCosts.GetValueOrDefault(row.Line.BookId) * row.Line.Quantity));
        var monthKeys = EnumerateMonths(fromDate, toDate);
        var revenueByMonth = monthKeys.Select(key =>
        {
            var sales = monthlySales.GetValueOrDefault(key);
            return new MonthlyRevenue(key.Year, key.Month, sales.Revenue, sales.Orders,
                monthlyPurchases.GetValueOrDefault(key), monthlyProfit.GetValueOrDefault(key));
        }).ToArray();

        var quarterlySales = orders.GroupBy(order =>
                (Year: order.CreatedAt.ToLocalTime().Year, Quarter: (order.CreatedAt.ToLocalTime().Month - 1) / 3 + 1))
            .ToDictionary(group => group.Key, group => (Revenue: group.Sum(order => order.TotalAmount), Orders: group.Count()));
        var quarterlyPurchases = receiptLines.GroupBy(line =>
                (Year: line.CreatedAt.ToLocalTime().Year, Quarter: (line.CreatedAt.ToLocalTime().Month - 1) / 3 + 1))
            .ToDictionary(group => group.Key, group => group.Sum(line => line.LineTotal));
        var quarterlyProfit = netLines.GroupBy(row =>
                (Year: row.Order.CreatedAt.ToLocalTime().Year, Quarter: (row.Order.CreatedAt.ToLocalTime().Month - 1) / 3 + 1))
            .ToDictionary(group => group.Key, group => group.Sum(row => row.NetRevenue - averageCosts.GetValueOrDefault(row.Line.BookId) * row.Line.Quantity));
        var quarterKeys = EnumerateQuarters(fromDate, toDate);
        var revenueByQuarter = quarterKeys.Select(key =>
        {
            var sales = quarterlySales.GetValueOrDefault(key);
            return new QuarterlyRevenue(key.Year, key.Quarter, sales.Revenue, sales.Orders,
                quarterlyPurchases.GetValueOrDefault(key), quarterlyProfit.GetValueOrDefault(key));
        }).ToArray();

        var yearlySales = orders.GroupBy(order => order.CreatedAt.ToLocalTime().Year)
            .ToDictionary(group => group.Key, group => (Revenue: group.Sum(order => order.TotalAmount), Orders: group.Count()));
        var yearlyPurchases = receiptLines.GroupBy(line => line.CreatedAt.ToLocalTime().Year)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.LineTotal));
        var yearlyProfit = netLines.GroupBy(row => row.Order.CreatedAt.ToLocalTime().Year)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.NetRevenue - averageCosts.GetValueOrDefault(row.Line.BookId) * row.Line.Quantity));
        var revenueByYear = Enumerable.Range(fromDate.Year, toDate.Year - fromDate.Year + 1).Select(year =>
        {
            var sales = yearlySales.GetValueOrDefault(year);
            return new YearlyRevenue(year, sales.Revenue, sales.Orders,
                yearlyPurchases.GetValueOrDefault(year), yearlyProfit.GetValueOrDefault(year));
        }).ToArray();
        var revenueByEmployeeYear = orders.GroupBy(order => new
                { EmployeeId = order.EmployeeId ?? "(Chưa phân công)", EmployeeName = order.Employee is null ? "Chưa phân công" : $"{order.Employee.LastName} {order.Employee.FirstName}".Trim(), Year = order.CreatedAt.ToLocalTime().Year })
            .OrderBy(group => group.Key.Year).ThenBy(group => group.Key.EmployeeName)
            .Select(group => new EmployeeYearSales(group.Key.EmployeeId, group.Key.EmployeeName,
                group.Key.Year, group.Count(), group.Sum(order => order.TotalAmount))).ToArray();
        var productSalesByQuarter = netLines.GroupBy(row => new
                { row.Line.BookId, row.Line.Book.Title, Year = row.Order.CreatedAt.ToLocalTime().Year,
                    Quarter = (row.Order.CreatedAt.ToLocalTime().Month - 1) / 3 + 1 })
            .OrderBy(group => group.Key.Year).ThenBy(group => group.Key.Quarter).ThenByDescending(group => group.Sum(row => row.NetRevenue))
            .Select(group => new ProductQuarterSales(group.Key.BookId, group.Key.Title, group.Key.Year,
                group.Key.Quarter, group.Sum(row => row.Line.Quantity), group.Sum(row => row.NetRevenue))).ToArray();

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
            revenueByDay, bestSellers, revenueByCategory, topCustomers, stockSummary,
            fromDate.ToDateTime(TimeOnly.MinValue), toDate.ToDateTime(TimeOnly.MinValue),
            revenueByMonth, revenueByQuarter, revenueByYear, revenueByEmployeeYear, productSalesByQuarter);
    }

    private static DateTime ToUtc(DateOnly date)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, TimeZoneInfo.Local);
    }

    private static IEnumerable<(int Year, int Month)> EnumerateMonths(DateOnly from, DateOnly to)
    {
        var year = from.Year;
        var month = from.Month;
        while (year < to.Year || year == to.Year && month <= to.Month)
        {
            yield return (year, month);
            if (++month == 13) { month = 1; year++; }
        }
    }

    private static IEnumerable<(int Year, int Quarter)> EnumerateQuarters(DateOnly from, DateOnly to)
    {
        var year = from.Year;
        var quarter = (from.Month - 1) / 3 + 1;
        var lastYear = to.Year;
        var lastQuarter = (to.Month - 1) / 3 + 1;
        while (year < lastYear || year == lastYear && quarter <= lastQuarter)
        {
            yield return (year, quarter);
            if (++quarter == 5) { quarter = 1; year++; }
        }
    }
}
