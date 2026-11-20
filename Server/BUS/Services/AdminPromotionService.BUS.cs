using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    public class AdminPromotionService
    {
        private readonly BookStoreDao dao;

        // Danh sách khuyến mãi được load vào RAM
        private List<ManagerPromotionDto> _promotions = new();

        public AdminPromotionService(BookStoreDao dao)
        {
            this.dao = dao;
        }

        // ---- READ ----
        public List<ManagerPromotionDto> GetPromotions() => _promotions;

        // ----------------------------------------------------------------
        // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
        // ----------------------------------------------------------------
        public List<ManagerPromotionDto> GetAllPromotionsList()
        {
            if (_promotions.Count == 0) _promotions = dao.Promotions.GetAllPromotions();
            return _promotions;
        }

        public string Create(CreatePromotionDto command)
        {
            if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length < 2 || command.Name.Trim().Length > 180
                || (command.BookDiscountPercent != null && (command.BookDiscountPercent < 0 || command.BookDiscountPercent > 90))
                || (command.OrderDiscountPercent != null && (command.OrderDiscountPercent < 0 || command.OrderDiscountPercent > 90))
                || (command.VoucherDiscountPercent != null && (command.VoucherDiscountPercent < 0 || command.VoucherDiscountPercent > 90))
                || (command.MinimumOrderAmount != null && (command.MinimumOrderAmount < 0 || command.MinimumOrderAmount > 999999999))
                || (command.VoucherCode != null && command.VoucherCode.Length > 40)
                || (command.VoucherMaxUses != null && (command.VoucherMaxUses < 1 || command.VoucherMaxUses > 1000000)))
            {
                throw new ArgumentException("Thông tin chương trình khuyến mãi không hợp lệ.");
            }
            if (command.EndsAt <= command.StartsAt)
                throw new ArgumentException("Thời điểm kết thúc phải sau thời điểm bắt đầu.");

            var id = $"KM{DateTime.UtcNow:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(2))}";

            (string BookId, decimal DiscountPercent)? bookRule = null;
            if (command.BookDiscountPercent != null && command.BookDiscountPercent > 0 && !string.IsNullOrWhiteSpace(command.BookId))
                bookRule = (command.BookId.Trim(), command.BookDiscountPercent.Value);

            (decimal MinOrder, decimal DiscountPercent)? orderRule = null;
            if (command.MinimumOrderAmount != null && command.MinimumOrderAmount > 0 && command.OrderDiscountPercent != null && command.OrderDiscountPercent > 0)
                orderRule = (command.MinimumOrderAmount.Value, command.OrderDiscountPercent.Value);

            (string Code, decimal DiscountPercent, int? MaxUses)? voucherRule = null;
            if (!string.IsNullOrWhiteSpace(command.VoucherCode) && command.VoucherDiscountPercent != null && command.VoucherDiscountPercent > 0)
                voucherRule = (command.VoucherCode.Trim(), command.VoucherDiscountPercent.Value, command.VoucherMaxUses);

            dao.Promotions.InsertPromotion(id, command.Name.Trim(), command.StartsAt, command.EndsAt, bookRule, orderRule, voucherRule);

            // Nếu truyền danh sách quy tắc chi tiết phong phú (Master-Detail)
            if ((command.BookRules != null && command.BookRules.Count > 0) ||
                (command.OrderRules != null && command.OrderRules.Count > 0) ||
                (command.VoucherRules != null && command.VoucherRules.Count > 0))
            {
                dao.Promotions.UpdatePromotionRules(id,
                    command.BookRules ?? new List<PromotionBookRuleDto>(),
                    command.OrderRules ?? new List<PromotionOrderRuleDto>(),
                    command.VoucherRules ?? new List<PromotionVoucherRuleDto>());
            }

            _promotions = dao.Promotions.GetAllPromotions();
            return id;
        }

        public void UpdateDetails(string id, UpdatePromotionDetailsDto command)
        {
            if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length < 2 || command.Name.Trim().Length > 180)
                throw new ArgumentException("Tên chương trình khuyến mãi không hợp lệ.");
            if (command.EndsAt <= command.StartsAt)
                throw new ArgumentException("Thời điểm kết thúc phải sau thời điểm bắt đầu.");

            if (_promotions.Count == 0) GetAllPromotionsList();
            var cached = _promotions.FirstOrDefault(x => x.Id == id);
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy chương trình khuyến mãi.");

            dao.Promotions.UpdatePromotionDetails(id, command.Name.Trim(), command.StartsAt, command.EndsAt);
            cached.Name = command.Name.Trim();
            cached.StartsAt = command.StartsAt;
            cached.EndsAt = command.EndsAt;
        }

        public void Update(string id, UpdatePromotionDetailsDto command) => UpdateDetails(id, command);

        public void UpdateRules(string id, UpdatePromotionRulesDto command)
        {
            if (_promotions.Count == 0) GetAllPromotionsList();
            var cached = _promotions.FirstOrDefault(x => x.Id == id);
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy chương trình khuyến mãi.");

            if (command.Books.Any(x => string.IsNullOrWhiteSpace(x.BookId) || x.DiscountPercent <= 0 || x.DiscountPercent > 90)
                || command.Orders.Any(x => x.MinimumOrderAmount <= 0 || x.DiscountPercent <= 0 || x.DiscountPercent > 90)
                || command.Vouchers.Any(x => string.IsNullOrWhiteSpace(x.VoucherId) || x.DiscountPercent <= 0 || x.DiscountPercent > 90))
            {
                throw new ArgumentException("Quy tắc cần có mã hợp lệ, ngưỡng dương và mức giảm từ trên 0 đến 90%.");
            }

            dao.Promotions.UpdatePromotionRules(id, command.Books, command.Orders, command.Vouchers);
            _promotions = dao.Promotions.GetAllPromotions();
        }

        public void Delete(string id)
        {
            if (_promotions.Count == 0) GetAllPromotionsList();
            var cached = _promotions.FirstOrDefault(x => x.Id == id);
            if (cached == null) throw new KeyNotFoundException("Không tìm thấy chương trình khuyến mãi.");

            dao.Promotions.DeletePromotion(id);
            _promotions.RemoveAll(x => x.Id == id);
        }
    }
}
