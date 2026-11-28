using Microsoft.AspNetCore.Mvc;
using TrangSach.Customer.BUS;

namespace TrangSach.Customer.Web.Pages;

public sealed class IndexModel(CustomerWebApiClient api) : PageModel
{
    public List<TrangSach.Shared.DTO.BookItemDto> Books { get; private set; } = [];
    public List<string> Categories { get; private set; } = [];
    public List<string> Publishers { get; private set; } = [];
    public List<string> PromotionPrograms { get; private set; } = [];
    public List<string> PromotionTypes { get; private set; } = [];

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Category { get; set; }
    [BindProperty(SupportsGet = true)] public string? Publisher { get; set; }
    [BindProperty(SupportsGet = true)] public string? PriceRange { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MinPrice { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MaxPrice { get; set; }
    [BindProperty(SupportsGet = true)] public string? Promotion { get; set; }
    [BindProperty(SupportsGet = true)] public string? PromoType { get; set; }
    [BindProperty(SupportsGet = true)] public string? SortBy { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var allBooks = await api.GetBooksAsync(null, null, ct);

        // Danh mục bộ lọc
        Categories = allBooks.Select(book => book.Category)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(item => item).ToList();

        Publishers = allBooks.Select(book => book.Publisher)
            .Where(item => !string.IsNullOrWhiteSpace(item) && !item.Equals("Chưa cập nhật", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(item => item).ToList();

        PromotionPrograms = allBooks.Select(book => book.PromotionName)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(item => item).ToList();

        PromotionTypes = allBooks.Select(book => book.PromotionType)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(item => item).ToList();

        // Xử lý khoảng giá định sẵn nếu có chọn PriceRange
        decimal? effectiveMinPrice = MinPrice;
        decimal? effectiveMaxPrice = MaxPrice;

        if (!string.IsNullOrWhiteSpace(PriceRange))
        {
            switch (PriceRange)
            {
                case "under-100k":
                    effectiveMinPrice = 0;
                    effectiveMaxPrice = 100000;
                    break;
                case "100k-200k":
                    effectiveMinPrice = 100000;
                    effectiveMaxPrice = 200000;
                    break;
                case "200k-500k":
                    effectiveMinPrice = 200000;
                    effectiveMaxPrice = 500000;
                    break;
                case "above-500k":
                    effectiveMinPrice = 500000;
                    effectiveMaxPrice = null;
                    break;
            }
        }

        // Lọc đa điều kiện
        var query = allBooks.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(Category))
        {
            query = query.Where(book => book.Category.Equals(Category, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(Publisher))
        {
            query = query.Where(book => book.Publisher.Equals(Publisher, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            query = query.Where(book =>
                book.Title.Contains(Search, StringComparison.CurrentCultureIgnoreCase)
                || book.Author.Contains(Search, StringComparison.CurrentCultureIgnoreCase)
                || book.Publisher.Contains(Search, StringComparison.CurrentCultureIgnoreCase)
                || book.Description.Contains(Search, StringComparison.CurrentCultureIgnoreCase)
                || book.Isbn.Contains(Search, StringComparison.CurrentCultureIgnoreCase));
        }

        if (effectiveMinPrice.HasValue)
        {
            query = query.Where(book => book.Price >= effectiveMinPrice.Value);
        }

        if (effectiveMaxPrice.HasValue)
        {
            query = query.Where(book => book.Price <= effectiveMaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(Promotion))
        {
            if (Promotion.Equals("all-promo", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(book => book.DiscountPercent > 0 || !string.IsNullOrWhiteSpace(book.PromotionName));
            }
            else
            {
                query = query.Where(book => book.PromotionName.Equals(Promotion, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        if (!string.IsNullOrWhiteSpace(PromoType))
        {
            query = query.Where(book => book.PromotionType.Equals(PromoType, StringComparison.CurrentCultureIgnoreCase));
        }

        // Sắp xếp
        query = SortBy switch
        {
            "price-asc" => query.OrderBy(b => b.Price),
            "price-desc" => query.OrderByDescending(b => b.Price),
            "title-asc" => query.OrderBy(b => b.Title),
            "discount-desc" => query.OrderByDescending(b => b.DiscountPercent),
            _ => query.OrderBy(b => b.Title)
        };

        Books = query.ToList();
        ViewData["Title"] = "Cửa hàng sách";
    }

    public async Task<IActionResult> OnPostAddToCartAsync(string id, CancellationToken ct)
    {
        try
        {
            var guestModel = new AnonymousCartHelper(HttpContext, api);
            var token = await guestModel.EnsureCustomerTokenAsync(ct);
            var cart = await api.GetCartAsync(token, ct);
            var current = cart.FirstOrDefault(line => line.BookId == id)?.Quantity ?? 0;
            await api.SetCartQuantityAsync(token, id, current + 1, ct);
            TempData["Notice"] = "Đã thêm sách vào giỏ hàng.";
        }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage("/Index", new
        {
            search = Search,
            category = Category,
            publisher = Publisher,
            priceRange = PriceRange,
            minPrice = MinPrice,
            maxPrice = MaxPrice,
            promotion = Promotion,
            promoType = PromoType,
            sortBy = SortBy
        });
    }

    private sealed class AnonymousCartHelper(HttpContext httpContext, CustomerWebApiClient apiClient) : CustomerPageModel
    {
        public Task<string> EnsureCustomerTokenAsync(CancellationToken ct)
        {
            PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext { HttpContext = httpContext };
            return EnsureCustomerTokenAsync(apiClient, ct);
        }
    }
}
