using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    public class AdminDashboardService
    {
        private readonly BookStoreDao dao;

        public AdminDashboardService(BookStoreDao dao)
        {
            this.dao = dao;
        }

        public ManagerDashboardDto GetSummary()
        {
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var revenue = dao.Reports.GetMonthRevenue(monthStart);
            var ordersToday = dao.Reports.CountOrdersToday(today);
            var pendingOrders = dao.Reports.CountOrdersByStatus("Pending");
            var activeBooks = dao.Reports.CountActiveBooks();
            var lowStockBooks = dao.Reports.CountLowStockBooks(5);
            var customerCount = dao.Reports.CountCustomers();
            var recentOrders = dao.Reports.GetRecentOrders(8);

            return new ManagerDashboardDto(
                revenue,
                ordersToday,
                pendingOrders,
                activeBooks,
                lowStockBooks,
                customerCount,
                recentOrders);
        }

        public async Task<ManagerDashboardDto> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var revenueTask = dao.Reports.GetMonthRevenueAsync(monthStart, cancellationToken);
            var ordersTodayTask = dao.Reports.CountOrdersTodayAsync(today, cancellationToken);
            var pendingOrdersTask = dao.Reports.CountOrdersByStatusAsync("Pending", cancellationToken);
            var activeBooksTask = dao.Reports.CountActiveBooksAsync(cancellationToken);
            var lowStockBooksTask = dao.Reports.CountLowStockBooksAsync(5, cancellationToken);
            var customerCountTask = dao.Reports.CountCustomersAsync(cancellationToken);
            var recentOrdersTask = dao.Reports.GetRecentOrdersAsync(8, cancellationToken);

            await Task.WhenAll(revenueTask, ordersTodayTask, pendingOrdersTask, activeBooksTask, lowStockBooksTask, customerCountTask, recentOrdersTask);

            return new ManagerDashboardDto(
                await revenueTask,
                await ordersTodayTask,
                await pendingOrdersTask,
                await activeBooksTask,
                await lowStockBooksTask,
                await customerCountTask,
                await recentOrdersTask);
        }
    }
}
