using BookStore.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using TrangSach.BUS.Services;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController(BookCatalogService catalog) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BookItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookItem>>> GetBooks(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default) =>
        Ok(await catalog.GetBooksAsync(search, category, cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<BookItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookItem>> GetBook(string id, CancellationToken cancellationToken)
    {
        var book = await catalog.GetBookAsync(id, cancellationToken);
        return book is null ? NotFound() : Ok(book);
    }
}
