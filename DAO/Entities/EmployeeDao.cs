using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class EmployeeDao(BookStoreDbContext context) : EntityDao<Employee>(context)
{
    public Task<bool> ExistsByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(employee => employee.Id == id, cancellationToken);

    public Task<List<Employee>> GetAllWithAccountsAsync(CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Include(employee => employee.UserAccount).ThenInclude(account => account!.Role)
            .OrderBy(employee => employee.LastName).ThenBy(employee => employee.FirstName).ToListAsync(cancellationToken);

    public Task<Employee?> FindWithAccountAsync(string id, CancellationToken cancellationToken = default) =>
        Query.Include(employee => employee.UserAccount).SingleOrDefaultAsync(employee => employee.Id == id, cancellationToken);

    public Task<bool> HasPurchaseReceiptsAsync(string id, CancellationToken cancellationToken = default) =>
        Context.PurchaseReceipts.AnyAsync(receipt => receipt.EmployeeId == id, cancellationToken);

    public Task<string?> FindIdByAccountAsync(int accountId, CancellationToken cancellationToken = default) =>
        Query.Where(employee => employee.UserAccountId == accountId).Select(employee => employee.Id).SingleOrDefaultAsync(cancellationToken);

    public Task<string?> FindFirstIdByAccountAsync(int accountId, CancellationToken cancellationToken = default) =>
        Query.Where(employee => employee.UserAccountId == accountId).Select(employee => employee.Id).FirstOrDefaultAsync(cancellationToken);
}
