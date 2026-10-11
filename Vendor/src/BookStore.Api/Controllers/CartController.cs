using System.Security.Claims;
using BookStore.Api.Contracts;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using BookStore.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/cart")]
public sealed class CartController(BookStoreDbContext db, BookPricingService pricing) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CartLineResponse>>> GetCart(CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue("customer_id");
        if (customerId is null)
        {
            return Forbid();
        }

        var cart = await db.CartItems.AsNoTracking()
            .Where(item => item.CustomerId == customerId)
            .OrderBy(item => item.Book.Title)
            .Include(item => item.Book).ThenInclude(book => book.Authors).ThenInclude(link => link.Author)
            .ToListAsync(cancellationToken);
        var discounts = await pricing.GetActiveDiscountsAsync(cart.Select(item => item.BookId).ToArray(), DateTime.UtcNow, cancellationToken);
        var items = cart.Select(item =>
        {
            var unitPrice = BookPricingService.ApplyDiscount(item.Book.SalePrice, discounts.GetValueOrDefault(item.BookId));
            return new CartLineResponse(
                item.BookId,
                item.Book.Title,
                item.Book.Authors.Select(author => author.Author.Name).FirstOrDefault() ?? "Chưa cập nhật",
                unitPrice,
                item.Quantity,
                item.Book.StockQuantity,
                unitPrice * item.Quantity);
        }).ToArray();

        return Ok(items);
    }

    [HttpPut("items/{bookId}")]
    public async Task<IActionResult> SetQuantity(string bookId, SetCartQuantityRequest request, CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue("customer_id");
        if (customerId is null)
        {
            return Forbid();
        }

        var book = await db.Books.SingleOrDefaultAsync(item => item.Id == bookId, cancellationToken);
        if (book is null)
        {
            return NotFound(new { message = "Không tìm thấy sách." });
        }

        if (request.Quantity > book.StockQuantity)
        {
            return Conflict(new { message = "Số lượng vượt quá tồn kho hiện tại." });
        }

        var cartItem = await db.CartItems.FindAsync([customerId, bookId], cancellationToken);
        if (cartItem is null)
        {
            db.CartItems.Add(new CartItem { CustomerId = customerId, BookId = bookId, Quantity = request.Quantity });
        }
        else
        {
            cartItem.Quantity = request.Quantity;
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("items/{bookId}")]
    public async Task<IActionResult> RemoveItem(string bookId, CancellationToken cancellationToken)
    {
        var customerId = User.FindFirstValue("customer_id");
        if (customerId is null)
        {
            return Forbid();
        }

        var item = await db.CartItems.FindAsync([customerId, bookId], cancellationToken);
        if (item is null)
        {
            return NoContent();
        }

        db.CartItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
