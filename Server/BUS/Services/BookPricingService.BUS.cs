using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    /// <summary>
    /// Service tính toán giá sách và chiết khấu từ CTKM trong RAM.
    /// </summary>
    public class BookPricingService
    {
        private readonly BookStoreDao _dao;
        private List<ManagerPromotionDto> _promotions = new();

        public BookPricingService(BookStoreDao dao)
        {
            _dao = dao;
        }

        private async Task EnsureLoadedAsync(CancellationToken ct = default)
        {
            if (_promotions.Count == 0)
                _promotions = await _dao.Promotions.GetAllPromotionsAsync(ct).ConfigureAwait(false);
        }

        public async Task<IReadOnlyDictionary<string, decimal>> GetActiveDiscountsAsync(
            IReadOnlyCollection<string> bookIds,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return GetActiveDiscounts(bookIds, nowUtc, cancellationToken);
        }

        public IReadOnlyDictionary<string, decimal> GetActiveDiscounts(
            IReadOnlyCollection<string> bookIds,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            if (_promotions.Count == 0)
                _promotions = _dao.Promotions.GetAllPromotions();

            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var active = _promotions.Where(p => p.StartsAt <= nowUtc && p.EndsAt >= nowUtc).ToList();
            foreach (var bookId in bookIds)
            {
                decimal best = 0;
                foreach (var p in active)
                {
                    var rule = p.BookRules.FirstOrDefault(r => string.Equals(r.BookId, bookId, StringComparison.OrdinalIgnoreCase));
                    if (rule != null && rule.DiscountPercent > best)
                        best = rule.DiscountPercent;
                }
                if (best > 0) result[bookId] = best;
            }
            return result;
        }

        /// <summary>
        /// Tính % giảm giá theo đơn hàng (theo ngưỡng tổng tiền).
        /// </summary>
        public decimal GetOrderDiscount(decimal subtotal, DateTime nowUtc)
        {
            if (_promotions.Count == 0)
                _promotions = _dao.Promotions.GetAllPromotions();

            var active = _promotions.Where(p => p.StartsAt <= nowUtc && p.EndsAt >= nowUtc);
            decimal best = 0;
            foreach (var promo in active)
            {
                foreach (var rule in promo.OrderRules)
                {
                    if (subtotal >= rule.MinimumOrderAmount && rule.DiscountPercent > best)
                        best = rule.DiscountPercent;
                }
            }
            return best;
        }

        /// <summary>
        /// Tính % giảm giá theo voucher.
        /// </summary>
        public decimal GetVoucherDiscount(string voucherId, DateTime nowUtc)
        {
            if (_promotions.Count == 0)
                _promotions = _dao.Promotions.GetAllPromotions();

            var active = _promotions.Where(p => p.StartsAt <= nowUtc && p.EndsAt >= nowUtc);
            decimal best = 0;
            foreach (var promo in active)
            {
                var rule = promo.VoucherRules.FirstOrDefault(r =>
                    string.Equals(r.VoucherId, voucherId, StringComparison.OrdinalIgnoreCase));
                if (rule != null && rule.DiscountPercent > best)
                    best = rule.DiscountPercent;
            }
            return best;
        }

        public static decimal ApplyDiscount(decimal price, decimal discountPercent)
            => decimal.Round(price * (100m - discountPercent) / 100m, 2, MidpointRounding.AwayFromZero);
    }
}
