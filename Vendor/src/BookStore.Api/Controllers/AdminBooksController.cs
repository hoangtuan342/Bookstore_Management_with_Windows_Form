using System.Security.Cryptography;
using BookStore.Api.Contracts;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using BookStore.Shared.Security;
using BookStore.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/books")]
public sealed class AdminBooksController(BookStoreDbContext db) : ControllerBase
{
    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpGet("{id}/edit")]
    public async Task<IActionResult> GetBookForEdit(string id, CancellationToken cancellationToken)
    {
        var book = await db.Books.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new BookEditDetails(item.Id, item.Title, item.SalePrice, item.Unit, item.CategoryId, item.PublisherId,
                item.Authors.Select(link => link.AuthorId).ToArray(), item.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
        return book is null ? NotFound() : Ok(book);
    }

    [Authorize(Roles = StaffRoles.ProductReaders)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookItem>>> GetBooks(CancellationToken cancellationToken)
    {
        var books = await db.Books.AsNoTracking()
            .Include(book => book.Category)
            .Include(book => book.Authors).ThenInclude(author => author.Author)
            .OrderBy(book => book.Title)
            .Select(book => new BookItem(
                book.Id,
                book.Title,
                book.Authors.OrderBy(author => author.Author.Name).Select(author => author.Author.Name).FirstOrDefault() ?? "Chưa cập nhật",
                book.Category.Name,
                book.SalePrice,
                "cover-sage",
                book.IsActive ? "ĐANG BÁN" : "ĐÃ ẨN",
                string.Empty,
                book.StockQuantity))
            .ToListAsync(cancellationToken);

        return Ok(books);
    }

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPost]
    [ProducesResponseType<BookItem>(StatusCodes.Status201Created)]
    public async Task<ActionResult<BookItem>> CreateBook(BookManagementRequest request, CancellationToken cancellationToken)
    {
        var referencesValid = await db.Categories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken)
            && (request.PublisherId is null || await db.Publishers.AnyAsync(publisher => publisher.Id == request.PublisherId, cancellationToken));
        var authors = await db.Authors.Where(author => request.AuthorIds.Contains(author.Id)).ToListAsync(cancellationToken);
        if (!referencesValid || authors.Count != request.AuthorIds.Distinct().Count())
        {
            return BadRequest(new { message = "Thể loại, nhà xuất bản hoặc tác giả không hợp lệ." });
        }

        var book = new Book
        {
            Id = $"S{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}",
            Title = request.Title.Trim(),
            CategoryId = request.CategoryId,
            PublisherId = request.PublisherId,
            SalePrice = request.SalePrice,
            Unit = request.Unit.Trim(),
            StockQuantity = 0,
            Authors = authors.Select(author => new BookAuthor { Author = author }).ToList()
        };

        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);
        var categoryName = await db.Categories.Where(category => category.Id == book.CategoryId)
            .Select(category => category.Name).SingleAsync(cancellationToken);
        var response = new BookItem(book.Id, book.Title,
            authors.OrderBy(author => author.Name).Select(author => author.Name).FirstOrDefault() ?? "Chưa cập nhật",
            categoryName, book.SalePrice, "cover-sage", "ĐANG BÁN", string.Empty, book.StockQuantity);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBook(string id, BookManagementRequest request, CancellationToken cancellationToken)
    {
        var book = await db.Books.Include(item => item.Authors)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        var referencesValid = await db.Categories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken)
            && (request.PublisherId is null || await db.Publishers.AnyAsync(publisher => publisher.Id == request.PublisherId, cancellationToken));
        var authors = await db.Authors.Where(author => request.AuthorIds.Contains(author.Id)).ToListAsync(cancellationToken);
        if (!referencesValid || authors.Count != request.AuthorIds.Distinct().Count())
        {
            return BadRequest(new { message = "Thể loại, nhà xuất bản hoặc tác giả không hợp lệ." });
        }

        book.Title = request.Title.Trim();
        book.CategoryId = request.CategoryId;
        book.PublisherId = request.PublisherId;
        book.SalePrice = request.SalePrice;
        book.Unit = request.Unit.Trim();
        db.BookAuthors.RemoveRange(book.Authors);
        book.Authors = authors.Select(author => new BookAuthor { BookId = book.Id, AuthorId = author.Id, Author = author }).ToList();

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> HideBook(string id, CancellationToken cancellationToken)
    {
        var book = await db.Books.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        book.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = StaffRoles.ManagerOrCatalog)]
    [HttpPut("{id}/visibility")]
    public async Task<IActionResult> SetVisibility(string id, BookVisibilityRequest request, CancellationToken cancellationToken)
    {
        var book = await db.Books.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (book is null)
        {
            return NotFound();
        }

        book.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record BookEditDetails(string Id, string Title, decimal SalePrice, string Unit, string CategoryId, int? PublisherId, IReadOnlyList<int> AuthorIds, bool IsActive);
