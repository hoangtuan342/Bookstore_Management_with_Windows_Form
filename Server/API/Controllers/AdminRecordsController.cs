using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace TrangSach.API.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public sealed class AdminRecordsController(AdminRecordsService service) : ControllerBase
{
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpGet("categories")]
    public IReadOnlyList<ManagerCategoryDto> Categories() => service.GetAllCategoriesList();
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPost("categories")]
    public IActionResult CreateCategory(CategoryRequest r)
    {
        var result = service.CreateCategory(new(r.Id, r.Name));
        return result.Status switch { OperationStatus.Created => Created($"api/admin/categories/{result.Value}", new { Id = result.Value, Name = r.Name.Trim() }),
            OperationStatus.Conflict => Conflict(new { message = result.Message }), OperationStatus.Invalid => BadRequest(new { message = result.Message }), _ => StatusCode(500) };
    }
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPut("categories/{id}")]
    public IActionResult UpdateCategory(string id, CategoryRequest r) => Result(service.UpdateCategory(id, new(r.Id, r.Name)));
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpDelete("categories/{id}")]
    public IActionResult DeleteCategory(string id) => Result(service.DeleteCategory(id));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpGet("authors")]
    public IReadOnlyList<ManagerAuthorDto> Authors() => service.GetAllAuthorsList();
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPost("authors")]
    public IActionResult CreateAuthor(AuthorRequest r)
    {
        var result = service.CreateAuthor(new(r.Name, r.Biography));
        return result.Status switch { OperationStatus.Created => Created($"api/admin/authors/{result.Value}", new { Id = result.Value, Name = r.Name.Trim(), Biography = r.Biography?.Trim() }),
            OperationStatus.Invalid => BadRequest(new { message = result.Message }), _ => StatusCode(500) };
    }
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPut("authors/{id:int}")]
    public IActionResult UpdateAuthor(int id, AuthorRequest r) => Result(service.UpdateAuthor(id, new(r.Name, r.Biography)));
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpDelete("authors/{id:int}")]
    public IActionResult DeleteAuthor(int id) => Result(service.DeleteAuthor(id));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpGet("publishers")]
    public IReadOnlyList<ManagerPublisherDto> Publishers() => service.GetAllPublishersList();
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPost("publishers")]
    public IActionResult CreatePublisher(PublisherRequest r)
    {
        var result = service.CreatePublisher(new(r.Name, r.Address));
        return result.Status switch { OperationStatus.Created => Created($"api/admin/publishers/{result.Value}", new { Id = result.Value, Name = r.Name.Trim(), Address = r.Address?.Trim() }),
            OperationStatus.Invalid => BadRequest(new { message = result.Message }), _ => StatusCode(500) };
    }
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPut("publishers/{id:int}")]
    public IActionResult UpdatePublisher(int id, PublisherRequest r) => Result(service.UpdatePublisher(id, new(r.Name, r.Address)));
    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpDelete("publishers/{id:int}")]
    public IActionResult DeletePublisher(int id) => Result(service.DeletePublisher(id));

    [Authorize(Roles = StaffRoles.ManagerOrSales), HttpGet("customers")]
    public IReadOnlyList<ManagerCustomerDto> Customers() => service.GetAllCustomersList();
    [Authorize(Roles = StaffRoles.ManagerOrSales), HttpPost("customers")]
    public IActionResult CreateCustomer(CustomerRequest r) => CreatedAtRouteResult(service.CreateCustomer(r.ToModel()), "api/admin/customers");
    [Authorize(Roles = StaffRoles.ManagerOrSales), HttpPut("customers/{id}")]
    public IActionResult UpdateCustomer(string id, CustomerRequest r) => Result(service.UpdateCustomer(id, r.ToModel()));
    [Authorize(Roles = StaffRoles.ManagerOrSales), HttpDelete("customers/{id}")]
    public IActionResult DeleteCustomer(string id) => Result(service.DeleteCustomer(id));

    [Authorize(Roles = StaffRoles.Manager), HttpGet("employees")]
    public IReadOnlyList<ManagerEmployeeDto> Employees() => service.GetAllEmployeesList();
    [Authorize(Roles = StaffRoles.Manager), HttpGet("employee-roles")]
    public IReadOnlyList<StaffRoleOptionDto> EmployeeRoles() => service.GetAllEmployeeRolesList();
    [Authorize(Roles = StaffRoles.Manager), HttpPost("employees")]
    public IActionResult CreateEmployee(EmployeeRequest r) => CreatedAtRouteResult(service.CreateEmployee(r.ToModel()), "api/admin/employees");
    [Authorize(Roles = StaffRoles.Manager), HttpPut("employees/{id}")]
    public IActionResult UpdateEmployee(string id, EmployeeRequest r) => Result(service.UpdateEmployee(id, r.ToModel(), GetAccountId()));
    [Authorize(Roles = StaffRoles.Manager), HttpDelete("employees/{id}")]
    public IActionResult DeleteEmployee(string id) => Result(service.DeleteEmployee(id, GetAccountId()));

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
    public SaveCustomerDto ToModel() => new(Id, FullName, PhoneNumber, Address, UserName, Email, Password, AccountStatus);
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
    public SaveEmployeeDto ToModel() => new(Id, LastName, FirstName, BirthDate, AvatarPath, Address, AccountStatus, BaseSalary, UserName, Email, Password, AccountLoginStatus, RoleName);
}
