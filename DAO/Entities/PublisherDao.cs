using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class PublisherDao(BookStoreDbContext context) : EntityDao<Publisher>(context)
{
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(publisher => publisher.Id == id, cancellationToken);
}
