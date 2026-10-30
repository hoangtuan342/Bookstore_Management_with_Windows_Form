using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BookStore.Shared.Models;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.BUS.Results;
using TrangSach.BUS.Services;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public sealed class AdminRecordsController(AdminRecordsService service) : ControllerBase
{
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpGet("categories")]
    public Task<IReadOnlyList<ManagerCategory>> Categories(CancellationToken ct) => service.GetCategoriesAsync(ct);
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CategoryRequest r, CancellationToken ct)
    {
        var result = await service.CreateCategoryAsync(new(r.Id, r.Name), ct);
        return result.Status switch { OperationStatus.Created => Created($"api/admin/categories/{result.Value}", new { Id = result.Value, Name = r.Name.Trim() }),
            OperationStatus.Conflict => Conflict(new { message = result.Message }), OperationStatus.Invalid => BadRequest(new { message = result.Message }), _ => StatusCode(500) };
    }
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(string id, CategoryRequest r, CancellationToken ct) => Result(await service.UpdateCategoryAsync(id, new(r.Id, r.Name), ct));
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(string id, CancellationToken ct) => Result(await service.DeleteCategoryAsync(id, ct));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpGet("authors")]
    public Task<IReadOnlyList<ManagerAuthor>> Authors(CancellationToken ct) => service.GetAuthorsAsync(ct);
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPost("authors")]
    public async Task<IActionResult> CreateAuthor(AuthorRequest r, CancellationToken ct)
    {
        var result = await service.CreateAuthorAsync(new(r.Name, r.Biography), ct);
        return result.Status switch { OperationStatus.Created => Created($"api/admin/authors/{result.Value}", new { Id = result.Value, Name = r.Name.Trim(), Biography = r.Biography?.Trim() }),
            OperationStatus.Invalid => BadRequest(new { message = result.Message }), _ => StatusCode(500) };
    }
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPut("authors/{id:int}")]
    public async Task<IActionResult> UpdateAuthor(int id, AuthorRequest r, CancellationToken ct) => Result(await service.UpdateAuthorAsync(id, new(r.Name, r.Biography), ct));
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpDelete("authors/{id:int}")]
    public async Task<IActionResult> DeleteAuthor(int id, CancellationToken ct) => Result(await service.DeleteAuthorAsync(id, ct));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpGet("publishers")]
    public Task<IReadOnlyList<ManagerPublisher>> Publishers(CancellationToken ct) => service.GetPublishersAsync(ct);
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPost("publishers")]
    public async Task<IActionResult> CreatePublisher(PublisherRequest r, CancellationToken ct)
    {
        var result = await service.CreatePublisherAsync(new(r.Name, r.Address), ct);
        return result.Status switch { OperationStatus.Created => Created($"api/admin/publishers/{result.Value}", new { Id = result.Value, Name = r.Name.Trim(), Address = r.Address?.Trim() }),
            OperationStatus.Invalid => BadRequest(new { message = result.Message }), _ => StatusCode(500) };
    }
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPut("publishers/{id:int}")]
    public async Task<IActionResult> UpdatePublisher(int id, PublisherRequest r, CancellationToken ct) => Result(await service.UpdatePublisherAsync(id, new(r.Name, r.Address), ct));
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpDelete("publishers/{id:int}")]
    public async Task<IActionResult> DeletePublisher(int id, CancellationToken ct) => Result(await service.DeletePublisherAsync(id, ct));

    [Authorize(Roles = StaffRoles.ManagerOrSales), HttpGet("customers")]
    public Task<IReadOnlyList<ManagerCustomer>> Customers(CancellationToken ct) => service.GetCustomersAsync(ct);
    [Authorize(Roles = StaffRoles.ManagerOrSales), HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer(CustomerRequest r, CancellationToken ct) => CreatedAtRouteResult(await service.CreateCustomerAsync(r.ToModel(), ct), "api/admin/customers");
    [Authorize(Roles = StaffRoles.ManagerOrSales), HttpPut("customers/{id}")]
    public async Task<IActionResult> UpdateCustomer(string id, CustomerRequest r, CancellationToken ct) => Result(await service.UpdateCustomerAsync(id, r.ToModel(), ct));
    [Authorize(Roles = StaffRoles.ManagerOrSales), HttpDelete("customers/{id}")]
    public async Task<IActionResult> DeleteCustomer(string id, CancellationToken ct) => Result(await service.DeleteCustomerAsync(id, ct));

    [Authorize(Roles = StaffRoles.Manager), HttpGet("employees")]
    public Task<IReadOnlyList<ManagerEmployee>> Employees(CancellationToken ct) => service.GetEmployeesAsync(ct);
    [Authorize(Roles = StaffRoles.Manager), HttpGet("employee-roles")]
    public Task<IReadOnlyList<StaffRoleOption>> EmployeeRoles(CancellationToken ct) => service.GetEmployeeRolesAsync(ct);
    [Authorize(Roles = StaffRoles.Manager), HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee(EmployeeRequest r, CancellationToken ct) => CreatedAtRouteResult(await service.CreateEmployeeAsync(r.ToModel(), ct), "api/admin/employees");
    [Authorize(Roles = StaffRoles.Manager), HttpPut("employees/{id}")]
    public async Task<IActionResult> UpdateEmployee(string id, EmployeeRequest r, CancellationToken ct) => Result(await service.UpdateEmployeeAsync(id, r.ToModel(), GetAccountId(), ct));
    [Authorize(Roles = StaffRoles.Manager), HttpDelete("employees/{id}")]
    public async Task<IActionResult> DeleteEmployee(string id, CancellationToken ct) => Result(await service.DeleteEmployeeAsync(id, GetAccountId(), ct));

    private int? GetAccountId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : null;
    private IActionResult CreatedAtRouteResult<T>(OperationResult<T> result, string route) => result.Status switch
    {
        OperationStatus.Created => Created($"{route}/{result.Value}", new { Id = result.Value }),
        OperationStatus.Conflict => Conflict(new { message = result.Message }),
        OperationStatus.Invalid => BadRequest(new { message = result.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
    private IActionResult Result(OperationResult<Unit> result) => result.Status switch
    {
        OperationStatus.Success => NoContent(), OperationStatus.NotFound => NotFound(),
        OperationStatus.Conflict => Conflict(new { message = result.Message }),
        OperationStatus.Invalid => BadRequest(new { message = result.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}

public sealed class CategoryRequest { [Required, StringLength(20, MinimumLength = 1)] public string Id { get; init; } = ""; [Required, StringLength(120, MinimumLength = 2)] public string Name { get; init; } = ""; }
public sealed class AuthorRequest { [Required, StringLength(180, MinimumLength = 2)] public string Name { get; init; } = ""; public string? Biography { get; init; } }
public sealed class PublisherRequest { [Required, StringLength(180, MinimumLength = 2)] public string Name { get; init; } = ""; [StringLength(300)] public string? Address { get; init; } }
public sealed class CustomerRequest
{
    [StringLength(20)] public string? Id { get; init; }
    [Required, StringLength(180, MinimumLength = 2)] public string FullName { get; init; } = "";
    [Phone, StringLength(30)] public string? PhoneNumber { get; init; }
    [StringLength(500)] public string? Address { get; init; }
    [StringLength(100)] public string? UserName { get; init; }
    [EmailAddress, StringLength(254)] public string? Email { get; init; }
    [StringLength(100)] public string? Password { get; init; }
    [StringLength(30)] public string? AccountStatus { get; init; }
    public SaveCustomer ToModel() => new(Id, FullName, PhoneNumber, Address, UserName, Email, Password, AccountStatus);
}
public sealed class EmployeeRequest
{
    [StringLength(20)] public string? Id { get; init; }
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
    public SaveEmployee ToModel() => new(Id, LastName, FirstName, BirthDate, AvatarPath, Address, AccountStatus, BaseSalary, UserName, Email, Password, AccountLoginStatus, RoleName);
}
