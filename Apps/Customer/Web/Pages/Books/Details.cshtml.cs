using Microsoft.AspNetCore.Mvc;
using TrangSach.Customer.BUS;
using TrangSach.Shared.DTO;

namespace TrangSach.Customer.Web.Pages.Books;

public sealed class DetailsModel(CustomerWebApiClient api) : PageModel
{
    public BookItemDto? Book { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken ct)
    {
        Book = await api.GetBookAsync(id, ct);
        if (Book is null) return NotFound();
        ViewData["Title"] = Book.Title;
        return Page();
    }

    public Task<IActionResult> OnPostAddToCartAsync(string id, CancellationToken ct) => AddToCartAsync(id, buyNow: false, ct);
    public Task<IActionResult> OnPostBuyNowAsync(string id, CancellationToken ct) => AddToCartAsync(id, buyNow: true, ct);

    private async Task<IActionResult> AddToCartAsync(string id, bool buyNow, CancellationToken ct)
    {
        if (!User.IsInRole("Customer"))
            return RedirectToPage("/Account/Login", new { returnUrl = Url.Page("/Books/Details", new { id }) });
        try
        {
            var token = User.FindFirst("access_token")?.Value ?? string.Empty;
            var book = await api.GetBookAsync(id, ct) ?? throw new InvalidOperationException("Sản phẩm không còn tồn tại.");
            if (book.AvailableStock <= 0) throw new InvalidOperationException("Sản phẩm hiện đã hết hàng.");
            var cart = await api.GetCartAsync(token, ct);
            var quantity = cart.FirstOrDefault(line => line.BookId == id)?.Quantity ?? 0;
            await api.SetCartQuantityAsync(token, id, quantity + 1, ct);
            TempData["Notice"] = $"Đã thêm ‘{book.Title}’ vào giỏ hàng.";
            return buyNow ? RedirectToPage("/Checkout/Index") : RedirectToPage("/Books/Details", new { id });
        }
        catch (Exception ex)
        {
            TempData["Notice"] = ex.Message;
            return RedirectToPage("/Books/Details", new { id });
        }
    }
}
