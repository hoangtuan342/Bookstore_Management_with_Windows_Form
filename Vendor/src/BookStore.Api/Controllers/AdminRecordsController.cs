using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public sealed class AdminRecordsController(BookStoreDbContext db, IPasswordHasher<UserAccount> passwordHasher) : ControllerBase
{
    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken ct) => Ok(await db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CategoryRequest request, CancellationToken ct)
    {
        var item = new BookCategory { Id = request.Id.Trim(), Name = request.Name.Trim() };
        if (await db.Categories.AnyAsync(x => x.Id == item.Id, ct)) return Conflict(new { message = "Mã loại sách đã tồn tại." });
        db.Categories.Add(item); await db.SaveChangesAsync(ct); return Created($"api/admin/categories/{item.Id}", item);
    }

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(string id, CategoryRequest request, CancellationToken ct)
    {
        var item = await db.Categories.FindAsync([id], ct); if (item is null) return NotFound();
        if (!id.Equals(request.Id.Trim(), StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "Không thể đổi mã loại sách sau khi tạo." });
        item.Name = request.Name.Trim(); await db.SaveChangesAsync(ct); return NoContent();
    }

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(string id, CancellationToken ct)
    {
        var item = await db.Categories.FindAsync([id], ct); if (item is null) return NotFound();
        if (await db.Books.AnyAsync(x => x.CategoryId == id, ct)) return Conflict(new { message = "Không thể xóa loại sách đang được sách sử dụng." });
        db.Categories.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpGet("authors")]
    public async Task<IActionResult> Authors(CancellationToken ct) => Ok(await db.Authors.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPost("authors")]
    public async Task<IActionResult> CreateAuthor(AuthorRequest request, CancellationToken ct)
    {
        var item = new Author { Name = request.Name.Trim(), Biography = request.Biography?.Trim() };
        db.Authors.Add(item); await db.SaveChangesAsync(ct); return Created($"api/admin/authors/{item.Id}", item);
    }

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPut("authors/{id:int}")]
    public async Task<IActionResult> UpdateAuthor(int id, AuthorRequest request, CancellationToken ct)
    {
        var item = await db.Authors.FindAsync([id], ct); if (item is null) return NotFound();
        item.Name = request.Name.Trim(); item.Biography = request.Biography?.Trim(); await db.SaveChangesAsync(ct); return NoContent();
    }

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpDelete("authors/{id:int}")]
    public async Task<IActionResult> DeleteAuthor(int id, CancellationToken ct)
    {
        var item = await db.Authors.FindAsync([id], ct); if (item is null) return NotFound();
        if (await db.BookAuthors.AnyAsync(x => x.AuthorId == id, ct)) return Conflict(new { message = "Không thể xóa tác giả còn liên kết với sách." });
        db.Authors.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpGet("publishers")]
    public async Task<IActionResult> Publishers(CancellationToken ct) => Ok(await db.Publishers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPost("publishers")]
    public async Task<IActionResult> CreatePublisher(PublisherRequest request, CancellationToken ct)
    {
        var item = new Publisher { Name = request.Name.Trim(), Address = request.Address?.Trim() };
        db.Publishers.Add(item); await db.SaveChangesAsync(ct); return Created($"api/admin/publishers/{item.Id}", item);
    }

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPut("publishers/{id:int}")]
    public async Task<IActionResult> UpdatePublisher(int id, PublisherRequest request, CancellationToken ct)
    {
        var item = await db.Publishers.FindAsync([id], ct); if (item is null) return NotFound();
        item.Name = request.Name.Trim(); item.Address = request.Address?.Trim(); await db.SaveChangesAsync(ct); return NoContent();
    }

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpDelete("publishers/{id:int}")]
    public async Task<IActionResult> DeletePublisher(int id, CancellationToken ct)
    {
        var item = await db.Publishers.FindAsync([id], ct); if (item is null) return NotFound();
        if (await db.Books.AnyAsync(x => x.PublisherId == id, ct)) return Conflict(new { message = "Không thể xóa nhà xuất bản đang được sách tham chiếu." });
        db.Publishers.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }

    [Authorize(Roles = StaffRoles.ManagerOrSales)]
    [HttpGet("customers")]
    public async Task<IActionResult> Customers(CancellationToken ct) => Ok(await db.Customers.AsNoTracking().Include(x => x.UserAccount)
        .OrderBy(x => x.FullName).Select(x => new CustomerRow(x.Id, x.FullName, x.PhoneNumber, x.ShippingAddress,
            x.UserAccountId, x.UserAccount == null ? null : x.UserAccount.UserName, x.UserAccount == null ? null : x.UserAccount.Email,
            x.UserAccount == null ? null : x.UserAccount.Status)).ToListAsync(ct));

    [Authorize(Roles = StaffRoles.ManagerOrSales)]
    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer(CustomerRequest request, CancellationToken ct)
    {
        var item = new Customer { Id = string.IsNullOrWhiteSpace(request.Id) ? $"KH{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}" : request.Id.Trim(), FullName = request.FullName.Trim(), PhoneNumber = request.PhoneNumber?.Trim(), ShippingAddress = request.Address?.Trim() };
        if (await db.Customers.AnyAsync(x => x.Id == item.Id, ct)) return Conflict(new { message = "Mã khách hàng đã tồn tại." });
        if (request.HasAccount)
        {
            var error = await ValidateNewAccount(request.UserName!, request.Email!, request.Password!, ct); if (error is not null) return Conflict(new { message = error });
            var role = await db.Roles.SingleAsync(x => x.Name == "Customer", ct);
            var account = new UserAccount { UserName = request.UserName!.Trim(), Email = request.Email!.Trim(), Status = request.AccountStatus ?? "Active", RoleId = role.Id };
            account.PasswordHash = passwordHasher.HashPassword(account, request.Password!); item.UserAccount = account;
        }
        db.Customers.Add(item); await db.SaveChangesAsync(ct); return Created($"api/admin/customers/{item.Id}", new { item.Id });
    }

    [Authorize(Roles = StaffRoles.ManagerOrSales)]
    [HttpPut("customers/{id}")]
    public async Task<IActionResult> UpdateCustomer(string id, CustomerRequest request, CancellationToken ct)
    {
        var item = await db.Customers.Include(x => x.UserAccount).SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound();
        item.FullName = request.FullName.Trim(); item.PhoneNumber = request.PhoneNumber?.Trim(); item.ShippingAddress = request.Address?.Trim();
        if (item.UserAccount is not null)
        {
            var error = await ValidateExistingAccount(item.UserAccount, request, ct); if (error is not null) return Conflict(new { message = error });
            ApplyAccount(item.UserAccount, request);
        }
        else if (request.HasAccount)
        {
            var error = await ValidateNewAccount(request.UserName!, request.Email!, request.Password!, ct); if (error is not null) return Conflict(new { message = error });
            var role = await db.Roles.SingleAsync(x => x.Name == "Customer", ct);
            var account = new UserAccount { UserName = request.UserName!.Trim(), Email = request.Email!.Trim(), Status = request.AccountStatus ?? "Active", RoleId = role.Id };
            account.PasswordHash = passwordHasher.HashPassword(account, request.Password!); item.UserAccount = account;
        }
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [Authorize(Roles = StaffRoles.ManagerOrSales)]
    [HttpDelete("customers/{id}")]
    public async Task<IActionResult> DeleteCustomer(string id, CancellationToken ct)
    {
        var item = await db.Customers.Include(x => x.UserAccount).SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound();
        if (await db.Orders.AnyAsync(x => x.CustomerId == id, ct)) return Conflict(new { message = "Khách hàng đã có đơn hàng; hãy khóa tài khoản để giữ lịch sử." });
        db.Customers.Remove(item); if (item.UserAccount is not null) db.Accounts.Remove(item.UserAccount); await db.SaveChangesAsync(ct); return NoContent();
    }

    [Authorize(Roles = StaffRoles.Manager)]
    [HttpGet("employees")]
    public async Task<IActionResult> Employees(CancellationToken ct) => Ok(await db.Employees.AsNoTracking().Include(x => x.UserAccount)
        .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).Select(x => new EmployeeRow(x.Id, x.LastName, x.FirstName, x.BirthDate, x.AvatarPath, x.Address, x.AccountStatus, x.BaseSalary,
            x.UserAccountId, x.UserAccount == null ? null : x.UserAccount.UserName, x.UserAccount == null ? null : x.UserAccount.Email,
            x.UserAccount == null ? null : x.UserAccount.Status, x.UserAccount == null ? null : x.UserAccount.Role.Name)).ToListAsync(ct));

    [Authorize(Roles = StaffRoles.Manager)]
    [HttpGet("employee-roles")]
    public async Task<IActionResult> EmployeeRoles(CancellationToken ct)
    {
        var existingRoles = await db.Roles.AsNoTracking().Select(role => role.Name).ToListAsync(ct);
        return Ok(StaffPermissions.EmployeeRoles
            .Where(role => existingRoles.Contains(role.Name))
            .Select(role => new { role.Name, Label = role.Label }));
    }

    [Authorize(Roles = StaffRoles.Manager)]
    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee(EmployeeRequest request, CancellationToken ct)
    {
        var item = new Employee { Id = string.IsNullOrWhiteSpace(request.Id) ? $"NV{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}" : request.Id.Trim(), LastName = request.LastName.Trim(), FirstName = request.FirstName.Trim(), BirthDate = request.BirthDate, AvatarPath = request.AvatarPath?.Trim(), Address = request.Address?.Trim(), AccountStatus = request.AccountStatus, BaseSalary = request.BaseSalary };
        if (await db.Employees.AnyAsync(x => x.Id == item.Id, ct)) return Conflict(new { message = "Mã nhân viên đã tồn tại." });
        if (request.HasAccount)
        {
            var error = await ValidateNewAccount(request.UserName!, request.Email!, request.Password!, ct); if (error is not null) return Conflict(new { message = error });
            if (!StaffPermissions.IsEmployeeRole(request.RoleName)) return BadRequest(new { message = "Chức danh nhân viên không hợp lệ." });
            var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == request.RoleName, ct);
            if (role is null) return BadRequest(new { message = "Chức danh nhân viên không hợp lệ." });
            var account = new UserAccount { UserName = request.UserName!.Trim(), Email = request.Email!.Trim(), Status = request.AccountStatus == "Active" ? request.AccountLoginStatus ?? "Active" : "Disabled", RoleId = role.Id };
            account.PasswordHash = passwordHasher.HashPassword(account, request.Password!); item.UserAccount = account;
        }
        db.Employees.Add(item); await db.SaveChangesAsync(ct); return Created($"api/admin/employees/{item.Id}", new { item.Id });
    }

    [Authorize(Roles = StaffRoles.Manager)]
    [HttpPut("employees/{id}")]
    public async Task<IActionResult> UpdateEmployee(string id, EmployeeRequest request, CancellationToken ct)
    {
        var item = await db.Employees.Include(x => x.UserAccount).SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound();
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentAccountId)
            && item.UserAccountId == currentAccountId
            && (request.AccountStatus != "Active" || request.AccountLoginStatus == "Disabled"))
            return Conflict(new { message = "Không thể tự khóa tài khoản quản lý đang sử dụng." });
        item.LastName = request.LastName.Trim(); item.FirstName = request.FirstName.Trim(); item.BirthDate = request.BirthDate; item.AvatarPath = request.AvatarPath?.Trim(); item.Address = request.Address?.Trim(); item.AccountStatus = request.AccountStatus; item.BaseSalary = request.BaseSalary;
        if (item.UserAccount is not null)
        {
            var error = await ValidateExistingAccount(item.UserAccount, request, ct); if (error is not null) return Conflict(new { message = error });
            if (!StaffPermissions.IsEmployeeRole(request.RoleName)) return BadRequest(new { message = "Chức danh nhân viên không hợp lệ." });
            var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == request.RoleName, ct);
            if (role is null) return BadRequest(new { message = "Chức danh nhân viên không hợp lệ." });
            if (item.UserAccount.RoleId != role.Id)
            {
                var managerRoleId = await db.Roles.Where(x => x.Name == StaffRoles.Manager).Select(x => x.Id).SingleAsync(ct);
                if (item.UserAccount.RoleId == managerRoleId
                    && await db.Accounts.CountAsync(x => x.RoleId == managerRoleId, ct) <= 1)
                    return Conflict(new { message = "Không thể hạ quyền tài khoản quản trị viên cuối cùng." });
                if (item.UserAccountId == currentAccountId)
                    return Conflict(new { message = "Không thể tự thay đổi chức danh của tài khoản đang đăng nhập." });
                item.UserAccount.RoleId = role.Id;
            }
            ApplyAccount(item.UserAccount, request);
            if (item.AccountStatus != "Active") item.UserAccount.Status = "Disabled";
        }
        else if (request.HasAccount)
        {
            var error = await ValidateNewAccount(request.UserName!, request.Email!, request.Password!, ct); if (error is not null) return Conflict(new { message = error });
            if (!StaffPermissions.IsEmployeeRole(request.RoleName)) return BadRequest(new { message = "Chức danh nhân viên không hợp lệ." });
            var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == request.RoleName, ct);
            if (role is null) return BadRequest(new { message = "Chức danh nhân viên không hợp lệ." });
            var account = new UserAccount { UserName = request.UserName!.Trim(), Email = request.Email!.Trim(), Status = request.AccountStatus == "Active" ? request.AccountLoginStatus ?? "Active" : "Disabled", RoleId = role.Id };
            account.PasswordHash = passwordHasher.HashPassword(account, request.Password!); item.UserAccount = account;
        }
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [Authorize(Roles = StaffRoles.Manager)]
    [HttpDelete("employees/{id}")]
    public async Task<IActionResult> DeleteEmployee(string id, CancellationToken ct)
    {
        var item = await db.Employees.Include(x => x.UserAccount).SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound();
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentAccountId) && item.UserAccountId == currentAccountId)
            return Conflict(new { message = "Không thể xóa hồ sơ và tài khoản quản lý đang sử dụng." });
        if (await db.PurchaseReceipts.AnyAsync(x => x.EmployeeId == id, ct) || await db.Orders.AnyAsync(x => x.EmployeeId == id, ct)) return Conflict(new { message = "Nhân viên đã được ghi nhận trong nghiệp vụ; hãy ngừng hoạt động để giữ lịch sử." });
        db.Employees.Remove(item); if (item.UserAccount is not null) db.Accounts.Remove(item.UserAccount); await db.SaveChangesAsync(ct); return NoContent();
    }

    private async Task<string?> ValidateNewAccount(string userName, string email, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return "Để tạo tài khoản cần nhập tên đăng nhập, email và mật khẩu.";
        if (await db.Accounts.AnyAsync(x => x.UserName == userName.Trim() || x.Email == email.Trim(), ct)) return "Tên đăng nhập hoặc email đã được sử dụng.";
        return string.IsNullOrWhiteSpace(password) || password.Length < 8 ? "Mật khẩu mới cần ít nhất 8 ký tự." : null;
    }

    private async Task<string?> ValidateExistingAccount(UserAccount account, AccountFields request, CancellationToken ct)
    {
        var userName = request.UserName?.Trim() ?? account.UserName;
        var email = request.Email?.Trim() ?? account.Email;
        if (await db.Accounts.AnyAsync(x => x.Id != account.Id && (x.UserName == userName || x.Email == email), ct)) return "Tên đăng nhập hoặc email đã được sử dụng.";
        return !string.IsNullOrWhiteSpace(request.Password) && request.Password.Length < 8 ? "Mật khẩu mới cần ít nhất 8 ký tự." : null;
    }

    private void ApplyAccount(UserAccount account, AccountFields request)
    {
        account.UserName = request.UserName?.Trim() ?? account.UserName; account.Email = request.Email?.Trim() ?? account.Email; account.Status = request.AccountStatus ?? account.Status;
        if (!string.IsNullOrWhiteSpace(request.Password)) account.PasswordHash = passwordHasher.HashPassword(account, request.Password);
    }
}

