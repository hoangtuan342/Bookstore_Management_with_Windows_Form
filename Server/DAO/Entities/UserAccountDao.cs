using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class UserAccountDao(BookStoreDbContext context) : EntityDao<UserAccount>(context)
{
    public Task<UserAccount?> FindCustomerLoginAsync(string identifier, CancellationToken cancellationToken = default) =>
        Query.Include(account => account.Role).Include(account => account.Customer)
            .SingleOrDefaultAsync(account => account.UserName == identifier || account.Email == identifier, cancellationToken);

    public Task<UserAccount?> FindEmployeeLoginAsync(string identifier, CancellationToken cancellationToken = default) =>
        Query.Include(account => account.Role).Include(account => account.Employee)
            .SingleOrDefaultAsync(account => account.UserName == identifier || account.Email == identifier, cancellationToken);

    public Task<bool> ExistsByUserNameOrEmailAsync(string userName, string email, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(account => account.UserName == userName || account.Email == email, cancellationToken);

    public Task<UserAccount?> FindWithEmployeeAsync(int id, CancellationToken cancellationToken = default) =>
        Query.Include(account => account.Employee).SingleOrDefaultAsync(account => account.Id == id, cancellationToken);

    public Task<UserAccount?> FindForAuthenticationAsync(string identifier, CancellationToken cancellationToken = default) =>
        Query.Include(account => account.Role).Include(account => account.Customer).Include(account => account.Employee)
            .SingleOrDefaultAsync(account => account.UserName == identifier || account.Email == identifier, cancellationToken);

    public Task<UserAccount?> FindCurrentUserAsync(int id, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Include(account => account.Role).Include(account => account.Customer).Include(account => account.Employee)
            .SingleOrDefaultAsync(account => account.Id == id, cancellationToken);

    public Task<UserAccount?> FindForTokenValidationAsync(int id, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Include(account => account.Role).Include(account => account.Employee)
            .SingleOrDefaultAsync(account => account.Id == id, cancellationToken);

    public Task<AccountAuditDetails?> FindAuditDetailsByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Where(account => account.Id == id).Select(account => new AccountAuditDetails(
            account.Id, account.UserName, account.Role.Name,
            account.Employee == null ? null : account.Employee.Id,
            account.Employee == null ? null : (account.Employee.LastName + " " + account.Employee.FirstName).Trim(),
            account.Customer == null ? null : account.Customer.Id,
            account.Customer == null ? null : account.Customer.FullName)).SingleOrDefaultAsync(cancellationToken);

    public Task<AccountAuditDetails?> FindAuditDetailsByIdentifierAsync(string identifier, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Where(account => account.UserName == identifier || account.Email == identifier)
            .Select(account => new AccountAuditDetails(
                account.Id, account.UserName, account.Role.Name,
                account.Employee == null ? null : account.Employee.Id,
                account.Employee == null ? null : (account.Employee.LastName + " " + account.Employee.FirstName).Trim(),
                account.Customer == null ? null : account.Customer.Id,
                account.Customer == null ? null : account.Customer.FullName)).SingleOrDefaultAsync(cancellationToken);

    public Task<int> CountByRoleIdAsync(int roleId, CancellationToken cancellationToken = default) =>
        Query.CountAsync(account => account.RoleId == roleId, cancellationToken);

    public Task<bool> ExistsForDifferentAccountAsync(int accountId, string userName, string email, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(account => account.Id != accountId && (account.UserName == userName || account.Email == email), cancellationToken);
}

public sealed record AccountAuditDetails(int Id, string UserName, string RoleName, string? EmployeeCode,
    string? EmployeeName, string? CustomerCode, string? CustomerName);
