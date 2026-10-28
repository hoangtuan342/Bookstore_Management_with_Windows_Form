using System.Security.Cryptography;
using BookStore.Domain.Entities;
using BookStore.Shared.Models;
using TrangSach.BUS.Results;
using TrangSach.DAO.Entities;

namespace TrangSach.BUS.Services;

public sealed class AdminPromotionService(BookStoreDao dao)
{
    public async Task<IReadOnlyList<ManagerPromotion>> GetAsync(CancellationToken ct = default)
    {
        var items = await dao.Promotions.GetAllWithRulesAsync(ct);
        return items.Select(x => new ManagerPromotion(x.Id, x.Name, x.StartsAt, x.EndsAt,
            x.Books.Select(y => $"{y.Book.Title}: {y.DiscountPercent}%").ToArray(),
            x.OrderDiscounts.Select(y => $"Từ {y.MinimumOrderAmount:N0}₫: {y.DiscountPercent}%").ToArray(),
            x.Vouchers.Select(y => $"{y.VoucherId}: {y.DiscountPercent}%").ToArray(),
            x.Books.Select(y => new BookStore.Shared.Models.PromotionBookRule(y.BookId, y.DiscountPercent)).ToArray(),
            x.OrderDiscounts.Select(y => new BookStore.Shared.Models.PromotionOrderRule(y.MinimumOrderAmount, y.DiscountPercent)).ToArray(),
            x.Vouchers.Select(y => new BookStore.Shared.Models.PromotionVoucherRule(y.VoucherId, y.DiscountPercent)).ToArray())).ToArray();
    }

