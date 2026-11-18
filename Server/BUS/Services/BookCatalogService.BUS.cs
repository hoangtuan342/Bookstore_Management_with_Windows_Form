using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    /// <summary>
    /// Service hiển thị danh mục sách cho cửa hàng (store-front).
    /// </summary>
    public class BookCatalogService
    {
        private readonly BookStoreDao _dao;
        private readonly BookPricingService _pricing;

        private List<BookItemDto> _activeBooks = new();

        public BookCatalogService(BookStoreDao dao, BookPricingService pricing)
        {
            _dao = dao;
            _pricing = pricing;
        }

        private async Task EnsureLoadedAsync(CancellationToken ct)
        {
            if (_activeBooks.Count == 0)
            {
                var all = await _dao.Catalog.GetAllBooksAsync(ct).ConfigureAwait(false);
                _activeBooks = all.Where(b => b.Label == "ĐANG BÁN").ToList();
            }
        }

        public async Task<IReadOnlyList<BookItemDto>> GetBooksAsync(
            string? search = null, string? category = null, CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var books = _activeBooks.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
                books = books.Where(b => b.Title.Contains(search, System.StringComparison.OrdinalIgnoreCase)
                                      || b.Author.Contains(search, System.StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(category))
                books = books.Where(b => b.Category == category);

            var result = books.ToList();
            await ApplyDiscountsAsync(result, System.DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public async Task<BookItemDto?> GetBookAsync(string id, CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

            var book = _activeBooks.FirstOrDefault(b => b.Id == id);
            if (book == null) return null;

            var discounts = await _pricing.GetActiveDiscountsAsync(new[] { id }, System.DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
            if (discounts.TryGetValue(id, out var pct) && pct > 0)
            {
                var finalPrice = BookPricingService.ApplyDiscount(book.Price, pct);
                return new BookItemDto(book.Id, book.Title, book.Author, book.Category, finalPrice,
                    book.CoverStyle, $"GIẢM {pct:0.#}%", book.Description, book.AvailableStock, book.Publisher, book.CoverImagePath, book.Isbn);
            }
            return book;
        }

        private async Task ApplyDiscountsAsync(List<BookItemDto> books, System.DateTime nowUtc, CancellationToken ct)
        {
            if (books.Count == 0) return;
            var ids = books.Select(b => b.Id).ToArray();
            var discounts = await _pricing.GetActiveDiscountsAsync(ids, nowUtc, ct).ConfigureAwait(false);
            for (int i = 0; i < books.Count; i++)
            {
                var b = books[i];
                if (discounts.TryGetValue(b.Id, out var pct) && pct > 0)
                {
                    var newPrice = BookPricingService.ApplyDiscount(b.Price, pct);
                    books[i] = new BookItemDto(b.Id, b.Title, b.Author, b.Category, newPrice,
                        b.CoverStyle, $"GIẢM {pct:0.#}%", b.Description, b.AvailableStock, b.Publisher, b.CoverImagePath, b.Isbn);
                }
            }
        }
    }
}
