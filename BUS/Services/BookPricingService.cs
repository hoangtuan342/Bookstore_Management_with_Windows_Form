using TrangSach.DAO.Entities;

namespace TrangSach.BUS.Services;

/// <summary>Business rules for calculating book prices and active discounts.</summary>
public sealed class BookPricingService(BookStoreDao dao)
{
    public async Task<IReadOnlyDictionary<string, decimal>> GetActiveDiscountsAsync(
        IReadOnlyCollection<string> bookIds,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await dao.BookPromotions.GetActiveDiscountsAsync(bookIds, nowUtc, cancellationToken);
    }

    public static decimal ApplyDiscount(decimal price, decimal discountPercent) =>
        decimal.Round(price * (100m - discountPercent) / 100m, 2, MidpointRounding.AwayFromZero);
}
