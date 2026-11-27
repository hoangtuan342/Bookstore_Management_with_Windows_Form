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
        try
        {
            var guestModel = new AnonymousCartHelper(HttpContext, api);
            var token = await guestModel.EnsureCustomerTokenAsync(ct);
            var book = await api.GetBookAsync(id, ct) ?? throw new InvalidOperationException("Sản phẩm không còn tồn tại.");
            if (book.AvailableStock <= 0) throw new InvalidOperationException("Sản phẩm hiện đã hết hàng.");
            
            try
            {
                var cart = await api.GetCartAsync(token, ct);
                var quantity = cart.FirstOrDefault(line => line.BookId == id)?.Quantity ?? 0;
                await api.SetCartQuantityAsync(token, id, quantity + 1, ct);
            }
            catch (Exception ex) when (CustomerWebApiClient.IsUnauthorizedException(ex))
            {
                // Nếu token cũ của cookie trước đó đã hết hạn, tự động làm mới token phiên khách vãng lai và thử lại
                token = await guestModel.EnsureCustomerTokenAsync(ct, forceRefresh: true);
                var cart = await api.GetCartAsync(token, ct);
                var quantity = cart.FirstOrDefault(line => line.BookId == id)?.Quantity ?? 0;
                await api.SetCartQuantityAsync(token, id, quantity + 1, ct);
            }

            TempData["Notice"] = $"Đã thêm ‘{book.Title}’ vào giỏ hàng.";
            return buyNow ? RedirectToPage("/Checkout/Index") : RedirectToPage("/Books/Details", new { id });
        }
        catch (Exception ex)
        {
            TempData["Notice"] = ex.Message;
            return RedirectToPage("/Books/Details", new { id });
        }
    }

    private sealed class AnonymousCartHelper(HttpContext httpContext, CustomerWebApiClient apiClient) : CustomerPageModel
    {
        public Task<string> EnsureCustomerTokenAsync(CancellationToken ct, bool forceRefresh = false)
        {
            PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext { HttpContext = httpContext };
            return EnsureCustomerTokenAsync(apiClient, ct, forceRefresh);
        }
    }
}
