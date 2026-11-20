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
    public IActionResult GetBookForEdit(string id)
    {
        var book = service.GetBookForEdit(id);
        return book is null ? NotFound() : Ok(book);
    }

    [Authorize(Roles = StaffRoles.ProductReaders)]
    [HttpGet]
    public ActionResult<List<BookItemDto>> GetBooks() =>
        Ok(service.GetBooks());

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPost]
    [ProducesResponseType<BookItemDto>(StatusCodes.Status201Created)]
    public IActionResult CreateBook(BookManagementRequest request) =>
        ControllerExceptionHelper.Execute(() => service.CreateBook(ToCommand(request)), StatusCodes.Status201Created);

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPut("{id}")]
    public IActionResult UpdateBook(string id, BookManagementRequest request) =>
        ControllerExceptionHelper.Execute(() => service.UpdateBook(id, ToCommand(request)));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpDelete("{id}")]
    public IActionResult HideBook(string id) =>
        ControllerExceptionHelper.Execute(() => service.HideBook(id));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPut("{id}/visibility")]
    public IActionResult SetVisibility(string id, BookVisibilityRequest request) =>
        ControllerExceptionHelper.Execute(() => service.SetVisibility(id, new BookVisibilityCommandDto(request.IsActive)));

    private static BookManagementCommandDto ToCommand(BookManagementRequest request) =>
        new(request.Title, request.SalePrice, request.Unit, request.CategoryId, request.PublisherId, request.AuthorIds, request.Description, request.CoverImagePath, request.Isbn, request.IsActive);
}
