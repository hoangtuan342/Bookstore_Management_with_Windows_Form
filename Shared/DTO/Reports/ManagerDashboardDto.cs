
namespace TrangSach.Shared.DTO
{
    public class ManagerDashboardDto
    {
        public decimal RevenueThisMonth { get; set; }
        public int OrdersToday { get; set; }
        public int PendingOrders { get; set; }
        public int ActiveBooks { get; set; }
        public int LowStockBooks { get; set; }
        public int CustomerCount { get; set; }
        public IReadOnlyList<ManagerRecentOrderDto> RecentOrders { get; set; } = new List<ManagerRecentOrderDto>();

        public ManagerDashboardDto()
        {
        }

        public ManagerDashboardDto(decimal revenueThisMonth, int ordersToday, int pendingOrders, int activeBooks, int lowStockBooks, int customerCount, IReadOnlyList<ManagerRecentOrderDto> recentOrders)
        {
            this.RevenueThisMonth = revenueThisMonth;
            this.OrdersToday = ordersToday;
            this.PendingOrders = pendingOrders;
            this.ActiveBooks = activeBooks;
            this.LowStockBooks = lowStockBooks;
            this.CustomerCount = customerCount;
            this.RecentOrders = recentOrders;
        }
    }
}

