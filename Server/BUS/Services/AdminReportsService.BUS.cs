using System;
using System.Collections.Generic;
using System.Linq;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    public class AdminReportsService
    {
        private readonly BookStoreDao dao;

        public AdminReportsService(BookStoreDao dao)
        {
            this.dao = dao;
        }

        public ManagerReportDto Get(int days = 30)
        {
            days = Math.Clamp(days, 1, 3650);
            var to = DateOnly.FromDateTime(DateTime.Now);
            return Get(to.AddDays(1 - days), to);
        }

        public ManagerReportDto Get(DateOnly fromDate, DateOnly toDate)
        {
            if (toDate < fromDate)
            {
                throw new ArgumentException("Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.", nameof(toDate));
            }
            var days = toDate.DayNumber - fromDate.DayNumber + 1;
            if (days > 3650)
            {
                throw new ArgumentOutOfRangeException(nameof(toDate), "Khoảng thống kê tối đa là 10 năm.");
            }

            var fromUtc = ToUtc(fromDate);
            var untilUtc = ToUtc(toDate.AddDays(1));
            var previousFromUtc = ToUtc(fromDate.AddDays(-days));

            var allOrders = dao.Reports.GetAllOrdersInPeriod(fromUtc, untilUtc);
            var orders = dao.Reports.GetCompletedOrdersInPeriod(fromUtc, untilUtc);
            var orderLines = dao.Reports.GetOrderLinesForPeriod(fromUtc, untilUtc);
            var previousRevenue = dao.Reports.GetPreviousPeriodRevenue(previousFromUtc, fromUtc);
            var previousCompletedCount = dao.Reports.GetPreviousPeriodOrderCount(previousFromUtc, fromUtc);
            var averageCosts = dao.Reports.GetAveragePurchaseCosts();
            var purchaseSpend = dao.Reports.GetPurchaseSpendInPeriod(fromUtc, untilUtc);
            var purchases = dao.Reports.GetCompletedPurchasesInPeriod(fromUtc, untilUtc);

            var knownCostQuantity = orderLines.Where(line => averageCosts.ContainsKey(line.BookId)).Sum(line => line.Qty);
            var soldQuantity = orderLines.Sum(line => line.Qty);
            var estimatedCost = orderLines.Sum(line => averageCosts.GetValueOrDefault(line.BookId) * line.Qty);

            var purchasesByDay = purchases.GroupBy(p => p.CreatedAt.ToLocalTime().Date)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Total));

            var revenueByDayList = Enumerable.Range(0, days).Select(offset =>
            {
                var day = fromDate.AddDays(offset);
                var localDay = day.ToDateTime(TimeOnly.MinValue);
                return new RevenueByDayDto(localDay,
                    orders.Where(order => order.CreatedAt.ToLocalTime().Date == localDay).Sum(order => order.Total),
                    purchasesByDay.GetValueOrDefault(localDay, 0m),
                    allOrders.Count(order => order.CreatedAt.ToLocalTime().Date == localDay));
            }).ToList();

            var bestSellers = orderLines.GroupBy(row => new { row.BookId, row.BookTitle })
                .Select(group => new BestSellerDto(group.Key.BookId, group.Key.BookTitle,
                    group.Sum(row => row.Qty), group.Sum(row => row.LineTotal)))
                .OrderByDescending(item => item.Quantity).Take(10).ToList();

            var revenueByCategory = orderLines.GroupBy(row => row.CategoryName ?? "Chưa phân loại")
                .Select(group => new CategorySalesDto(group.Key, group.Sum(row => row.Qty), group.Sum(row => row.LineTotal)))
                .OrderByDescending(item => item.Revenue).ToList();

            var topCustomers = orders.GroupBy(order => new { order.CustomerId, order.CustomerName })
                .Select(group => new CustomerSalesDto(group.Key.CustomerId, group.Key.CustomerName, group.Count(),
                    orderLines.Where(l => orders.Any(o => o.OrderId == l.OrderId && o.CustomerId == group.Key.CustomerId)).Sum(l => l.Qty),
                    group.Sum(order => order.Total)))
                .OrderByDescending(item => item.Revenue).Take(8).ToList();

            var monthlyPurchases = purchases.GroupBy(p =>
                    (Year: p.CreatedAt.ToLocalTime().Year, Month: p.CreatedAt.ToLocalTime().Month))
                .ToDictionary(group => group.Key, group => group.Sum(p => p.Total));

            var monthlySales = orders.GroupBy(order =>
                    (Year: order.CreatedAt.ToLocalTime().Year, Month: order.CreatedAt.ToLocalTime().Month))
                .ToDictionary(group => group.Key, group => (Revenue: group.Sum(order => order.Total), Orders: group.Count()));

            var revenueByMonth = EnumerateMonths(fromDate, toDate).Select(slot =>
            {
                var stats = monthlySales.GetValueOrDefault(slot);
                var spend = monthlyPurchases.GetValueOrDefault(slot, 0m);
                return new MonthlyRevenueDto(slot.Year, slot.Month, stats.Revenue, stats.Orders, spend, stats.Revenue - spend);
            }).ToList();

            var quarterlyPurchases = purchases.GroupBy(p =>
                    (Year: p.CreatedAt.ToLocalTime().Year, Quarter: (p.CreatedAt.ToLocalTime().Month - 1) / 3 + 1))
                .ToDictionary(group => group.Key, group => group.Sum(p => p.Total));

            var quarterlySales = orders.GroupBy(order =>
                    (Year: order.CreatedAt.ToLocalTime().Year, Quarter: (order.CreatedAt.ToLocalTime().Month - 1) / 3 + 1))
                .ToDictionary(group => group.Key, group => (Revenue: group.Sum(order => order.Total), Orders: group.Count()));

            var revenueByQuarter = EnumerateQuarters(fromDate, toDate).Select(slot =>
            {
                var stats = quarterlySales.GetValueOrDefault(slot);
                var spend = quarterlyPurchases.GetValueOrDefault(slot, 0m);
                return new QuarterlyRevenueDto(slot.Year, slot.Quarter, stats.Revenue, stats.Orders, spend, stats.Revenue - spend);
            }).ToList();

            var yearlyPurchases = purchases.GroupBy(p => p.CreatedAt.ToLocalTime().Year)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Total));

            var allYears = orders.Select(o => o.CreatedAt.ToLocalTime().Year)
                .Concat(purchases.Select(p => p.CreatedAt.ToLocalTime().Year))
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            var revenueByYear = allYears.Select(y =>
            {
                var yearOrders = orders.Where(o => o.CreatedAt.ToLocalTime().Year == y).ToList();
                var rev = yearOrders.Sum(o => o.Total);
                var spend = yearlyPurchases.GetValueOrDefault(y, 0m);
                return new YearlyRevenueDto(y, rev, yearOrders.Count, spend, rev - spend);
            }).ToList();

            var revenueByEmployeeYear = orders.Where(o => !string.IsNullOrEmpty(o.EmployeeId))
                .GroupBy(order => new
                {
                    EmployeeId = order.EmployeeId!,
                    order.EmployeeName,
                    Year = order.CreatedAt.ToLocalTime().Year
                })
                .OrderBy(group => group.Key.Year).ThenBy(group => group.Key.EmployeeName)
                .Select(group => new EmployeeYearSalesDto(group.Key.EmployeeId, group.Key.EmployeeName,
                    group.Key.Year, group.Count(), group.Sum(order => order.Total))).ToList();

            var revenueByCustomerYear = orders.Where(o => !string.IsNullOrEmpty(o.CustomerId))
                .GroupBy(order => new
                {
                    CustomerId = order.CustomerId,
                    order.CustomerName,
                    Year = order.CreatedAt.ToLocalTime().Year
                })
                .OrderBy(group => group.Key.Year).ThenBy(group => group.Key.CustomerName)
                .Select(group => new CustomerYearSalesDto(group.Key.CustomerId, group.Key.CustomerName,
                    group.Key.Year, group.Count(), group.Sum(order => order.Total))).ToList();

            var productSalesByQuarter = orderLines.GroupBy(row =>
                {
                    var order = orders.FirstOrDefault(o => o.OrderId == row.OrderId);
                    var dt = order.CreatedAt != default ? order.CreatedAt : fromUtc;
                    return new
                    {
                        row.BookId,
                        row.BookTitle,
                        Year = dt.ToLocalTime().Year,
                        Quarter = (dt.ToLocalTime().Month - 1) / 3 + 1
                    };
                })
                .OrderBy(group => group.Key.Year).ThenBy(group => group.Key.Quarter).ThenByDescending(group => group.Sum(row => row.LineTotal))
                .Select(group => new ProductQuarterSalesDto(group.Key.BookId, group.Key.BookTitle, group.Key.Year,
                    group.Key.Quarter, group.Sum(row => row.Qty), group.Sum(row => row.LineTotal))).ToList();

            var inventory = dao.Reports.GetActiveInventory();
            var stockSummary = inventory.GroupBy(book => book.CategoryName ?? "Chưa phân loại")
                .Select(group => new CategoryInventorySummaryDto(group.Key, group.Count(), group.Sum(book => book.Stock),
                    group.Count(book => book.Stock <= 5), group.Sum(book => book.Stock * book.SalePrice)))
                .OrderBy(item => item.StockQuantity).ToList();

            var revenue = orders.Sum(order => order.Total);

            decimal? changePercent(decimal current, decimal previous)
            {
                return previous == 0m ? null : decimal.Round((current - previous) * 100m / previous, 1);
            }

            return new ManagerReportDto(
                days,
                allOrders.Count,
                orders.Count,
                allOrders.Count(order => order.OrderStatus == "Confirmed" || order.OrderStatus == "Shipping"),
                allOrders.Count(order => order.OrderStatus == "Pending"),
                allOrders.Count(order => order.OrderStatus == "Cancelled"),
                revenue,
                orders.Count == 0 ? 0m : orders.Average(order => order.Total),
                orders.Sum(order => order.Discount),
                purchaseSpend,
                estimatedCost,
                revenue - estimatedCost,
                soldQuantity == 0 ? 0m : decimal.Round(knownCostQuantity * 100m / soldQuantity, 1),
                previousRevenue,
                previousCompletedCount,
                changePercent(revenue, previousRevenue),
                changePercent(orders.Count, previousCompletedCount),
                revenueByDayList,
                bestSellers,
                revenueByCategory,
                topCustomers,
                stockSummary,
                fromDate.ToDateTime(TimeOnly.MinValue),
                toDate.ToDateTime(TimeOnly.MinValue),
                revenueByMonth,
                revenueByQuarter,
                revenueByYear,
                revenueByEmployeeYear,
                productSalesByQuarter,
                revenueByCustomerYear);
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
            while (year < to.Year || (year == to.Year && month <= to.Month))
            {
                yield return (year, month);
                month++;
                if (month == 13)
                {
                    month = 1;
                    year++;
                }
            }
        }

        private static IEnumerable<(int Year, int Quarter)> EnumerateQuarters(DateOnly from, DateOnly to)
        {
            var year = from.Year;
            var quarter = (from.Month - 1) / 3 + 1;
            var lastYear = to.Year;
            var lastQuarter = (to.Month - 1) / 3 + 1;
            while (year < lastYear || (year == lastYear && quarter <= lastQuarter))
            {
                yield return (year, quarter);
                quarter++;
                if (quarter == 5)
                {
                    quarter = 1;
                    year++;
                }
            }
        }
    }
}
