using BookStore.Domain.Entities;
using BookStore.Shared.Models;
using TrangSach.DAO.Entities;

namespace TrangSach.BUS.Services;

public sealed class BookCatalogService(BookStoreDao dao, BookPricingService pricing)
{
    public async Task<IReadOnlyList<BookItem>> GetBooksAsync(
        string? search = null, string? category = null, CancellationToken cancellationToken = default)
    {
        var books = await dao.Books.SearchActiveCatalogAsync(search, category, cancellationToken);
        return await ToBookItemsAsync(books, cancellationToken);
    }

    public async Task<BookItem?> GetBookAsync(string id, CancellationToken cancellationToken = default)
    {
        var book = await dao.Books.FindActiveWithDetailsAsync(id, cancellationToken);
        if (book is null) return null;
        return (await ToBookItemsAsync([book], cancellationToken))[0];
    }

    private async Task<IReadOnlyList<BookItem>> ToBookItemsAsync(IReadOnlyCollection<Book> books, CancellationToken cancellationToken)
    {
        var discounts = await pricing.GetActiveDiscountsAsync(books.Select(book => book.Id).ToArray(), DateTime.UtcNow, cancellationToken);
        return books.Select(book =>
        {
            var discount = discounts.GetValueOrDefault(book.Id);
            var coverStyle = book.Id switch
            {
                "sach-01" => "cover-sand", "sach-02" => "cover-coral", "sach-03" => "cover-sage",
                "sach-04" => "cover-ink", "sach-05" => "cover-blue", "sach-06" => "cover-plum",
                _ => "cover-sage"
            };
            return new BookItem(book.Id, book.Title,
                book.Authors.OrderBy(author => author.Author.Name).Select(author => author.Author.Name).FirstOrDefault() ?? "Chưa cập nhật",
                book.Category.Name, BookPricingService.ApplyDiscount(book.SalePrice, discount), coverStyle,
                discount > 0 ? $"GIẢM {discount:0.#}%" : "SÁCH HAY", string.Empty, book.StockQuantity);
        }).ToArray();
    }
}
