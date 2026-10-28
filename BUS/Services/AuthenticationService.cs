using System.Data;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using BookStore.Domain.Entities;
using BookStore.Shared.Security;
using TrangSach.BUS.Models;
using TrangSach.BUS.Results;
using TrangSach.DAO.Entities;

namespace TrangSach.BUS.Services;

public sealed class AuthenticationService(BookStoreDao dao, IPasswordHashingService passwordHasher)
{
    public async Task<OperationResult<AuthenticatedAccount>> RegisterAsync(RegisterAccountCommand command, CancellationToken ct = default)
    {
        var userName = command.UserName.Trim(); var email = command.Email.Trim();
        if (userName.Length is < 3 or > 80 || command.Password.Length is < 8 or > 100
            || command.FullName.Trim().Length is < 2 or > 180 || !new EmailAddressAttribute().IsValid(email))
            return OperationResult<AuthenticatedAccount>.Invalid("Tên đăng nhập, email, họ tên hoặc mật khẩu không hợp lệ.");
        if (await dao.Accounts.ExistsByUserNameOrEmailAsync(userName, email, ct)) return OperationResult<AuthenticatedAccount>.Conflict("Tên đăng nhập hoặc email đã được sử dụng.");
        var role = await dao.Roles.GetByNameAsync("Customer", ct);
        var account = new UserAccount { UserName = userName, Email = email, Status = "Active", RoleId = role.Id };
        account.PasswordHash = passwordHasher.Hash(account, command.Password);
        var customer = new Customer { Id = $"KH{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}", FullName = command.FullName.Trim(), PhoneNumber = command.PhoneNumber?.Trim(), UserAccount = account };
        dao.Customers.Add(customer);
        try { await dao.SaveChangesAsync(ct); }
        catch (DaoUpdateException) { return OperationResult<AuthenticatedAccount>.Conflict("Tên đăng nhập hoặc email đã được sử dụng."); }
        return OperationResult<AuthenticatedAccount>.Created(ToAccount(account, customer.Id));
    }

    public async Task<OperationResult<AuthenticatedAccount>> SetupFirstManagerAsync(RegisterAccountCommand command, CancellationToken ct = default)
    {
        var userName = command.UserName.Trim();
        var email = command.Email.Trim();
        var fullName = command.FullName.Trim();
        if (userName.Length is < 3 or > 80 || command.Password.Length is < 8 or > 100
            || fullName.Length is < 2 or > 180 || !new EmailAddressAttribute().IsValid(email))
            return OperationResult<AuthenticatedAccount>.Invalid("Tên đăng nhập, email, họ tên hoặc mật khẩu không hợp lệ.");

        await using var tx = await dao.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var role = await dao.Roles.GetByNameAsync(StaffRoles.Manager, ct);
        if (await dao.Accounts.CountByRoleIdAsync(role.Id, ct) > 0) return OperationResult<AuthenticatedAccount>.Conflict("Tài khoản quản lý ban đầu đã được tạo.");
        if (await dao.Accounts.ExistsByUserNameOrEmailAsync(userName, email, ct)) return OperationResult<AuthenticatedAccount>.Conflict("Tên đăng nhập hoặc email đã được sử dụng.");
        var account = new UserAccount { UserName = userName, Email = email, Status = "Active", RoleId = role.Id };
        account.PasswordHash = passwordHasher.Hash(account, command.Password);
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var employee = new Employee { Id = $"NV{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}",
            LastName = parts.Length > 1 ? string.Join(' ', parts[..^1]) : string.Empty, FirstName = parts[^1], UserAccount = account };
        dao.Employees.Add(employee); await dao.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return OperationResult<AuthenticatedAccount>.Created(ToAccount(account, null));
    }

    public async Task<bool> HasManagerAccountAsync(CancellationToken ct = default)
    {
        var role = await dao.Roles.GetByNameAsync(StaffRoles.Manager, ct);
        return await dao.Accounts.CountByRoleIdAsync(role.Id, ct) > 0;
    }

    public async Task<OperationResult<AuthenticatedAccount>> LoginCustomerAsync(LoginCommand command, CancellationToken ct = default) =>
        await LoginAsync(command, StaffRoles.Customer, ct);

    public async Task<OperationResult<AuthenticatedAccount>> LoginEmployeeAsync(LoginCommand command, CancellationToken ct = default)
    {
        var account = await dao.Accounts.FindForAuthenticationAsync(command.UserNameOrEmail.Trim(), ct);
        if (!IsValidPassword(account, command.Password) || !StaffPermissions.IsEmployeeRole(account!.Role.Name))
            return OperationResult<AuthenticatedAccount>.Unauthorized("Thông tin đăng nhập không hợp lệ hoặc tài khoản không có quyền nhân viên.");
        return OperationResult<AuthenticatedAccount>.Success(ToAccount(account, null));
    }

    public async Task<OperationResult<AccountProfile>> GetCurrentUserAsync(int accountId, CancellationToken ct = default)
    {
        var account = await dao.Accounts.FindCurrentUserAsync(accountId, ct);
        if (account is null) return OperationResult<AccountProfile>.Unauthorized();
        var fullName = account.Customer?.FullName ?? (account.Employee is null ? account.UserName : $"{account.Employee.LastName} {account.Employee.FirstName}".Trim());
        return OperationResult<AccountProfile>.Success(new AccountProfile(account.UserName, account.Email, account.Role.Name,
            account.Customer?.Id ?? account.Employee?.Id ?? account.Id.ToString(), fullName,
            account.Customer?.PhoneNumber, account.Customer?.ShippingAddress ?? account.Employee?.Address));
    }

    public async Task<bool> IsTokenValidAsync(int accountId, string? tokenRole, CancellationToken ct = default)
    {
        var account = await dao.Accounts.FindForTokenValidationAsync(accountId, ct);
        return account is not null
            && account.Status == "Active"
            && (account.Employee is null || account.Employee.AccountStatus == "Active")
            && string.Equals(account.Role.Name, tokenRole, StringComparison.Ordinal);
    }

    private async Task<OperationResult<AuthenticatedAccount>> LoginAsync(LoginCommand command, string roleName, CancellationToken ct)
    {
        var account = await dao.Accounts.FindForAuthenticationAsync(command.UserNameOrEmail.Trim(), ct);
        if (!IsValidPassword(account, command.Password) || account!.Role.Name != roleName)
            return OperationResult<AuthenticatedAccount>.Unauthorized("Thông tin đăng nhập không hợp lệ cho khu vực khách hàng.");
        return OperationResult<AuthenticatedAccount>.Success(ToAccount(account, account.Customer?.Id));
    }
    private bool IsValidPassword(UserAccount? account, string password) => account is not null && account.Status == "Active"
        && (account.Employee is null || account.Employee.AccountStatus == "Active")
        && passwordHasher.Verify(account, password);
    private static AuthenticatedAccount ToAccount(UserAccount account, string? customerId) =>
        new(account.Id, account.UserName, account.Email, account.Role.Name, customerId);
}
