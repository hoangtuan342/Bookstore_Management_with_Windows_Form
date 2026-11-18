using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
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
        public IReadOnlyList<ManagerPromotionDto> GetPromotions() => _promotions;

        public async Task<IReadOnlyList<ManagerPromotionDto>> GetAsync(CancellationToken ct = default)
        {
            if (_promotions.Count == 0)
                _promotions = await dao.Promotions.GetAllPromotionsAsync(ct).ConfigureAwait(false);
            return _promotions;
        }

        // ---- CREATE ----
        public async Task<OperationResult<string>> CreateAsync(CreatePromotionDto command, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length < 2 || command.Name.Trim().Length > 180
                || (command.BookDiscountPercent != null && (command.BookDiscountPercent < 0 || command.BookDiscountPercent > 90))
                || (command.OrderDiscountPercent != null && (command.OrderDiscountPercent < 0 || command.OrderDiscountPercent > 90))
                || (command.VoucherDiscountPercent != null && (command.VoucherDiscountPercent < 0 || command.VoucherDiscountPercent > 90))
                || (command.MinimumOrderAmount != null && (command.MinimumOrderAmount < 0 || command.MinimumOrderAmount > 999999999))
                || (command.VoucherCode != null && command.VoucherCode.Length > 40)
                || (command.VoucherMaxUses != null && (command.VoucherMaxUses < 1 || command.VoucherMaxUses > 1000000)))
            {
                return OperationResult<string>.Invalid("Thông tin chương trình khuyến mãi không hợp lệ.");
            }
            if (command.EndsAt <= command.StartsAt)
                return OperationResult<string>.Invalid("Thời điểm kết thúc phải sau thời điểm bắt đầu.");

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

            await dao.Promotions.InsertPromotionAsync(id, command.Name.Trim(), command.StartsAt, command.EndsAt, bookRule, orderRule, voucherRule, ct).ConfigureAwait(false);

            // Reload danh sách trong RAM
            _promotions = await dao.Promotions.GetAllPromotionsAsync(ct).ConfigureAwait(false);
            return OperationResult<string>.Created(id);
        }

        // ---- UPDATE DETAILS ----
        public async Task<OperationResult<Unit>> UpdateDetailsAsync(string id, UpdatePromotionDetailsDto command, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length < 2 || command.Name.Trim().Length > 180)
                return OperationResult<Unit>.Invalid("Tên chương trình khuyến mãi không hợp lệ.");
            if (command.EndsAt <= command.StartsAt)
                return OperationResult<Unit>.Invalid("Thời điểm kết thúc phải sau thời điểm bắt đầu.");

            if (_promotions.Count == 0) await GetAsync(ct).ConfigureAwait(false);
            var cached = _promotions.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            await dao.Promotions.UpdatePromotionDetailsAsync(id, command.Name.Trim(), command.StartsAt, command.EndsAt, ct).ConfigureAwait(false);

            cached.Name = command.Name.Trim();
            cached.StartsAt = command.StartsAt;
            cached.EndsAt = command.EndsAt;
            return OperationResult<Unit>.Success(new Unit());
        }

        public Task<OperationResult<Unit>> UpdateAsync(string id, UpdatePromotionDetailsDto command, CancellationToken ct = default)
            => UpdateDetailsAsync(id, command, ct);

        // ---- UPDATE RULES ----
        public async Task<OperationResult<Unit>> UpdateRulesAsync(string id, UpdatePromotionRulesDto command, CancellationToken ct = default)
        {
            if (_promotions.Count == 0) await GetAsync(ct).ConfigureAwait(false);
            var cached = _promotions.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            if (command.Books.Any(x => string.IsNullOrWhiteSpace(x.BookId) || x.DiscountPercent <= 0 || x.DiscountPercent > 90)
                || command.Orders.Any(x => x.MinimumOrderAmount <= 0 || x.DiscountPercent <= 0 || x.DiscountPercent > 90)
                || command.Vouchers.Any(x => string.IsNullOrWhiteSpace(x.VoucherId) || x.DiscountPercent <= 0 || x.DiscountPercent > 90))
            {
                return OperationResult<Unit>.Invalid("Quy tắc cần có mã hợp lệ, ngưỡng dương và mức giảm từ trên 0 đến 90%.");
            }

            await dao.Promotions.UpdatePromotionRulesAsync(id, command.Books, command.Orders, command.Vouchers, ct).ConfigureAwait(false);

            _promotions = await dao.Promotions.GetAllPromotionsAsync(ct).ConfigureAwait(false);
            return OperationResult<Unit>.Success(new Unit());
        }

        // ---- DELETE ----
        public async Task<OperationResult<Unit>> DeleteAsync(string id, CancellationToken ct = default)
        {
            if (_promotions.Count == 0) await GetAsync(ct).ConfigureAwait(false);
            var cached = _promotions.FirstOrDefault(x => x.Id == id);
            if (cached == null) return OperationResult<Unit>.NotFound();

            await dao.Promotions.DeletePromotionAsync(id, ct).ConfigureAwait(false);
            _promotions.RemoveAll(x => x.Id == id);
            return OperationResult<Unit>.Success(new Unit());
        }
    }
}
