using BookStore.Infrastructure.Persistence;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize(Roles = StaffRoles.ManagerOrAccountant)]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController(BookStoreDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var revenue = await db.Orders.AsNoTracking()
            .Where(order => order.OrderStatus == "Completed" && order.CreatedAt >= monthStart)
            .SumAsync(order => (decimal?)order.TotalAmount, cancellationToken) ?? 0m;
        var recentOrders = await db.Orders.AsNoTracking()
            .OrderByDescending(order => order.CreatedAt)
            .Take(8)
            .Select(order => new
            {
                order.Id,
                order.CreatedAt,
                order.OrderStatus,
                order.TotalAmount,
                CustomerName = order.Customer.FullName
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            RevenueThisMonth = revenue,
            OrdersToday = await db.Orders.CountAsync(order => order.CreatedAt >= today, cancellationToken),
            PendingOrders = await db.Orders.CountAsync(order => order.OrderStatus == "Pending", cancellationToken),
            ActiveBooks = await db.Books.CountAsync(book => book.IsActive, cancellationToken),
            LowStockBooks = await db.Books.CountAsync(book => book.IsActive && book.StockQuantity <= 5, cancellationToken),
            CustomerCount = await db.Customers.CountAsync(cancellationToken),
            RecentOrders = recentOrders
        });
    }
}
