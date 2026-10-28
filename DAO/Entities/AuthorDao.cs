using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class AuthorDao(BookStoreDbContext context) : EntityDao<Author>(context)
{
    public Task<List<Author>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default) =>
        Query.Where(author => ids.Contains(author.Id)).ToListAsync(cancellationToken);
}
