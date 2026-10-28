using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using BookStore.Domain.Entities;
using BookStore.Shared.Models;
using BookStore.Shared.Security;
using TrangSach.BUS.Results;
using TrangSach.DAO.Entities;

namespace TrangSach.BUS.Services;

public sealed class AdminRecordsService(BookStoreDao dao, IPasswordHashingService passwordHasher)
{
    public async Task<IReadOnlyList<ManagerCategory>> GetCategoriesAsync(CancellationToken ct = default) =>
        (await dao.Categories.GetAllAsync(ct)).OrderBy(x => x.Name).Select(x => new ManagerCategory(x.Id, x.Name)).ToArray();
    public async Task<OperationResult<string>> CreateCategoryAsync(SaveCategory command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Id) || command.Id.Trim().Length > 20 || string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length is < 2 or > 120)
            return OperationResult<string>.Invalid("Mã hoặc tên loại sách không hợp lệ.");
        var id = command.Id.Trim();
        if (await dao.Categories.ExistsAsync(x => x.Id == id, ct)) return OperationResult<string>.Conflict("Mã loại sách đã tồn tại.");
        var item = new BookCategory { Id = id, Name = command.Name.Trim() }; dao.Categories.Add(item); await dao.SaveChangesAsync(ct);
        return OperationResult<string>.Created(item.Id);
    }
    public async Task<OperationResult<Unit>> UpdateCategoryAsync(string id, SaveCategory command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Id) || command.Id.Trim().Length > 20 || string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length is < 2 or > 120)
            return OperationResult<Unit>.Invalid("Mã hoặc tên loại sách không hợp lệ.");
        var item = await dao.Categories.FindAsync([id], ct); if (item is null) return OperationResult<Unit>.NotFound();
        if (!id.Equals(command.Id.Trim(), StringComparison.OrdinalIgnoreCase)) return OperationResult<Unit>.Invalid("Không thể đổi mã loại sách sau khi tạo.");
        item.Name = command.Name.Trim(); await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }
    public async Task<OperationResult<Unit>> DeleteCategoryAsync(string id, CancellationToken ct = default)
    {
        var item = await dao.Categories.FindAsync([id], ct); if (item is null) return OperationResult<Unit>.NotFound();
        if (await dao.Books.HasCategoryAsync(id, ct)) return OperationResult<Unit>.Conflict("Không thể xóa loại sách đang được sách sử dụng.");
        dao.Categories.Remove(item); await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<IReadOnlyList<ManagerAuthor>> GetAuthorsAsync(CancellationToken ct = default) =>
        (await dao.Authors.GetAllAsync(ct)).OrderBy(x => x.Name).Select(x => new ManagerAuthor(x.Id, x.Name, x.Biography)).ToArray();
    public async Task<OperationResult<int>> CreateAuthorAsync(SaveAuthor command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length is < 2 or > 180) return OperationResult<int>.Invalid("Tên tác giả cần từ 2 đến 180 ký tự.");
        var item = new Author { Name = command.Name.Trim(), Biography = command.Biography?.Trim() }; dao.Authors.Add(item); await dao.SaveChangesAsync(ct);
        return OperationResult<int>.Created(item.Id);
    }
    public async Task<OperationResult<Unit>> UpdateAuthorAsync(int id, SaveAuthor command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length is < 2 or > 180) return OperationResult<Unit>.Invalid("Tên tác giả cần từ 2 đến 180 ký tự.");
        var item = await dao.Authors.FindAsync([id], ct); if (item is null) return OperationResult<Unit>.NotFound();
        item.Name = command.Name.Trim(); item.Biography = command.Biography?.Trim(); await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }
    public async Task<OperationResult<Unit>> DeleteAuthorAsync(int id, CancellationToken ct = default)
    {
        var item = await dao.Authors.FindAsync([id], ct); if (item is null) return OperationResult<Unit>.NotFound();
        if (await dao.BookAuthors.HasAuthorAsync(id, ct)) return OperationResult<Unit>.Conflict("Không thể xóa tác giả còn liên kết với sách.");
        dao.Authors.Remove(item); await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<IReadOnlyList<ManagerPublisher>> GetPublishersAsync(CancellationToken ct = default) =>
        (await dao.Publishers.GetAllAsync(ct)).OrderBy(x => x.Name).Select(x => new ManagerPublisher(x.Id, x.Name, x.Address)).ToArray();
    public async Task<OperationResult<int>> CreatePublisherAsync(SavePublisher command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length is < 2 or > 180 || command.Address?.Length > 300)
            return OperationResult<int>.Invalid("Tên hoặc địa chỉ nhà xuất bản không hợp lệ.");
        var item = new Publisher { Name = command.Name.Trim(), Address = command.Address?.Trim() }; dao.Publishers.Add(item); await dao.SaveChangesAsync(ct);
        return OperationResult<int>.Created(item.Id);
    }
    public async Task<OperationResult<Unit>> UpdatePublisherAsync(int id, SavePublisher command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length is < 2 or > 180 || command.Address?.Length > 300)
            return OperationResult<Unit>.Invalid("Tên hoặc địa chỉ nhà xuất bản không hợp lệ.");
        var item = await dao.Publishers.FindAsync([id], ct); if (item is null) return OperationResult<Unit>.NotFound();
        item.Name = command.Name.Trim(); item.Address = command.Address?.Trim(); await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }
    public async Task<OperationResult<Unit>> DeletePublisherAsync(int id, CancellationToken ct = default)
    {
        var item = await dao.Publishers.FindAsync([id], ct); if (item is null) return OperationResult<Unit>.NotFound();
        if (await dao.Books.HasPublisherAsync(id, ct)) return OperationResult<Unit>.Conflict("Không thể xóa nhà xuất bản đang được sách tham chiếu.");
        dao.Publishers.Remove(item); await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<IReadOnlyList<ManagerCustomer>> GetCustomersAsync(CancellationToken ct = default)
    {
        var rows = await dao.Customers.GetAllWithAccountsAsync(ct);
        return rows.Select(x => new ManagerCustomer(x.Id, x.FullName, x.PhoneNumber, x.ShippingAddress, x.UserAccountId,
            x.UserAccount?.UserName, x.UserAccount?.Email, x.UserAccount?.Status)).ToArray();
    }
    public async Task<OperationResult<string>> CreateCustomerAsync(SaveCustomer command, CancellationToken ct = default)
    {
        var validation = ValidateCustomer(command);
        if (validation is not null) return OperationResult<string>.Invalid(validation);
        var item = new Customer { Id = string.IsNullOrWhiteSpace(command.Id) ? $"KH{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}" : command.Id.Trim(),
            FullName = command.FullName.Trim(), PhoneNumber = command.PhoneNumber?.Trim(), ShippingAddress = command.Address?.Trim() };
        if (await dao.Customers.ExistsByIdAsync(item.Id, ct)) return OperationResult<string>.Conflict("Mã khách hàng đã tồn tại.");
        if (HasAccount(command.UserName, command.Email, command.Password))
        {
            var error = await ValidateNewAccountAsync(command.UserName, command.Email, command.Password, ct);
            if (error is not null) return OperationResult<string>.Conflict(error);
            var role = await dao.Roles.GetByNameAsync("Customer", ct);
            var account = new UserAccount { UserName = command.UserName!.Trim(), Email = command.Email!.Trim(), Status = command.AccountStatus ?? "Active", RoleId = role.Id };
            account.PasswordHash = passwordHasher.Hash(account, command.Password!); item.UserAccount = account;
        }
        dao.Customers.Add(item); await dao.SaveChangesAsync(ct); return OperationResult<string>.Created(item.Id);
    }
    public async Task<OperationResult<Unit>> UpdateCustomerAsync(string id, SaveCustomer command, CancellationToken ct = default)
    {
        var validation = ValidateCustomer(command);
        if (validation is not null) return OperationResult<Unit>.Invalid(validation);
        var item = await dao.Customers.FindWithAccountAsync(id, ct); if (item is null) return OperationResult<Unit>.NotFound();
        item.FullName = command.FullName.Trim(); item.PhoneNumber = command.PhoneNumber?.Trim(); item.ShippingAddress = command.Address?.Trim();
        if (item.UserAccount is not null)
        {
            var error = await ValidateExistingAccountAsync(item.UserAccount, command.UserName, command.Email, command.Password, ct);
            if (error is not null) return OperationResult<Unit>.Conflict(error);
            ApplyAccount(item.UserAccount, command.UserName, command.Email, command.Password, command.AccountStatus);
        }
        else if (HasAccount(command.UserName, command.Email, command.Password))
        {
            var error = await ValidateNewAccountAsync(command.UserName, command.Email, command.Password, ct);
            if (error is not null) return OperationResult<Unit>.Conflict(error);
            var role = await dao.Roles.GetByNameAsync("Customer", ct);
            var account = new UserAccount { UserName = command.UserName!.Trim(), Email = command.Email!.Trim(), Status = command.AccountStatus ?? "Active", RoleId = role.Id };
            account.PasswordHash = passwordHasher.Hash(account, command.Password!); item.UserAccount = account;
        }
        await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }
    public async Task<OperationResult<Unit>> DeleteCustomerAsync(string id, CancellationToken ct = default)
    {
        var item = await dao.Customers.FindWithAccountAsync(id, ct); if (item is null) return OperationResult<Unit>.NotFound();
        if (await dao.Orders.HasCustomerAsync(id, ct)) return OperationResult<Unit>.Conflict("Khách hàng đã có đơn hàng; hãy khóa tài khoản để giữ lịch sử.");
        dao.Customers.Remove(item); if (item.UserAccount is not null) dao.Accounts.Remove(item.UserAccount);
        await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<IReadOnlyList<ManagerEmployee>> GetEmployeesAsync(CancellationToken ct = default)
    {
        var rows = await dao.Employees.GetAllWithAccountsAsync(ct);
        return rows.Select(x => new ManagerEmployee(x.Id, x.LastName, x.FirstName, x.BirthDate, x.AvatarPath, x.Address, x.AccountStatus,
            x.BaseSalary, x.UserAccountId, x.UserAccount?.UserName, x.UserAccount?.Email, x.UserAccount?.Status, x.UserAccount?.Role.Name)).ToArray();
    }
    public async Task<IReadOnlyList<StaffRoleOption>> GetEmployeeRolesAsync(CancellationToken ct = default)
    {
        var roles = (await dao.Roles.GetAllAsync(ct)).Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        return StaffPermissions.EmployeeRoles.Where(x => roles.Contains(x.Name)).Select(x => new StaffRoleOption(x.Name, x.Label)).ToArray();
    }
    public async Task<OperationResult<string>> CreateEmployeeAsync(SaveEmployee command, CancellationToken ct = default)
    {
        var validation = ValidateEmployee(command);
        if (validation is not null) return OperationResult<string>.Invalid(validation);
        var item = new Employee { Id = string.IsNullOrWhiteSpace(command.Id) ? $"NV{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}" : command.Id.Trim(),
            LastName = command.LastName.Trim(), FirstName = command.FirstName.Trim(), BirthDate = command.BirthDate, AvatarPath = command.AvatarPath?.Trim(),
            Address = command.Address?.Trim(), AccountStatus = command.AccountStatus, BaseSalary = command.BaseSalary };
        if (await dao.Employees.ExistsByIdAsync(item.Id, ct)) return OperationResult<string>.Conflict("Mã nhân viên đã tồn tại.");
        if (HasAccount(command.UserName, command.Email, command.Password))
        {
            var error = await ValidateNewAccountAsync(command.UserName, command.Email, command.Password, ct); if (error is not null) return OperationResult<string>.Conflict(error);
            var role = await dao.Roles.FindByNameAsync(command.RoleName, ct);
            if (!StaffPermissions.IsEmployeeRole(command.RoleName) || role is null) return OperationResult<string>.Invalid("Chức danh nhân viên không hợp lệ.");
            var account = new UserAccount { UserName = command.UserName!.Trim(), Email = command.Email!.Trim(),
                Status = command.AccountStatus == "Active" ? command.AccountLoginStatus ?? "Active" : "Disabled", RoleId = role.Id };
            account.PasswordHash = passwordHasher.Hash(account, command.Password!); item.UserAccount = account;
        }
        dao.Employees.Add(item); await dao.SaveChangesAsync(ct); return OperationResult<string>.Created(item.Id);
    }
    public async Task<OperationResult<Unit>> UpdateEmployeeAsync(string id, SaveEmployee command, int? currentAccountId, CancellationToken ct = default)
    {
        var validation = ValidateEmployee(command);
        if (validation is not null) return OperationResult<Unit>.Invalid(validation);
        var item = await dao.Employees.FindWithAccountAsync(id, ct); if (item is null) return OperationResult<Unit>.NotFound();
        if (item.UserAccountId == currentAccountId && currentAccountId is not null && (command.AccountStatus != "Active" || command.AccountLoginStatus == "Disabled"))
            return OperationResult<Unit>.Conflict("Không thể tự khóa tài khoản quản lý đang sử dụng.");
        item.LastName = command.LastName.Trim(); item.FirstName = command.FirstName.Trim(); item.BirthDate = command.BirthDate; item.AvatarPath = command.AvatarPath?.Trim();
        item.Address = command.Address?.Trim(); item.AccountStatus = command.AccountStatus; item.BaseSalary = command.BaseSalary;
        if (item.UserAccount is not null)
        {
            var error = await ValidateExistingAccountAsync(item.UserAccount, command.UserName, command.Email, command.Password, ct);
            if (error is not null) return OperationResult<Unit>.Conflict(error);
            if (!StaffPermissions.IsEmployeeRole(command.RoleName)) return OperationResult<Unit>.Invalid("Chức danh nhân viên không hợp lệ.");
            var role = await dao.Roles.FindByNameAsync(command.RoleName, ct); if (role is null) return OperationResult<Unit>.Invalid("Chức danh nhân viên không hợp lệ.");
            if (item.UserAccount.RoleId != role.Id)
            {
                var managerRoleId = (await dao.Roles.GetByNameAsync(StaffRoles.Manager, ct)).Id;
                if (item.UserAccount.RoleId == managerRoleId && await dao.Accounts.CountByRoleIdAsync(managerRoleId, ct) <= 1)
                    return OperationResult<Unit>.Conflict("Không thể hạ quyền tài khoản quản trị viên cuối cùng.");
                if (item.UserAccountId == currentAccountId) return OperationResult<Unit>.Conflict("Không thể tự thay đổi chức danh của tài khoản đang đăng nhập.");
                item.UserAccount.RoleId = role.Id;
            }
            ApplyAccount(item.UserAccount, command.UserName, command.Email, command.Password, command.AccountLoginStatus);
            if (item.AccountStatus != "Active") item.UserAccount.Status = "Disabled";
        }
        else if (HasAccount(command.UserName, command.Email, command.Password))
        {
            var error = await ValidateNewAccountAsync(command.UserName, command.Email, command.Password, ct); if (error is not null) return OperationResult<Unit>.Conflict(error);
            var role = await dao.Roles.FindByNameAsync(command.RoleName, ct);
            if (!StaffPermissions.IsEmployeeRole(command.RoleName) || role is null) return OperationResult<Unit>.Invalid("Chức danh nhân viên không hợp lệ.");
            var account = new UserAccount { UserName = command.UserName!.Trim(), Email = command.Email!.Trim(),
                Status = command.AccountStatus == "Active" ? command.AccountLoginStatus ?? "Active" : "Disabled", RoleId = role.Id };
            account.PasswordHash = passwordHasher.Hash(account, command.Password!); item.UserAccount = account;
        }
        await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }
    public async Task<OperationResult<Unit>> DeleteEmployeeAsync(string id, int? currentAccountId, CancellationToken ct = default)
    {
        var item = await dao.Employees.FindWithAccountAsync(id, ct); if (item is null) return OperationResult<Unit>.NotFound();
        if (currentAccountId is not null && item.UserAccountId == currentAccountId) return OperationResult<Unit>.Conflict("Không thể xóa hồ sơ và tài khoản quản lý đang sử dụng.");
        if (await dao.Employees.HasPurchaseReceiptsAsync(id, ct) || await dao.Orders.HasEmployeeAsync(id, ct))
            return OperationResult<Unit>.Conflict("Nhân viên đã được ghi nhận trong nghiệp vụ; hãy ngừng hoạt động để giữ lịch sử.");
        dao.Employees.Remove(item); if (item.UserAccount is not null) dao.Accounts.Remove(item.UserAccount);
        await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }

    private static bool HasAccount(string? userName, string? email, string? password) =>
        !string.IsNullOrWhiteSpace(userName) || !string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(password);
    private static string? ValidateCustomer(SaveCustomer x)
    {
        if (x.Id?.Length > 20 || string.IsNullOrWhiteSpace(x.FullName) || x.FullName.Trim().Length is < 2 or > 180
            || x.PhoneNumber?.Length > 30 || !string.IsNullOrWhiteSpace(x.PhoneNumber) && !new PhoneAttribute().IsValid(x.PhoneNumber)
            || x.Address?.Length > 500 || x.UserName?.Length > 100 || x.Email?.Length > 254 || x.Password?.Length > 100
            || !string.IsNullOrWhiteSpace(x.Email) && !new EmailAddressAttribute().IsValid(x.Email)
            || x.AccountStatus?.Length > 30)
            return "Thông tin khách hàng hoặc tài khoản không hợp lệ.";
        return null;
    }
    private static string? ValidateEmployee(SaveEmployee x)
    {
        if (x.Id?.Length > 20 || string.IsNullOrWhiteSpace(x.LastName) || x.LastName.Trim().Length > 100
            || string.IsNullOrWhiteSpace(x.FirstName) || x.FirstName.Trim().Length is < 1 or > 60
            || x.AvatarPath?.Length > 500 || x.Address?.Length > 300 || string.IsNullOrWhiteSpace(x.AccountStatus) || x.AccountStatus.Length > 30
            || x.BaseSalary is < 0 or > 999999999 || x.UserName?.Length > 100 || x.Email?.Length > 254 || x.Password?.Length > 100
            || !string.IsNullOrWhiteSpace(x.Email) && !new EmailAddressAttribute().IsValid(x.Email)
            || x.AccountLoginStatus?.Length > 30 || string.IsNullOrWhiteSpace(x.RoleName) || x.RoleName.Length > 60)
            return "Thông tin nhân viên hoặc tài khoản không hợp lệ.";
        return null;
    }
    private async Task<string?> ValidateNewAccountAsync(string? userName, string? email, string? password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return "Để tạo tài khoản cần nhập tên đăng nhập, email và mật khẩu.";
        if (await dao.Accounts.ExistsByUserNameOrEmailAsync(userName.Trim(), email.Trim(), ct)) return "Tên đăng nhập hoặc email đã được sử dụng.";
        return password.Length < 8 ? "Mật khẩu mới cần ít nhất 8 ký tự." : null;
    }
    private async Task<string?> ValidateExistingAccountAsync(UserAccount account, string? userName, string? email, string? password, CancellationToken ct)
    {
        if (await dao.Accounts.ExistsForDifferentAccountAsync(account.Id, userName?.Trim() ?? account.UserName, email?.Trim() ?? account.Email, ct))
            return "Tên đăng nhập hoặc email đã được sử dụng.";
        return !string.IsNullOrWhiteSpace(password) && password.Length < 8 ? "Mật khẩu mới cần ít nhất 8 ký tự." : null;
    }
    private void ApplyAccount(UserAccount account, string? userName, string? email, string? password, string? status)
    {
        account.UserName = userName?.Trim() ?? account.UserName; account.Email = email?.Trim() ?? account.Email; account.Status = status ?? account.Status;
        if (!string.IsNullOrWhiteSpace(password)) account.PasswordHash = passwordHasher.Hash(account, password);
    }
}
