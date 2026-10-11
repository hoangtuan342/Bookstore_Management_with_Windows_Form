using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize(Roles = StaffRoles.ManagerOrMarketing)]
[Route("api/admin/promotions")]
public sealed class AdminPromotionsController(BookStoreDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => Ok(await db.Promotions.AsNoTracking()
        .Include(item => item.Books).ThenInclude(item => item.Book)
        .Include(item => item.OrderDiscounts)
        .Include(item => item.Vouchers).ThenInclude(item => item.Voucher)
        .OrderByDescending(item => item.StartsAt)
        .Select(item => new PromotionRow(item.Id, item.Name, item.StartsAt, item.EndsAt,
            item.Books.Select(link => $"{link.Book.Title}: {link.DiscountPercent}%").ToArray(),
            item.OrderDiscounts.Select(link => $"Từ {link.MinimumOrderAmount:N0}₫: {link.DiscountPercent}%").ToArray(),
            item.Vouchers.Select(link => $"{link.VoucherId}: {link.DiscountPercent}%").ToArray(),
            item.Books.Select(link => new PromotionBookRule { BookId = link.BookId, DiscountPercent = link.DiscountPercent }).ToArray(),
            item.OrderDiscounts.Select(link => new PromotionOrderRule { MinimumOrderAmount = link.MinimumOrderAmount, DiscountPercent = link.DiscountPercent }).ToArray(),
            item.Vouchers.Select(link => new PromotionVoucherRule { VoucherId = link.VoucherId, DiscountPercent = link.DiscountPercent }).ToArray()))
        .ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        if (request.EndsAt <= request.StartsAt)
        {
            return BadRequest(new { message = "Thời điểm kết thúc phải sau thời điểm bắt đầu." });
        }
        if (request.BookDiscountPercent is > 0 && (string.IsNullOrWhiteSpace(request.BookId)
            || !await db.Books.AnyAsync(book => book.Id == request.BookId, cancellationToken)))
        {
            return BadRequest(new { message = "Mã sách không tồn tại." });
        }

        var promotion = new Promotion
        {
            Id = $"KM{DateTime.UtcNow:yyyyMMddHHmmss}{Convert.ToHexString(RandomNumberGenerator.GetBytes(2))}",
            Name = request.Name.Trim(),
            StartsAt = DateTime.SpecifyKind(request.StartsAt, DateTimeKind.Utc),
            EndsAt = DateTime.SpecifyKind(request.EndsAt, DateTimeKind.Utc)
        };
        if (request.BookDiscountPercent is > 0)
            promotion.Books.Add(new BookPromotion { PromotionId = promotion.Id, BookId = request.BookId!.Trim(), DiscountPercent = request.BookDiscountPercent.Value });
        if (request.MinimumOrderAmount is > 0 && request.OrderDiscountPercent is > 0)
            promotion.OrderDiscounts.Add(new OrderPromotion { PromotionId = promotion.Id, MinimumOrderAmount = request.MinimumOrderAmount.Value, DiscountPercent = request.OrderDiscountPercent.Value });
        if (!string.IsNullOrWhiteSpace(request.VoucherCode) && request.VoucherDiscountPercent is > 0)
        {
            var code = request.VoucherCode.Trim().ToUpperInvariant();
            var voucher = await db.Vouchers.SingleOrDefaultAsync(item => item.Id == code, cancellationToken);
            if (voucher is null)
            {
                voucher = new Voucher { Id = code, Status = "Active", ExpiresAt = promotion.EndsAt, MaxUsageCount = request.VoucherMaxUses };
                db.Vouchers.Add(voucher);
            }
            else
            {
                return BadRequest(new { message = "Mã voucher đã tồn tại." });
            }
            promotion.Vouchers.Add(new PromotionVoucher { PromotionId = promotion.Id, VoucherId = code, DiscountPercent = request.VoucherDiscountPercent.Value });
        }
        if (promotion.Books.Count + promotion.OrderDiscounts.Count + promotion.Vouchers.Count == 0)
            return BadRequest(new { message = "Hãy cấu hình ít nhất một ưu đãi sách, đơn hàng hoặc voucher." });

        db.Promotions.Add(promotion);
        await db.SaveChangesAsync(cancellationToken);
        return Created("api/admin/promotions", new { promotion.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, PromotionDetailsRequest request, CancellationToken cancellationToken)
    {
        if (request.EndsAt <= request.StartsAt) return BadRequest(new { message = "Thời điểm kết thúc phải sau thời điểm bắt đầu." });
        var item = await db.Promotions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();
        item.Name = request.Name.Trim();
        item.StartsAt = DateTime.SpecifyKind(request.StartsAt, DateTimeKind.Utc);
        item.EndsAt = DateTime.SpecifyKind(request.EndsAt, DateTimeKind.Utc);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var item = await db.Promotions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();
        db.Promotions.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("{id}/rules")]
    public async Task<IActionResult> UpdateRules(string id, PromotionRulesRequest request, CancellationToken cancellationToken)
    {
        var item = await db.Promotions.Include(x => x.Books).Include(x => x.OrderDiscounts).Include(x => x.Vouchers)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();
        if (request.Books.Count + request.Orders.Count + request.Vouchers.Count == 0)
            return BadRequest(new { message = "Chương trình cần có ít nhất một quy tắc giảm giá." });
        if (request.Books.Any(x => string.IsNullOrWhiteSpace(x.BookId) || x.DiscountPercent is <= 0 or > 90)
            || request.Orders.Any(x => x.MinimumOrderAmount <= 0 || x.DiscountPercent is <= 0 or > 90)
            || request.Vouchers.Any(x => string.IsNullOrWhiteSpace(x.VoucherId) || x.DiscountPercent is <= 0 or > 90))
            return BadRequest(new { message = "Quy tắc cần có mã hợp lệ, ngưỡng dương và mức giảm từ trên 0 đến 90%." });
        if (request.Books.GroupBy(x => x.BookId.Trim(), StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1) || request.Orders.GroupBy(x => x.MinimumOrderAmount).Any(x => x.Count() > 1) || request.Vouchers.GroupBy(x => x.VoucherId.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            return BadRequest(new { message = "Không được khai báo trùng sách, ngưỡng đơn hoặc voucher." });
        foreach (var rule in request.Books) rule.BookId = rule.BookId.Trim();
        foreach (var rule in request.Vouchers) rule.VoucherId = rule.VoucherId.Trim().ToUpperInvariant();
        var bookIds = request.Books.Select(x => x.BookId).ToArray();
        var voucherIds = request.Vouchers.Select(x => x.VoucherId).ToArray();
        if (await db.Books.CountAsync(x => bookIds.Contains(x.Id), cancellationToken) != bookIds.Distinct().Count()
            || await db.Vouchers.CountAsync(x => voucherIds.Contains(x.Id), cancellationToken) != voucherIds.Distinct().Count())
            return BadRequest(new { message = "Sách hoặc voucher trong quy tắc không tồn tại." });
        var bookRules = request.Books.ToDictionary(x => x.BookId, StringComparer.OrdinalIgnoreCase);
        foreach (var old in item.Books.ToArray())
        {
            if (bookRules.Remove(old.BookId, out var update)) old.DiscountPercent = update.DiscountPercent;
            else db.BookPromotions.Remove(old);
        }
        foreach (var rule in bookRules.Values) item.Books.Add(new BookPromotion { PromotionId = item.Id, BookId = rule.BookId, DiscountPercent = rule.DiscountPercent });
        var orderRules = request.Orders.ToDictionary(x => x.MinimumOrderAmount);
        foreach (var old in item.OrderDiscounts.ToArray())
        {
            if (orderRules.Remove(old.MinimumOrderAmount, out var update)) old.DiscountPercent = update.DiscountPercent;
            else db.OrderPromotions.Remove(old);
        }
        foreach (var rule in orderRules.Values) item.OrderDiscounts.Add(new OrderPromotion { PromotionId = item.Id, MinimumOrderAmount = rule.MinimumOrderAmount, DiscountPercent = rule.DiscountPercent });
        var voucherRules = request.Vouchers.ToDictionary(x => x.VoucherId, StringComparer.OrdinalIgnoreCase);
        foreach (var old in item.Vouchers.ToArray())
        {
            if (voucherRules.Remove(old.VoucherId, out var update)) old.DiscountPercent = update.DiscountPercent;
            else db.PromotionVouchers.Remove(old);
        }
        foreach (var rule in voucherRules.Values) item.Vouchers.Add(new PromotionVoucher { PromotionId = item.Id, VoucherId = rule.VoucherId, DiscountPercent = rule.DiscountPercent });
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed class CreatePromotionRequest
{
    [Required, StringLength(180, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public DateTime EndsAt { get; init; }
    public string? BookId { get; init; }
    [Range(typeof(decimal), "0", "90")] public decimal? BookDiscountPercent { get; init; }
    [Range(typeof(decimal), "0", "999999999")] public decimal? MinimumOrderAmount { get; init; }
    [Range(typeof(decimal), "0", "90")] public decimal? OrderDiscountPercent { get; init; }
    [StringLength(40)] public string? VoucherCode { get; init; }
    [Range(typeof(decimal), "0", "90")] public decimal? VoucherDiscountPercent { get; init; }
    [Range(1, 1000000)] public int? VoucherMaxUses { get; init; }
}

public sealed record PromotionRow(string Id, string Name, DateTime StartsAt, DateTime EndsAt,
    IReadOnlyList<string> BookOffers, IReadOnlyList<string> OrderOffers, IReadOnlyList<string> Vouchers,
    IReadOnlyList<PromotionBookRule> BookRules, IReadOnlyList<PromotionOrderRule> OrderRules, IReadOnlyList<PromotionVoucherRule> VoucherRules);
public sealed class PromotionBookRule { public string BookId { get; set; } = string.Empty; public decimal DiscountPercent { get; set; } }
public sealed class PromotionOrderRule { public decimal MinimumOrderAmount { get; set; } public decimal DiscountPercent { get; set; } }
public sealed class PromotionVoucherRule { public string VoucherId { get; set; } = string.Empty; public decimal DiscountPercent { get; set; } }

public sealed class PromotionDetailsRequest
{
    [Required, StringLength(180, MinimumLength = 2)] public string Name { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public DateTime EndsAt { get; init; }
}

public sealed class PromotionRulesRequest
{
    public IReadOnlyList<PromotionBookRule> Books { get; init; } = [];
    public IReadOnlyList<PromotionOrderRule> Orders { get; init; } = [];
    public IReadOnlyList<PromotionVoucherRule> Vouchers { get; init; } = [];
}
