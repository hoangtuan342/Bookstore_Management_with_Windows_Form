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
    public List<ManagerCategoryDto> Categories() => service.GetAllCategoriesList();

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPost("categories")]
    public IActionResult CreateCategory(CategoryRequest r) =>
        ControllerExceptionHelper.Execute(() =>
        {
            var id = service.CreateCategory(new(r.Id, r.Name));
            return new { Id = id, Name = r.Name.Trim() };
        }, StatusCodes.Status201Created, $"api/admin/categories/{r.Id.Trim()}");

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPut("categories/{id}")]
    public IActionResult UpdateCategory(string id, CategoryRequest r) =>
        ControllerExceptionHelper.Execute(() => service.UpdateCategory(id, new(r.Id, r.Name)));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpDelete("categories/{id}")]
    public IActionResult DeleteCategory(string id) =>
        ControllerExceptionHelper.Execute(() => service.DeleteCategory(id));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpGet("authors")]
    public List<ManagerAuthorDto> Authors() => service.GetAllAuthorsList();

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPost("authors")]
    public IActionResult CreateAuthor(AuthorRequest r) =>
        ControllerExceptionHelper.Execute(() =>
        {
            var id = service.CreateAuthor(new(r.Name, r.Biography));
            return new { Id = id, Name = r.Name.Trim(), Biography = r.Biography?.Trim() };
        }, StatusCodes.Status201Created, $"api/admin/authors");

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPut("authors/{id:int}")]
    public IActionResult UpdateAuthor(int id, AuthorRequest r) =>
        ControllerExceptionHelper.Execute(() => service.UpdateAuthor(id, new(r.Name, r.Biography)));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpDelete("authors/{id:int}")]
    public IActionResult DeleteAuthor(int id) =>
        ControllerExceptionHelper.Execute(() => service.DeleteAuthor(id));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpGet("publishers")]
    public List<ManagerPublisherDto> Publishers() => service.GetAllPublishersList();

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPost("publishers")]
    public IActionResult CreatePublisher(PublisherRequest r) =>
        ControllerExceptionHelper.Execute(() =>
        {
            var id = service.CreatePublisher(new(r.Name, r.Address));
            return new { Id = id, Name = r.Name.Trim(), Address = r.Address?.Trim() };
        }, StatusCodes.Status201Created, $"api/admin/publishers");

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpPut("publishers/{id:int}")]
    public IActionResult UpdatePublisher(int id, PublisherRequest r) =>
        ControllerExceptionHelper.Execute(() => service.UpdatePublisher(id, new(r.Name, r.Address)));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog), HttpDelete("publishers/{id:int}")]
    public IActionResult DeletePublisher(int id) =>
        ControllerExceptionHelper.Execute(() => service.DeletePublisher(id));

    [Authorize(Roles = StaffRoles.ManagerOrSales), HttpGet("customers")]
    public List<ManagerCustomerDto> Customers() => service.GetAllCustomersList();

    [Authorize(Roles = StaffRoles.ManagerOrSales), HttpPost("customers")]
    public IActionResult CreateCustomer(CustomerRequest r) =>
        ControllerExceptionHelper.Execute(() =>
        {
            var id = service.CreateCustomer(r.ToModel());
            return new { Id = id };
        }, StatusCodes.Status201Created, $"api/admin/customers");

    [Authorize(Roles = StaffRoles.ManagerOrSales), HttpPut("customers/{id}")]
    public IActionResult UpdateCustomer(string id, CustomerRequest r) =>
        ControllerExceptionHelper.Execute(() => service.UpdateCustomer(id, r.ToModel()));

    [Authorize(Roles = StaffRoles.ManagerOrSales), HttpDelete("customers/{id}")]
    public IActionResult DeleteCustomer(string id) =>
        ControllerExceptionHelper.Execute(() => service.DeleteCustomer(id));

    [Authorize(Roles = StaffRoles.Manager), HttpGet("employees")]
    public List<ManagerEmployeeDto> Employees() => service.GetAllEmployeesList();

    [Authorize(Roles = StaffRoles.Manager), HttpGet("employee-roles")]
    public List<StaffRoleOptionDto> EmployeeRoles() => service.GetAllEmployeeRolesList();

    [Authorize(Roles = StaffRoles.Manager), HttpPost("employees")]
    public IActionResult CreateEmployee(EmployeeRequest r) =>
        ControllerExceptionHelper.Execute(() =>
        {
            var id = service.CreateEmployee(r.ToModel());
            return new { Id = id };
        }, StatusCodes.Status201Created, $"api/admin/employees");

    [Authorize(Roles = StaffRoles.Manager), HttpPut("employees/{id}")]
    public IActionResult UpdateEmployee(string id, EmployeeRequest r) =>
        ControllerExceptionHelper.Execute(() => service.UpdateEmployee(id, r.ToModel(), GetAccountId()));

    [Authorize(Roles = StaffRoles.Manager), HttpDelete("employees/{id}")]
    public IActionResult DeleteEmployee(string id) =>
        ControllerExceptionHelper.Execute(() => service.DeleteEmployee(id, GetAccountId()));

    private int? GetAccountId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : null;
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
