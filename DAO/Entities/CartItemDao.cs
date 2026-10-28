using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class CartItemDao(BookStoreDbContext context) : EntityDao<CartItem>(context)
{
    public Task<List<CartItem>> GetCustomerCartAsync(string customerId, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = Query.Where(item => item.CustomerId == customerId);
        if (!tracking) query = query.AsNoTracking();
        return query.Include(item => item.Book).ThenInclude(book => book.Authors).ThenInclude(link => link.Author)
            .OrderBy(item => item.Book.Title).ToListAsync(cancellationToken);
    }

    public Task<List<CartItem>> GetCustomerCheckoutCartAsync(string customerId, CancellationToken cancellationToken = default) =>
        Query.Where(item => item.CustomerId == customerId).Include(item => item.Book).ToListAsync(cancellationToken);

    public Task<List<CartItem>> GetCustomerDisplayCartAsync(string customerId, CancellationToken cancellationToken = default) =>
        GetCustomerCartAsync(customerId, tracking: false, cancellationToken);

    public Task<CartItem?> FindByCustomerAndBookAsync(string customerId, string bookId, CancellationToken cancellationToken = default) =>
        Query.SingleOrDefaultAsync(item => item.CustomerId == customerId && item.BookId == bookId, cancellationToken);
}
