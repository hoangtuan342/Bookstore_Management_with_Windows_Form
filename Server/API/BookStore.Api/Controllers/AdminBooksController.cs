using BookStore.Api.Contracts;
using BookStore.Shared.Models;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.BUS.Models;
using TrangSach.BUS.Results;
using TrangSach.BUS.Services;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/books")]
public sealed class AdminBooksController(AdminBookService service) : ControllerBase
{
    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpGet("{id}/edit")]
    public async Task<IActionResult> GetBookForEdit(string id, CancellationToken cancellationToken) =>
        ToHttpResult(await service.GetBookForEditAsync(id, cancellationToken));

    [Authorize(Roles = StaffRoles.ProductReaders)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookItem>>> GetBooks(CancellationToken cancellationToken) =>
        Ok(await service.GetBooksAsync(cancellationToken));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPost]
    [ProducesResponseType<BookItem>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBook(BookManagementRequest request, CancellationToken cancellationToken) =>
        ToHttpResult(await service.CreateBookAsync(ToCommand(request), cancellationToken));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBook(string id, BookManagementRequest request, CancellationToken cancellationToken) =>
        ToHttpResult(await service.UpdateBookAsync(id, ToCommand(request), cancellationToken));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> HideBook(string id, CancellationToken cancellationToken) =>
        ToHttpResult(await service.HideBookAsync(id, cancellationToken));

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPut("{id}/visibility")]
    public async Task<IActionResult> SetVisibility(string id, BookVisibilityRequest request, CancellationToken cancellationToken) =>
        ToHttpResult(await service.SetVisibilityAsync(id, new BookVisibilityCommand(request.IsActive), cancellationToken));

    private static BookManagementCommand ToCommand(BookManagementRequest request) =>
        new(request.Title, request.SalePrice, request.Unit, request.CategoryId, request.PublisherId, request.AuthorIds);

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
