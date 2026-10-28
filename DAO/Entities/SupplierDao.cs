using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class SupplierDao(BookStoreDbContext context) : EntityDao<Supplier>(context)
{
    public Task<List<Supplier>> GetAllOrderedAsync(CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().OrderBy(supplier => supplier.Name).ToListAsync(cancellationToken);
}
