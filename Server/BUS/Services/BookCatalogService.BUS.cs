using System.Collections.Generic;
using System.Linq;
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

        private void EnsureLoaded()
        {
            if (_activeBooks.Count == 0)
            {
                var all = _dao.Catalog.GetAllBooks();
                _activeBooks = all.Where(b => b.Label == "ĐANG BÁN").ToList();
            }
        }

        public List<BookItemDto> GetBooks(string? search = null, string? category = null)
        {
            EnsureLoaded();

            var books = _activeBooks.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
                books = books.Where(b => b.Title.Contains(search, System.StringComparison.OrdinalIgnoreCase)
                                      || b.Author.Contains(search, System.StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(category))
                books = books.Where(b => b.Category == category);

            var result = books.ToList();
            ApplyDiscounts(result, System.DateTime.UtcNow);
            return result;
        }

        public BookItemDto? GetBook(string id)
        {
            EnsureLoaded();

            var book = _activeBooks.FirstOrDefault(b => b.Id == id);
            if (book == null) return null;

            var discounts = _pricing.GetActiveDiscounts(new List<string> { id }, System.DateTime.UtcNow);
            if (discounts.TryGetValue(id, out var pct) && pct > 0)
            {
                var finalPrice = BookPricingService.ApplyDiscount(book.Price, pct);
                return new BookItemDto(book.Id, book.Title, book.Author, book.Category, finalPrice,
                    book.CoverStyle, $"GIẢM {pct:0.#}%", book.Description, book.AvailableStock, book.Publisher, book.CoverImagePath, book.Isbn);
            }
            return book;
        }

        private void ApplyDiscounts(List<BookItemDto> books, System.DateTime nowUtc)
        {
            if (books.Count == 0) return;
            var ids = books.Select(b => b.Id).ToList();
            var discounts = _pricing.GetActiveDiscounts(ids, nowUtc);
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
