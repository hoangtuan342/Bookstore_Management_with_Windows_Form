using System;
using System.Linq;
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
    }
}
