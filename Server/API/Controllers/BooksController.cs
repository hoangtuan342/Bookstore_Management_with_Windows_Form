using TrangSach.Shared.DTO;
using Microsoft.AspNetCore.Mvc;
namespace TrangSach.API.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController(BookCatalogService catalog) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BookItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookItemDto>>> GetBooks(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default) =>
        Ok(await catalog.GetBooksAsync(search, category, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<BookItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookItemDto>> GetBook(string id, CancellationToken cancellationToken)
    {
        var book = await catalog.GetBookAsync(id, cancellationToken);
        return book is null ? NotFound() : Ok(book);
    }
}