public interface AccountFields
{
    string? UserName { get; }
    string? Email { get; }
    string? Password { get; }
    string? AccountStatus { get; }
}

public sealed class CategoryRequest { [Required, StringLength(20, MinimumLength = 1)] public string Id { get; init; } = ""; [Required, StringLength(120, MinimumLength = 2)] public string Name { get; init; } = ""; }
public sealed class AuthorRequest { [Required, StringLength(180, MinimumLength = 2)] public string Name { get; init; } = ""; public string? Biography { get; init; } }
public sealed class PublisherRequest { [Required, StringLength(180, MinimumLength = 2)] public string Name { get; init; } = ""; [StringLength(300)] public string? Address { get; init; } }
public sealed record CustomerRow(string Id, string FullName, string? PhoneNumber, string? Address, int? AccountId, string? UserName, string? Email, string? AccountStatus);
public sealed record EmployeeRow(string Id, string LastName, string FirstName, DateOnly? BirthDate, string? AvatarPath, string? Address, string AccountStatus, decimal BaseSalary, int? AccountId, string? UserName, string? Email, string? LoginStatus, string? RoleName);
public sealed class CustomerRequest : AccountFields
{
    [StringLength(20)]
    public string? Id { get; init; }
    [Required, StringLength(180, MinimumLength = 2)] public string FullName { get; init; } = "";
    [Phone, StringLength(30)] public string? PhoneNumber { get; init; }
    [StringLength(500)] public string? Address { get; init; }
    [StringLength(100)] public string? UserName { get; init; }
    [EmailAddress, StringLength(254)] public string? Email { get; init; }
    [StringLength(100)] public string? Password { get; init; }
    [StringLength(30)] public string? AccountStatus { get; init; }
    public bool HasAccount => !string.IsNullOrWhiteSpace(UserName) || !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(Password);
}
public sealed class EmployeeRequest : AccountFields
{
    [StringLength(20)]
    public string? Id { get; init; }
    [Required, StringLength(100)] public string LastName { get; init; } = "";
    [Required, StringLength(60, MinimumLength = 1)] public string FirstName { get; init; } = "";
    public DateOnly? BirthDate { get; init; }
    [StringLength(500)] public string? AvatarPath { get; init; }
    [StringLength(300)] public string? Address { get; init; }
    [Required, StringLength(30)] public string AccountStatus { get; init; } = "Active";
    [Range(typeof(decimal), "0", "999999999")] public decimal BaseSalary { get; init; }
    [StringLength(100)] public string? UserName { get; init; }
    [EmailAddress, StringLength(254)] public string? Email { get; init; }
    [StringLength(100)] public string? Password { get; init; }
    [StringLength(30)] public string? AccountLoginStatus { get; init; }
    [StringLength(60)] public string RoleName { get; init; } = StaffRoles.Sales;
    string? AccountFields.AccountStatus => AccountLoginStatus;
    public bool HasAccount => !string.IsNullOrWhiteSpace(UserName) || !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(Password);
}
