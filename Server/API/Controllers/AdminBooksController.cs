using TrangSach.API.Contracts;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace TrangSach.API.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/books")]
public sealed class AdminBooksController(AdminBookService service) : ControllerBase
{
    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpGet("{id}/edit")]
    public IActionResult GetBookForEdit(string id) =>
        ToHttpResult(service.GetBookForEdit(id));

    [Authorize(Roles = StaffRoles.ProductReaders)]
    [HttpGet]
    public ActionResult<IReadOnlyList<BookItemDto>> GetBooks() =>
        Ok(service.GetBooks());

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPost]
    [ProducesResponseType<BookItemDto>(StatusCodes.Status201Created)]
    public IActionResult CreateBook(BookManagementRequest request) =>
        ToHttpResult(service.CreateBook(ToCommand(request)));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPut("{id}")]
    public IActionResult UpdateBook(string id, BookManagementRequest request) =>
        ToHttpResult(service.UpdateBook(id, ToCommand(request)));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpDelete("{id}")]
    public IActionResult HideBook(string id) =>
        ToHttpResult(service.HideBook(id));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPut("{id}/visibility")]
    public IActionResult SetVisibility(string id, BookVisibilityRequest request) =>
        ToHttpResult(service.SetVisibility(id, new BookVisibilityCommandDto(request.IsActive)));

    private static BookManagementCommandDto ToCommand(BookManagementRequest request) =>
        new(request.Title, request.SalePrice, request.Unit, request.CategoryId, request.PublisherId, request.AuthorIds, request.Description);

    private IActionResult ToHttpResult<T>(OperationResult<T> result) => result.Status switch
    {
        OperationStatus.Success when result.Value is Unit => NoContent(),
        OperationStatus.Success => Ok(result.Value),
        OperationStatus.Created => StatusCode(StatusCodes.Status201Created, result.Value),
        OperationStatus.NotFound => NotFound(),
        OperationStatus.Invalid => BadRequest(new { message = result.Message }),
        OperationStatus.Conflict => Conflict(new { message = result.Message }),
        OperationStatus.Forbidden => Forbid(),
        OperationStatus.Unauthorized => Unauthorized(new { message = result.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}
