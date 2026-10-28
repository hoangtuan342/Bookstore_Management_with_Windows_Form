using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class PurchaseReceiptLineDao(BookStoreDbContext context) : EntityDao<PurchaseReceiptLine>(context)
{
    public Task<List<ReceiptSpendRecord>> GetPostedSpendSinceAsync(DateTime fromUtc, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Where(line => line.Receipt.Status == "Posted" && line.Receipt.CreatedAt >= fromUtc)
            .Select(line => new ReceiptSpendRecord(line.Receipt.CreatedAt, line.LineTotal)).ToListAsync(cancellationToken);

    public Task<Dictionary<string, decimal>> GetAverageUnitCostsAsync(CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().Where(line => line.Receipt.Status == "Posted")
            .GroupBy(line => line.BookId)
            .Select(group => new { BookId = group.Key, Quantity = group.Sum(line => line.Quantity), Total = group.Sum(line => line.LineTotal) })
            .ToDictionaryAsync(item => item.BookId, item => item.Quantity == 0 ? 0m : item.Total / item.Quantity, cancellationToken);
}

public sealed record ReceiptSpendRecord(DateTime CreatedAt, decimal LineTotal);
