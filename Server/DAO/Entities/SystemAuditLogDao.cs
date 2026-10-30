using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class SystemAuditLogDao(BookStoreDbContext context) : EntityDao<SystemAuditLog>(context)
{
    public Task<List<SystemAuditLog>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().OrderByDescending(entry => entry.OccurredAt).Take(count).ToListAsync(cancellationToken);
}
