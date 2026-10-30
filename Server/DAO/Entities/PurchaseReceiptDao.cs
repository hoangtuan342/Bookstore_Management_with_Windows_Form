using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class PurchaseReceiptDao(BookStoreDbContext context) : EntityDao<PurchaseReceipt>(context)
{
    public Task<bool> HasSupplierAsync(string supplierId, CancellationToken cancellationToken = default) =>
        Query.AnyAsync(receipt => receipt.SupplierId == supplierId, cancellationToken);

    public Task<List<PurchaseReceipt>> GetRecentWithDetailsAsync(int count, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Include(receipt => receipt.Supplier)
            .Include(receipt => receipt.Lines).ThenInclude(line => line.Book)
            .OrderByDescending(receipt => receipt.CreatedAt).Take(count).ToListAsync(cancellationToken);

    public Task<PurchaseReceipt?> FindWithLinesAndBooksAsync(string id, CancellationToken cancellationToken = default) =>
        Query.Include(receipt => receipt.Lines).ThenInclude(line => line.Book)
            .SingleOrDefaultAsync(receipt => receipt.Id == id, cancellationToken);
}
