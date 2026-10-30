using BookStore.Shared.Models;
using TrangSach.DAO.Entities;

namespace TrangSach.BUS.Services;

public sealed class AdminDashboardService(BookStoreDao dao)
{
    public async Task<ManagerDashboard> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var recentOrders = await dao.Orders.GetRecentSummariesAsync(8, cancellationToken);
        return new ManagerDashboard(
            await dao.Orders.GetCompletedRevenueSinceAsync(monthStart, cancellationToken),
            await dao.Orders.CountCreatedSinceAsync(today, cancellationToken),
            await dao.Orders.CountByStatusAsync("Pending", cancellationToken),
            await dao.Books.CountActiveAsync(cancellationToken),
            await dao.Books.CountActiveLowStockAsync(5, cancellationToken),
            await dao.Customers.CountAsync(cancellationToken),
            recentOrders.Select(order => new ManagerRecentOrder(
                order.Id, order.CreatedAt, order.OrderStatus, order.TotalAmount, order.CustomerName)).ToArray());
    }
}
