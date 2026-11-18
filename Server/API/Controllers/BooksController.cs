using TrangSach.Shared.DTO;
using Microsoft.AspNetCore.Mvc;
namespace TrangSach.API.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController(BookCatalogService catalog) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<BookItemDto>>(StatusCodes.Status200OK)]
    public ActionResult<List<BookItemDto>> GetBooks(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null) =>
        Ok(catalog.GetBooks(search, category));

    [HttpGet("{id}")]
    [ProducesResponseType<BookItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BookItemDto> GetBook(string id)
    {
        var book = catalog.GetBook(id);
        return book is null ? NotFound() : Ok(book);
    }
}
