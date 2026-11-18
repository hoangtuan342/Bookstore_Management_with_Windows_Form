using Microsoft.AspNetCore.Mvc;
using TrangSach.Customer.BUS;

namespace TrangSach.Customer.Web.Pages;

public sealed class IndexModel(CustomerWebApiClient api) : PageModel
{
    public IReadOnlyList<TrangSach.Shared.DTO.BookItemDto> Books { get; private set; } = [];
    public IReadOnlyList<string> Categories { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Category { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var allBooks = await api.GetBooksAsync(null, null, ct);
        Categories = allBooks.Select(book => book.Category).Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(item => item).ToArray();
        Books = allBooks.Where(book => (string.IsNullOrWhiteSpace(Category) || book.Category.Equals(Category, StringComparison.CurrentCultureIgnoreCase))
            && (string.IsNullOrWhiteSpace(Search) || book.Title.Contains(Search, StringComparison.CurrentCultureIgnoreCase)
                || book.Author.Contains(Search, StringComparison.CurrentCultureIgnoreCase)
                || book.Description.Contains(Search, StringComparison.CurrentCultureIgnoreCase))).ToArray();
        ViewData["Title"] = "Cửa hàng";
    }

    public async Task<IActionResult> OnPostAddToCartAsync(string id, CancellationToken ct)
    {
        if (!User.IsInRole("Customer")) return RedirectToPage("/Account/Login", new { returnUrl = Url.Page("/Books/Details", new { id }) });
        try
        {
            var token = User.FindFirst("access_token")?.Value ?? string.Empty;
            var cart = await api.GetCartAsync(token, ct);
            var current = cart.FirstOrDefault(line => line.BookId == id)?.Quantity ?? 0;
            await api.SetCartQuantityAsync(token, id, current + 1, ct);
            TempData["Notice"] = "Đã thêm sách vào giỏ hàng.";
        }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage("/Index", new { search = Search, category = Category });
    }
}
