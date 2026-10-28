using TrangSach.BUS.Models;
using TrangSach.BUS.Results;
using TrangSach.DAO.Entities;

namespace TrangSach.BUS.Services;

public sealed class AdminVoucherService(BookStoreDao dao)
{
    public async Task<IReadOnlyList<VoucherSummary>> GetAsync(CancellationToken cancellationToken = default)
    {
        var vouchers = await dao.Vouchers.GetAllWithUsageAsync(cancellationToken);
        return vouchers.Select(x => new VoucherSummary(x.Id, x.Status, x.ExpiresAt, x.MaxUsageCount, x.UsageCount,
            x.Promotions.Select(p => $"{p.Promotion.Name}: {p.DiscountPercent}%").ToArray(), x.Orders.Count)).ToArray();
    }

    public async Task<OperationResult<Unit>> UpdateAsync(string id, VoucherUpdateCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Status) || command.Status.Trim().Length > 30
            || command.MaxUsageCount is < 1 or > 1000000)
            return OperationResult<Unit>.Invalid("Trạng thái hoặc giới hạn lượt dùng voucher không hợp lệ.");
        var item = await dao.Vouchers.FindByIdAsync(id, cancellationToken);
        if (item is null) return OperationResult<Unit>.NotFound();
        if (command.MaxUsageCount is not null && command.MaxUsageCount < item.UsageCount)
            return OperationResult<Unit>.Invalid("Giới hạn lượt dùng không được nhỏ hơn số lượt đã sử dụng.");
        item.Status = command.Status.Trim();
        item.ExpiresAt = command.ExpiresAt;
        item.MaxUsageCount = command.MaxUsageCount;
        await dao.SaveChangesAsync(cancellationToken);
        return OperationResult<Unit>.Success(new Unit());
    }

    public async Task<OperationResult<Unit>> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await dao.Vouchers.FindByIdAsync(id, cancellationToken);
        if (item is null) return OperationResult<Unit>.NotFound();
        if (await dao.Vouchers.HasBeenUsedByOrderAsync(id, cancellationToken))
            return OperationResult<Unit>.Conflict("Voucher đã được dùng trong đơn hàng; hãy chuyển trạng thái sang ngừng sử dụng.");
        dao.Vouchers.Remove(item);
        await dao.SaveChangesAsync(cancellationToken);
        return OperationResult<Unit>.Success(new Unit());
    }
}
