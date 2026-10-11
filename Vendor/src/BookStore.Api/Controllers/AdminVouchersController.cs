using System.ComponentModel.DataAnnotations;
using BookStore.Infrastructure.Persistence;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize(Roles = StaffRoles.ManagerOrMarketing)]
[Route("api/admin/vouchers")]
public sealed class AdminVouchersController(BookStoreDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await db.Vouchers.AsNoTracking()
        .OrderBy(x => x.Id).Select(x => new VoucherRow(x.Id, x.Status, x.ExpiresAt, x.MaxUsageCount, x.UsageCount,
            x.Promotions.Select(p => $"{p.Promotion.Name}: {p.DiscountPercent}%").ToArray(), x.Orders.Count))
        .ToListAsync(ct));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, VoucherUpdateRequest request, CancellationToken ct)
    {
        var item = await db.Vouchers.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (request.MaxUsageCount is not null && request.MaxUsageCount < item.UsageCount)
            return BadRequest(new { message = "Giới hạn lượt dùng không được nhỏ hơn số lượt đã sử dụng." });
        item.Status = request.Status.Trim();
        item.ExpiresAt = request.ExpiresAt;
        item.MaxUsageCount = request.MaxUsageCount;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var item = await db.Vouchers.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (await db.Orders.AnyAsync(x => x.VoucherId == id, ct)) return Conflict(new { message = "Voucher đã được dùng trong đơn hàng; hãy chuyển trạng thái sang ngừng sử dụng." });
        db.Vouchers.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record VoucherRow(string Id, string Status, DateTime? ExpiresAt, int? MaxUsageCount, int UsageCount, IReadOnlyList<string> Promotions, int OrderCount);
public sealed class VoucherUpdateRequest
{
    [Required, StringLength(30)] public string Status { get; init; } = "Active";
    public DateTime? ExpiresAt { get; init; }
    [Range(1, 1000000)] public int? MaxUsageCount { get; init; }
}
