using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class SalesOrderDao(BookStoreDbContext context) : EntityDao<SalesOrder>(context)
{
    public Task<List<SalesOrder>> GetCustomerOrdersAsync(string customerId, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Where(order => order.CustomerId == customerId)
            .Include(order => order.Lines).ThenInclude(line => line.Book)
            .OrderByDescending(order => order.CreatedAt).ToListAsync(cancellationToken);

    public async Task<decimal> GetCompletedRevenueSinceAsync(DateTime startUtc, CancellationToken cancellationToken = default) =>
        await Query.Where(order => order.OrderStatus == "Completed" && order.CreatedAt >= startUtc)
            .SumAsync(order => (decimal?)order.TotalAmount, cancellationToken) ?? 0m;

    public Task<int> CountCreatedSinceAsync(DateTime startUtc, CancellationToken cancellationToken = default) =>
        Query.CountAsync(order => order.CreatedAt >= startUtc, cancellationToken);

    public Task<int> CountByStatusAsync(string status, CancellationToken cancellationToken = default) =>
        Query.CountAsync(order => order.OrderStatus == status, cancellationToken);

    public Task<bool> HasCustomerAsync(string customerId, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(order => order.CustomerId == customerId, cancellationToken);

    public Task<bool> HasEmployeeAsync(string employeeId, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(order => order.EmployeeId == employeeId, cancellationToken);

    public Task<List<RecentOrderSummary>> GetRecentSummariesAsync(int count, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().OrderByDescending(order => order.CreatedAt).Take(count)
            .Select(order => new RecentOrderSummary(order.Id, order.CreatedAt, order.OrderStatus,
                order.TotalAmount, order.Customer.FullName)).ToListAsync(cancellationToken);

    public Task<List<SalesOrder>> GetInPeriodAsync(DateTime fromUtc, DateTime untilUtc, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Where(order => order.CreatedAt >= fromUtc && order.CreatedAt < untilUtc)
            .ToListAsync(cancellationToken);

    public Task<List<SalesOrder>> GetCompletedWithLinesForPeriodAsync(DateTime fromUtc, DateTime untilUtc, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Where(order => order.OrderStatus == "Completed"
                && order.CreatedAt >= fromUtc && order.CreatedAt < untilUtc)
            .Include(order => order.Customer)
            .Include(order => order.Lines).ThenInclude(line => line.Book).ThenInclude(book => book.Category)
            .ToListAsync(cancellationToken);

    public Task<List<decimal>> GetCompletedRevenueValuesAsync(DateTime fromUtc, DateTime untilUtc, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Where(order => order.OrderStatus == "Completed"
                && order.CreatedAt >= fromUtc && order.CreatedAt < untilUtc)
            .Select(order => order.TotalAmount).ToListAsync(cancellationToken);

    public Task<List<SalesOrder>> GetManagementOrdersAsync(string? status, CancellationToken cancellationToken = default)
    {
        IQueryable<SalesOrder> query = Query.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(order => order.OrderStatus == status);
        return query.Include(order => order.Customer).Include(order => order.Employee).Include(order => order.Lines).ThenInclude(line => line.Book)
            .OrderByDescending(order => order.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<SalesOrder?> FindWithLinesAndVoucherAsync(string id, CancellationToken cancellationToken = default) =>
        Query.Include(order => order.Lines).ThenInclude(line => line.Book).Include(order => order.Voucher)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public Task<SalesOrder?> FindCustomerOrderWithLinesAndVoucherAsync(string id, string customerId, CancellationToken cancellationToken = default) =>
        Query.Include(order => order.Lines).ThenInclude(line => line.Book).Include(order => order.Voucher)
            .SingleOrDefaultAsync(order => order.Id == id && order.CustomerId == customerId, cancellationToken);
}

public sealed record RecentOrderSummary(string Id, DateTime CreatedAt, string OrderStatus, decimal TotalAmount, string CustomerName);
