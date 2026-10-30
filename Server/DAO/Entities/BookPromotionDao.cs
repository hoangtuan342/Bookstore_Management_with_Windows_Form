using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class BookPromotionDao(BookStoreDbContext context) : EntityDao<BookPromotion>(context)
{
    public async Task<IReadOnlyDictionary<string, decimal>> GetActiveDiscountsAsync(
        IReadOnlyCollection<string> bookIds, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        if (bookIds.Count == 0) return new Dictionary<string, decimal>();
        return await Query.AsNoTracking()
            .Where(item => bookIds.Contains(item.BookId)
                && item.Promotion.StartsAt <= nowUtc && item.Promotion.EndsAt >= nowUtc)
            .GroupBy(item => item.BookId)
            .Select(group => new { BookId = group.Key, DiscountPercent = group.Max(item => item.DiscountPercent) })
            .ToDictionaryAsync(item => item.BookId, item => item.DiscountPercent, cancellationToken);
    }
}