    public async Task<OperationResult<string>> CreateAsync(CreatePromotion command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length is < 2 or > 180
            || command.BookDiscountPercent is < 0 or > 90 || command.OrderDiscountPercent is < 0 or > 90
            || command.VoucherDiscountPercent is < 0 or > 90 || command.MinimumOrderAmount is < 0 or > 999999999
            || command.VoucherCode?.Length > 40 || command.VoucherMaxUses is < 1 or > 1000000)
            return OperationResult<string>.Invalid("Thông tin chương trình khuyến mãi không hợp lệ.");
        if (command.EndsAt <= command.StartsAt) return OperationResult<string>.Invalid("Thời điểm kết thúc phải sau thời điểm bắt đầu.");
        if (command.BookDiscountPercent is > 0 && (string.IsNullOrWhiteSpace(command.BookId)
            || !await dao.Books.ExistsAsync(x => x.Id == command.BookId, ct))) return OperationResult<string>.Invalid("Mã sách không tồn tại.");
        var item = new Promotion { Id = $"KM{DateTime.UtcNow:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(2))}",
            Name = command.Name.Trim(), StartsAt = DateTime.SpecifyKind(command.StartsAt, DateTimeKind.Utc), EndsAt = DateTime.SpecifyKind(command.EndsAt, DateTimeKind.Utc) };
        if (command.BookDiscountPercent is > 0) item.Books.Add(new BookPromotion { PromotionId = item.Id, BookId = command.BookId!.Trim(), DiscountPercent = command.BookDiscountPercent.Value });
        if (command.MinimumOrderAmount is > 0 && command.OrderDiscountPercent is > 0)
            item.OrderDiscounts.Add(new OrderPromotion { PromotionId = item.Id, MinimumOrderAmount = command.MinimumOrderAmount.Value, DiscountPercent = command.OrderDiscountPercent.Value });
        if (!string.IsNullOrWhiteSpace(command.VoucherCode) && command.VoucherDiscountPercent is > 0)
        {
            var code = command.VoucherCode.Trim().ToUpperInvariant();
            if (await dao.Vouchers.FindByIdAsync(code, ct) is not null) return OperationResult<string>.Invalid("Mã voucher đã tồn tại.");
            dao.Vouchers.Add(new Voucher { Id = code, Status = "Active", ExpiresAt = item.EndsAt, MaxUsageCount = command.VoucherMaxUses });
            item.Vouchers.Add(new PromotionVoucher { PromotionId = item.Id, VoucherId = code, DiscountPercent = command.VoucherDiscountPercent.Value });
        }
        if (item.Books.Count + item.OrderDiscounts.Count + item.Vouchers.Count == 0)
            return OperationResult<string>.Invalid("Hãy cấu hình ít nhất một ưu đãi sách, đơn hàng hoặc voucher.");
        dao.Promotions.Add(item); await dao.SaveChangesAsync(ct);
        return OperationResult<string>.Created(item.Id);
    }

    public async Task<OperationResult<Unit>> UpdateAsync(string id, UpdatePromotionDetails command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length is < 2 or > 180)
            return OperationResult<Unit>.Invalid("Tên chương trình cần từ 2 đến 180 ký tự.");
        if (command.EndsAt <= command.StartsAt) return OperationResult<Unit>.Invalid("Thời điểm kết thúc phải sau thời điểm bắt đầu.");
        var item = await dao.Promotions.FindSingleAsync(x => x.Id == id, ct);
        if (item is null) return OperationResult<Unit>.NotFound();
        item.Name = command.Name.Trim(); item.StartsAt = DateTime.SpecifyKind(command.StartsAt, DateTimeKind.Utc); item.EndsAt = DateTime.SpecifyKind(command.EndsAt, DateTimeKind.Utc);
        await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<OperationResult<Unit>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var item = await dao.Promotions.FindSingleAsync(x => x.Id == id, ct);
        if (item is null) return OperationResult<Unit>.NotFound();
        dao.Promotions.Remove(item); await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<OperationResult<Unit>> UpdateRulesAsync(string id, UpdatePromotionRules command, CancellationToken ct = default)
    {
        if (command.Books.Count > 1000 || command.Orders.Count > 1000 || command.Vouchers.Count > 1000)
            return OperationResult<Unit>.Invalid("Số lượng quy tắc vượt quá giới hạn.");
        var item = await dao.Promotions.FindWithRulesAsync(id, ct);
        if (item is null) return OperationResult<Unit>.NotFound();
        if (command.Books.Count + command.Orders.Count + command.Vouchers.Count == 0) return OperationResult<Unit>.Invalid("Chương trình cần có ít nhất một quy tắc giảm giá.");
        if (command.Books.Any(x => string.IsNullOrWhiteSpace(x.BookId) || x.DiscountPercent is <= 0 or > 90)
            || command.Orders.Any(x => x.MinimumOrderAmount <= 0 || x.DiscountPercent is <= 0 or > 90)
            || command.Vouchers.Any(x => string.IsNullOrWhiteSpace(x.VoucherId) || x.DiscountPercent is <= 0 or > 90))
            return OperationResult<Unit>.Invalid("Quy tắc cần có mã hợp lệ, ngưỡng dương và mức giảm từ trên 0 đến 90%.");
        var books = command.Books.Select(x => new BookStore.Shared.Models.PromotionBookRule(x.BookId.Trim(), x.DiscountPercent)).ToArray();
        var vouchers = command.Vouchers.Select(x => new BookStore.Shared.Models.PromotionVoucherRule(x.VoucherId.Trim().ToUpperInvariant(), x.DiscountPercent)).ToArray();
        if (books.GroupBy(x => x.BookId, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1)
            || command.Orders.GroupBy(x => x.MinimumOrderAmount).Any(x => x.Count() > 1)
            || vouchers.GroupBy(x => x.VoucherId, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            return OperationResult<Unit>.Invalid("Không được khai báo trùng sách, ngưỡng đơn hoặc voucher.");
        if (await dao.Books.CountByIdsAsync(books.Select(x => x.BookId).ToArray(), ct) != books.Select(x => x.BookId).Distinct().Count()
            || await dao.Vouchers.CountByIdsAsync(vouchers.Select(x => x.VoucherId).ToArray(), ct) != vouchers.Select(x => x.VoucherId).Distinct().Count())
            return OperationResult<Unit>.Invalid("Sách hoặc voucher trong quy tắc không tồn tại.");
        var bookRules = books.ToDictionary(x => x.BookId, StringComparer.OrdinalIgnoreCase);
        foreach (var old in item.Books.ToArray()) { if (bookRules.Remove(old.BookId, out var update)) old.DiscountPercent = update.DiscountPercent; else dao.BookPromotions.Remove(old); }
        foreach (var rule in bookRules.Values) item.Books.Add(new BookPromotion { PromotionId = item.Id, BookId = rule.BookId, DiscountPercent = rule.DiscountPercent });
        var orderRules = command.Orders.ToDictionary(x => x.MinimumOrderAmount);
        foreach (var old in item.OrderDiscounts.ToArray()) { if (orderRules.Remove(old.MinimumOrderAmount, out var update)) old.DiscountPercent = update.DiscountPercent; else dao.OrderPromotions.Remove(old); }
        foreach (var rule in orderRules.Values) item.OrderDiscounts.Add(new OrderPromotion { PromotionId = item.Id, MinimumOrderAmount = rule.MinimumOrderAmount, DiscountPercent = rule.DiscountPercent });
        var voucherRules = vouchers.ToDictionary(x => x.VoucherId, StringComparer.OrdinalIgnoreCase);
        foreach (var old in item.Vouchers.ToArray()) { if (voucherRules.Remove(old.VoucherId, out var update)) old.DiscountPercent = update.DiscountPercent; else dao.PromotionVouchers.Remove(old); }
        foreach (var rule in voucherRules.Values) item.Vouchers.Add(new PromotionVoucher { PromotionId = item.Id, VoucherId = rule.VoucherId, DiscountPercent = rule.DiscountPercent });
        await dao.SaveChangesAsync(ct); return OperationResult<Unit>.Success(new Unit());
    }
}
