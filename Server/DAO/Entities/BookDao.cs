using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class BookDao(BookStoreDbContext context) : EntityDao<Book>(context)
{
    public Task<Book?> FindActiveByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Query.SingleOrDefaultAsync(book => book.Id == id && book.IsActive, cancellationToken);

    public Task<Book?> FindForManagementAsync(string id, CancellationToken cancellationToken = default) =>
        Query.Include(book => book.Authors).SingleOrDefaultAsync(book => book.Id == id, cancellationToken);

    public Task<List<Book>> GetManagementListAsync(CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Include(book => book.Category)
            .Include(book => book.Authors).ThenInclude(link => link.Author)
            .OrderBy(book => book.Title).ToListAsync(cancellationToken);

    public Task<List<Book>> GetActiveCatalogAsync(CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Where(book => book.IsActive)
            .Include(book => book.Category)
            .Include(book => book.Authors).ThenInclude(link => link.Author)
            .OrderBy(book => book.Title).ToListAsync(cancellationToken);

    public Task<List<Book>> SearchActiveCatalogAsync(string? search, string? category, CancellationToken cancellationToken = default)
    {
        IQueryable<Book> query = Query.AsNoTracking().Where(book => book.IsActive)
            .Include(book => book.Category)
            .Include(book => book.Authors).ThenInclude(link => link.Author);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(book => book.Title.Contains(search)
                || book.Authors.Any(link => link.Author.Name.Contains(search)));
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(book => book.Category.Name == category);
        return query.OrderBy(book => book.Title).ToListAsync(cancellationToken);
    }

    public Task<Book?> FindActiveWithDetailsAsync(string id, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Include(book => book.Category)
            .Include(book => book.Authors).ThenInclude(link => link.Author)
            .FirstOrDefaultAsync(book => book.Id == id && book.IsActive, cancellationToken);

    public Task<int> CountActiveAsync(CancellationToken cancellationToken = default) =>
        Query.CountAsync(book => book.IsActive, cancellationToken);

    public Task<int> CountActiveLowStockAsync(int threshold, CancellationToken cancellationToken = default) =>
        Query.CountAsync(book => book.IsActive && book.StockQuantity <= threshold, cancellationToken);

    public Task<bool> HasCategoryAsync(string categoryId, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(book => book.CategoryId == categoryId, cancellationToken);

    public Task<bool> HasPublisherAsync(int publisherId, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(book => book.PublisherId == publisherId, cancellationToken);

    public Task<Dictionary<string, Book>> GetByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default) =>
        Query.Where(book => ids.Contains(book.Id)).ToDictionaryAsync(book => book.Id, cancellationToken);

    public Task<Dictionary<string, Book>> GetActiveByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default) =>
        Query.Where(book => ids.Contains(book.Id) && book.IsActive).ToDictionaryAsync(book => book.Id, cancellationToken);

    public Task<int> CountByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default) =>
        Query.CountAsync(book => ids.Contains(book.Id), cancellationToken);

    public Task<List<InventoryBookRecord>> GetActiveInventoryAsync(CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Where(book => book.IsActive)
            .Select(book => new InventoryBookRecord(book.Category.Name, book.StockQuantity, book.SalePrice))
            .ToListAsync(cancellationToken);
}

public sealed record InventoryBookRecord(string CategoryName, int StockQuantity, decimal SalePrice);
