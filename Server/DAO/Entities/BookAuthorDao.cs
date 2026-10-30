using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class BookAuthorDao(BookStoreDbContext context) : EntityDao<BookAuthor>(context)
{
    public Task<bool> HasAuthorAsync(int authorId, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(link => link.AuthorId == authorId, cancellationToken);
}
