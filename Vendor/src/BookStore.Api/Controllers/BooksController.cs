using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using BookStore.Infrastructure.Services;
using BookStore.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController(BookStoreDbContext db, BookPricingService pricing) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BookItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookItem>>> GetBooks(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Books.AsNoTracking().Where(book => book.IsActive)
            .Include(book => book.Category)
            .Include(book => book.Authors).ThenInclude(bookAuthor => bookAuthor.Author)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(book => book.Title.Contains(search)
                || book.Authors.Any(bookAuthor => bookAuthor.Author.Name.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(book => book.Category.Name == category);
        }

        var books = await query.OrderBy(book => book.Title).ToListAsync(cancellationToken);
        return Ok(await ToBookItemsAsync(books, cancellationToken));
    }

    [HttpGet("{id}")]
    [ProducesResponseType<BookItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookItem>> GetBook(string id, CancellationToken cancellationToken)
    {
        var book = await db.Books.AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Authors).ThenInclude(bookAuthor => bookAuthor.Author)
            .FirstOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken);

        if (book is null)
        {
            return NotFound();
        }

        var items = await ToBookItemsAsync([book], cancellationToken);
        return Ok(items[0]);
    }

    private async Task<IReadOnlyList<BookItem>> ToBookItemsAsync(IReadOnlyCollection<Book> books, CancellationToken cancellationToken)
    {
        var discounts = await pricing.GetActiveDiscountsAsync(books.Select(book => book.Id).ToArray(), DateTime.UtcNow, cancellationToken);
        return books.Select(book =>
        {
            var discount = discounts.GetValueOrDefault(book.Id);
            var coverStyle = book.Id switch
            {
                "sach-01" => "cover-sand",
                "sach-02" => "cover-coral",
                "sach-03" => "cover-sage",
                "sach-04" => "cover-ink",
                "sach-05" => "cover-blue",
                "sach-06" => "cover-plum",
                _ => "cover-sage"
            };
            return new BookItem(
                book.Id,
                book.Title,
                book.Authors.OrderBy(author => author.Author.Name).Select(author => author.Author.Name).FirstOrDefault() ?? "Chưa cập nhật",
                book.Category.Name,
                BookPricingService.ApplyDiscount(book.SalePrice, discount),
                coverStyle,
                discount > 0 ? $"GIẢM {discount:0.#}%" : "SÁCH HAY",
                string.Empty,
                book.StockQuantity);
        }).ToArray();
    }
}
