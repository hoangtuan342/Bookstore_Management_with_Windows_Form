using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.BUS
{
    public class AuthenticationService
    {
        private readonly BookStoreDao dao;
        private readonly IPasswordHashingService passwordHasher;

        public AuthenticationService(BookStoreDao dao, IPasswordHashingService passwordHasher)
        {
            this.dao = dao;
            this.passwordHasher = passwordHasher;
        }

        public async Task<OperationResult<AuthenticatedAccountDto>> RegisterAsync(RegisterAccountCommandDto command, CancellationToken ct = default)
        {
            var userName = command.UserName.Trim();
            var email = command.Email.Trim();
            if (userName.Length < 3 || userName.Length > 80 || command.Password.Length < 8 || command.Password.Length > 100
                || command.FullName.Trim().Length < 2 || command.FullName.Trim().Length > 180 || !new EmailAddressAttribute().IsValid(email))
            {
                return OperationResult<AuthenticatedAccountDto>.Invalid("Tên đăng nhập, email, họ tên hoặc mật khẩu không hợp lệ.");
            }

            if (await dao.Users.ExistsUserNameOrEmailAsync(userName, email, null, ct))
            {
                return OperationResult<AuthenticatedAccountDto>.Conflict("Tên đăng nhập hoặc email đã được sử dụng.");
            }

            var roleId = await dao.Users.GetRoleIdByNameAsync("Customer", ct);
            if (!roleId.HasValue) return OperationResult<AuthenticatedAccountDto>.Invalid("Vai trò người dùng không tồn tại.");

            var passwordHash = passwordHasher.Hash(userName, command.Password);
            var accountId = await dao.Users.CreateAccountAsync(userName, email, passwordHash, roleId.Value, "Active", ct);

            var customerId = $"KH{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}";
            await dao.Users.InsertCustomerAsync(customerId, command.FullName.Trim(), command.PhoneNumber?.Trim(), null, accountId, ct);

            var dto = new AuthenticatedAccountDto(accountId, userName, email, "Customer", customerId);
            return OperationResult<AuthenticatedAccountDto>.Created(dto);
        }

        public async Task<OperationResult<AuthenticatedAccountDto>> SetupFirstManagerAsync(RegisterAccountCommandDto command, CancellationToken ct = default)
        {
            var userName = command.UserName.Trim();
            var email = command.Email.Trim();
            var fullName = command.FullName.Trim();
            if (userName.Length < 3 || userName.Length > 80 || command.Password.Length < 8 || command.Password.Length > 100
                || fullName.Length < 2 || fullName.Length > 180 || !new EmailAddressAttribute().IsValid(email))
            {
                return OperationResult<AuthenticatedAccountDto>.Invalid("Tên đăng nhập, email, họ tên hoặc mật khẩu không hợp lệ.");
            }

            if (await dao.Users.CountAccountsByRoleAsync(StaffRoles.Manager, ct) > 0)
            {
                return OperationResult<AuthenticatedAccountDto>.Conflict("Tài khoản quản lý ban đầu đã được tạo.");
            }

            if (await dao.Users.ExistsUserNameOrEmailAsync(userName, email, null, ct))
            {
                return OperationResult<AuthenticatedAccountDto>.Conflict("Tên đăng nhập hoặc email đã được sử dụng.");
            }

            var roleId = await dao.Users.GetRoleIdByNameAsync(StaffRoles.Manager, ct);
            if (!roleId.HasValue) return OperationResult<AuthenticatedAccountDto>.Invalid("Vai trò Quản lý không tồn tại.");

            var passwordHash = passwordHasher.Hash(userName, command.Password);
            var accountId = await dao.Users.CreateAccountAsync(userName, email, passwordHash, roleId.Value, "Active", ct);

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string lastName = string.Empty;
            string firstName = fullName;
            if (parts.Length > 1)
            {
                lastName = string.Join(' ', parts, 0, parts.Length - 1);
                firstName = parts[parts.Length - 1];
            }

            var employeeId = $"NV{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}";
            await dao.Users.InsertEmployeeAsync(employeeId, lastName, firstName, null, null, null, "Active", 0m, accountId, ct);

            var dto = new AuthenticatedAccountDto(accountId, userName, email, StaffRoles.Manager, null);
            return OperationResult<AuthenticatedAccountDto>.Created(dto);
        }

        public async Task<bool> HasManagerAccountAsync(CancellationToken ct = default)
        {
            return await dao.Users.CountAccountsByRoleAsync(StaffRoles.Manager, ct) > 0;
        }

        public async Task<OperationResult<AuthenticatedAccountDto>> LoginCustomerAsync(LoginCommandDto command, CancellationToken ct = default)
        {
            return await LoginAsync(command, StaffRoles.Customer, ct);
        }

        public async Task<OperationResult<AuthenticatedAccountDto>> LoginEmployeeAsync(LoginCommandDto command, CancellationToken ct = default)
        {
            var acc = await dao.Users.FindAccountWithDetailsAsync(command.UserNameOrEmail.Trim(), ct);
            if (acc == null || acc.Value.Status != "Active"
                || (acc.Value.EmployeeStatus != null && acc.Value.EmployeeStatus != "Active")
                || !StaffPermissions.IsEmployeeRole(acc.Value.RoleName)
                || !passwordHasher.Verify(acc.Value.PasswordHash, command.Password))
            {
                return OperationResult<AuthenticatedAccountDto>.Unauthorized("Thông tin đăng nhập không hợp lệ hoặc tài khoản không có quyền nhân viên.");
            }

            var dto = new AuthenticatedAccountDto(acc.Value.Id, acc.Value.UserName, acc.Value.Email, acc.Value.RoleName, null);
            return OperationResult<AuthenticatedAccountDto>.Success(dto);
        }

        public async Task<OperationResult<AccountProfileDto>> GetCurrentUserAsync(int accountId, CancellationToken ct = default)
        {
            var profile = await dao.Users.FindCurrentUserProfileAsync(accountId, ct);
            if (profile == null)
            {
                return OperationResult<AccountProfileDto>.Unauthorized();
            }
            return OperationResult<AccountProfileDto>.Success(profile);
        }

        public async Task<bool> IsTokenValidAsync(int accountId, string? tokenRole, CancellationToken ct = default)
        {
            var acc = await dao.Users.FindAccountForTokenValidationAsync(accountId, ct);
            return acc != null
                && acc.Value.Status == "Active"
                && (acc.Value.EmployeeStatus == null || acc.Value.EmployeeStatus == "Active")
                && string.Equals(acc.Value.RoleName, tokenRole, StringComparison.Ordinal);
        }

        private async Task<OperationResult<AuthenticatedAccountDto>> LoginAsync(LoginCommandDto command, string roleName, CancellationToken ct)
        {
            var acc = await dao.Users.FindAccountWithDetailsAsync(command.UserNameOrEmail.Trim(), ct);
            if (acc == null || acc.Value.Status != "Active"
                || acc.Value.RoleName != roleName
                || !passwordHasher.Verify(acc.Value.PasswordHash, command.Password))
            {
                return OperationResult<AuthenticatedAccountDto>.Unauthorized("Thông tin đăng nhập không hợp lệ cho khu vực khách hàng.");
            }

            var dto = new AuthenticatedAccountDto(acc.Value.Id, acc.Value.UserName, acc.Value.Email, acc.Value.RoleName, acc.Value.CustomerId);
            return OperationResult<AuthenticatedAccountDto>.Success(dto);
        }
    }
}
