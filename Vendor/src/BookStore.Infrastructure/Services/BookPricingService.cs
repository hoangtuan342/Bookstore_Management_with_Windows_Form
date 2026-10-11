using Microsoft.EntityFrameworkCore;
using BookStore.Infrastructure.Persistence;

namespace BookStore.Infrastructure.Services;

public sealed class BookPricingService(BookStoreDbContext db)
{
    public async Task<IReadOnlyDictionary<string, decimal>> GetActiveDiscountsAsync(
        IReadOnlyCollection<string> bookIds,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (bookIds.Count == 0)
        {
            return new Dictionary<string, decimal>();
        }

        return await db.BookPromotions.AsNoTracking()
            .Where(item => bookIds.Contains(item.BookId)
                && item.Promotion.StartsAt <= nowUtc
                && item.Promotion.EndsAt >= nowUtc)
            .GroupBy(item => item.BookId)
            .Select(group => new { BookId = group.Key, DiscountPercent = group.Max(item => item.DiscountPercent) })
            .ToDictionaryAsync(item => item.BookId, item => item.DiscountPercent, cancellationToken);
    }

    public static decimal ApplyDiscount(decimal price, decimal discountPercent) =>
        decimal.Round(price * (100m - discountPercent) / 100m, 2, MidpointRounding.AwayFromZero);
}
