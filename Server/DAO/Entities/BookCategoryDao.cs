using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class BookCategoryDao(BookStoreDbContext context) : EntityDao<BookCategory>(context)
{
    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(category => category.Id == id, cancellationToken);

    public Task<string> GetNameAsync(string id, CancellationToken cancellationToken = default) =>
        Query.Where(category => category.Id == id).Select(category => category.Name).SingleAsync(cancellationToken);
}
