using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class UserRoleDao(BookStoreDbContext context) : EntityDao<UserRole>(context)
{
    public Task<UserRole> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Query.SingleAsync(role => role.Name == name, cancellationToken);

    public Task<UserRole?> FindByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Query.SingleOrDefaultAsync(role => role.Name == name, cancellationToken);

}
