using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrangSach.DAO.Entities;

public sealed class OrderPromotionDao(BookStoreDbContext context) : EntityDao<OrderPromotion>(context)
{
    public async Task<decimal> GetBestActiveDiscountAsync(decimal minimumAmount, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        await Query.Where(item => item.MinimumOrderAmount <= minimumAmount
                && item.Promotion.StartsAt <= nowUtc && item.Promotion.EndsAt >= nowUtc)
            .Select(item => (decimal?)item.DiscountPercent).MaxAsync(cancellationToken) ?? 0m;

    public Task<decimal> GetBestDiscountAsync(decimal minimumAmount, DateTime nowUtc, CancellationToken cancellationToken = default) =>
        GetBestActiveDiscountAsync(minimumAmount, nowUtc, cancellationToken);
}
