using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class CustomerDao(BookStoreDbContext context) : EntityDao<Customer>(context)
{
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Query.CountAsync(cancellationToken);

    public Task<bool> ExistsByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(customer => customer.Id == id, cancellationToken);

    public Task<List<Customer>> GetAllWithAccountsAsync(CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Include(customer => customer.UserAccount).OrderBy(customer => customer.FullName).ToListAsync(cancellationToken);

    public Task<Customer?> FindWithAccountAsync(string id, CancellationToken cancellationToken = default) =>
        Query.Include(customer => customer.UserAccount).SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public Task<Customer?> FindByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Query.SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);
}
