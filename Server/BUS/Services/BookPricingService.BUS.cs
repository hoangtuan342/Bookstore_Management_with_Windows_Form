using System;
using System.Collections.Generic;
using System.Linq;
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

        public Dictionary<string, decimal> GetActiveDiscounts(
            List<string> bookIds,
            DateTime nowUtc)
        {
            var detailed = GetActivePromotionDetails(bookIds, nowUtc);
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in detailed)
            {
                result[kvp.Key] = kvp.Value.DiscountPercent;
            }
            return result;
        }

        public Dictionary<string, (decimal DiscountPercent, string PromoName, string PromoType)> GetActivePromotionDetails(
            List<string> bookIds,
            DateTime nowUtc)
        {
            if (_promotions.Count == 0)
                _promotions = _dao.Promotions.GetAllPromotions();

            var result = new Dictionary<string, (decimal, string, string)>(StringComparer.OrdinalIgnoreCase);
            var active = _promotions.Where(p => p.StartsAt <= nowUtc && p.EndsAt >= nowUtc).ToList();
            foreach (var bookId in bookIds)
            {
                decimal best = 0;
                string promoName = "";
                foreach (var p in active)
                {
                    var rule = p.BookRules.FirstOrDefault(r => string.Equals(r.BookId, bookId, StringComparison.OrdinalIgnoreCase));
                    if (rule != null && rule.DiscountPercent > best)
                    {
                        best = rule.DiscountPercent;
                        promoName = p.Name;
                    }
                }
                if (best > 0)
                {
                    result[bookId] = (best, promoName, "Theo sản phẩm (% chiết khấu)");
                }
            }
            return result;
        }

        /// <summary>
        /// Tính % giảm giá theo đơn hàng (theo ngưỡng tổng tiền) kèm thông tin CTKM và ngưỡng tối thiểu áp dụng.
        /// </summary>
        public (decimal DiscountPercent, string PromoName, decimal MinOrderAmount) GetOrderDiscountDetails(decimal subtotal, DateTime nowUtc)
        {
            if (_promotions.Count == 0)
                _promotions = _dao.Promotions.GetAllPromotions();

            var active = _promotions.Where(p => p.StartsAt <= nowUtc && p.EndsAt >= nowUtc);
            decimal best = 0;
            string bestPromo = "";
            decimal minOrder = 0;
            foreach (var promo in active)
            {
                foreach (var rule in promo.OrderRules)
                {
                    if (subtotal >= rule.MinimumOrderAmount && rule.DiscountPercent > best)
                    {
                        best = rule.DiscountPercent;
                        bestPromo = promo.Name;
                        minOrder = rule.MinimumOrderAmount;
                    }
                }
            }
            return (best, bestPromo, minOrder);
        }

        public decimal GetOrderDiscount(decimal subtotal, DateTime nowUtc) => GetOrderDiscountDetails(subtotal, nowUtc).DiscountPercent;

        /// <summary>
        /// Tính % giảm giá theo voucher kèm thông tin CTKM.
        /// </summary>
        public (decimal DiscountPercent, string PromoName) GetVoucherDiscountDetails(string voucherId, DateTime nowUtc)
        {
            if (_promotions.Count == 0)
                _promotions = _dao.Promotions.GetAllPromotions();

            var active = _promotions.Where(p => p.StartsAt <= nowUtc && p.EndsAt >= nowUtc);
            decimal best = 0;
            string bestPromo = "";
            foreach (var promo in active)
            {
                var rule = promo.VoucherRules.FirstOrDefault(r =>
                    string.Equals(r.VoucherId, voucherId, StringComparison.OrdinalIgnoreCase));
                if (rule != null && rule.DiscountPercent > best)
                {
                    best = rule.DiscountPercent;
                    bestPromo = promo.Name;
                }
            }
            return (best, bestPromo);
        }

        public decimal GetVoucherDiscount(string voucherId, DateTime nowUtc) => GetVoucherDiscountDetails(voucherId, nowUtc).DiscountPercent;

        public static decimal ApplyDiscount(decimal price, decimal discountPercent)
            => decimal.Round(price * (100m - discountPercent) / 100m, 2, MidpointRounding.AwayFromZero);
    }
}
