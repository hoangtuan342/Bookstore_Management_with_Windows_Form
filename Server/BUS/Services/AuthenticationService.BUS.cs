using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
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

        public OperationResult<AuthenticatedAccountDto> Register(RegisterAccountCommandDto command)
        {
            var userName = command.UserName.Trim();
            var email = command.Email.Trim();
            if (userName.Length < 3 || userName.Length > 80 || command.Password.Length < 8 || command.Password.Length > 100
                || command.FullName.Trim().Length < 2 || command.FullName.Trim().Length > 180 || !new EmailAddressAttribute().IsValid(email))
            {
                return OperationResult<AuthenticatedAccountDto>.Invalid("Tên đăng nhập, email, họ tên hoặc mật khẩu không hợp lệ.");
            }

            if (dao.Users.ExistsUserNameOrEmail(userName, email, null))
            {
                return OperationResult<AuthenticatedAccountDto>.Conflict("Tên đăng nhập hoặc email đã được sử dụng.");
            }

            var roleId = dao.Users.GetRoleIdByName("Customer");
            if (!roleId.HasValue) return OperationResult<AuthenticatedAccountDto>.Invalid("Vai trò người dùng không tồn tại.");

            var passwordHash = passwordHasher.Hash(userName, command.Password);
            var accountId = dao.Users.CreateAccount(userName, email, passwordHash, roleId.Value, "Active");

            var customerId = $"KH{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}";
            dao.Users.InsertCustomer(customerId, command.FullName.Trim(), command.PhoneNumber?.Trim(), null, accountId);

            var dto = new AuthenticatedAccountDto(accountId, userName, email, "Customer", customerId);
            return OperationResult<AuthenticatedAccountDto>.Created(dto);
        }

        public OperationResult<AuthenticatedAccountDto> SetupFirstManager(RegisterAccountCommandDto command)
        {
            var userName = command.UserName.Trim();
            var email = command.Email.Trim();
            var fullName = command.FullName.Trim();
            if (userName.Length < 3 || userName.Length > 80 || command.Password.Length < 8 || command.Password.Length > 100
                || fullName.Length < 2 || fullName.Length > 180 || !new EmailAddressAttribute().IsValid(email))
            {
                return OperationResult<AuthenticatedAccountDto>.Invalid("Tên đăng nhập, email, họ tên hoặc mật khẩu không hợp lệ.");
            }

            if (dao.Users.CountAccountsByRole(StaffRoles.Manager) > 0)
            {
                return OperationResult<AuthenticatedAccountDto>.Conflict("Tài khoản quản lý ban đầu đã được tạo.");
            }

            if (dao.Users.ExistsUserNameOrEmail(userName, email, null))
            {
                return OperationResult<AuthenticatedAccountDto>.Conflict("Tên đăng nhập hoặc email đã được sử dụng.");
            }

            var roleId = dao.Users.GetRoleIdByName(StaffRoles.Manager);
            if (!roleId.HasValue) return OperationResult<AuthenticatedAccountDto>.Invalid("Vai trò Quản lý không tồn tại.");

            var passwordHash = passwordHasher.Hash(userName, command.Password);
            var accountId = dao.Users.CreateAccount(userName, email, passwordHash, roleId.Value, "Active");

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string lastName = string.Empty;
            string firstName = fullName;
            if (parts.Length > 1)
            {
                lastName = string.Join(' ', parts, 0, parts.Length - 1);
                firstName = parts[parts.Length - 1];
            }

            var employeeId = $"NV{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}";
            dao.Users.InsertEmployee(employeeId, lastName, firstName, null, null, null, "Active", 0m, accountId);

            var dto = new AuthenticatedAccountDto(accountId, userName, email, StaffRoles.Manager, null);
            return OperationResult<AuthenticatedAccountDto>.Created(dto);
        }

        public bool HasManagerAccount()
        {
            return dao.Users.CountAccountsByRole(StaffRoles.Manager) > 0;
        }

        public OperationResult<AuthenticatedAccountDto> LoginCustomer(LoginCommandDto command)
        {
            return Login(command, StaffRoles.Customer);
        }

        public OperationResult<AuthenticatedAccountDto> LoginEmployee(LoginCommandDto command)
        {
            var acc = dao.Users.FindAccountWithDetails(command.UserNameOrEmail.Trim());
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

        public OperationResult<AccountProfileDto> GetCurrentUser(int accountId)
        {
            var profile = dao.Users.FindCurrentUserProfile(accountId);
            if (profile == null)
            {
                return OperationResult<AccountProfileDto>.Unauthorized();
            }
            return OperationResult<AccountProfileDto>.Success(profile);
        }

        public bool IsTokenValid(int accountId, string? tokenRole)
        {
            var acc = dao.Users.FindAccountForTokenValidation(accountId);
            return acc != null
                && acc.Value.Status == "Active"
                && (acc.Value.EmployeeStatus == null || acc.Value.EmployeeStatus == "Active")
                && string.Equals(acc.Value.RoleName, tokenRole, StringComparison.Ordinal);
        }

        private OperationResult<AuthenticatedAccountDto> Login(LoginCommandDto command, string roleName)
        {
            var acc = dao.Users.FindAccountWithDetails(command.UserNameOrEmail.Trim());
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
