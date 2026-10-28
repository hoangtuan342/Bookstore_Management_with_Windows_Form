using BookStore.Shared.Models;
using System.Globalization;

namespace TrangSach.Management.Forms;

public partial class ManagementForm
{
    private async Task CreatePromotionAsync()
    {
        var values = EditDialog.Show(this, "Tạo khuyến mãi", [("name", "Tên chương trình", ""),
            ("start", "Bắt đầu (yyyy-MM-dd)", DateTime.Today.ToString("yyyy-MM-dd")), ("end", "Kết thúc (yyyy-MM-dd)", DateTime.Today.AddDays(7).ToString("yyyy-MM-dd")),
            ("book", "Mã sách (bỏ trống nếu không giảm sách)", ""), ("bookRate", "% giảm sách", ""),
            ("minimum", "Đơn tối thiểu", ""), ("orderRate", "% giảm đơn", ""), ("voucher", "Mã voucher (bỏ trống nếu không tạo)", ""),
            ("voucherRate", "% giảm voucher", ""), ("maxUses", "Số lượt tối đa", "")]);
        if (values is null) return;
        if (!DateTime.TryParse(values["start"], out var start) || !DateTime.TryParse(values["end"], out var end) || end < start) throw new InvalidOperationException("Ngày khuyến mãi không hợp lệ.");
        decimal? dec(string key) => decimal.TryParse(values[key], NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;
        int? integer(string key) => int.TryParse(values[key], out var n) ? n : null;
        await _services.Manager.CreatePromotionAsync(new CreatePromotion(values["name"], start, end, EmptyToNull(values["book"]), dec("bookRate"), dec("minimum"), dec("orderRate"), EmptyToNull(values["voucher"]), dec("voucherRate"), integer("maxUses")));
        await LoadPromotionsAsync();
    }

    private async Task EditPromotionAsync()
    {
        var old = Selected<ManagerPromotion>(_promotionsGrid) ?? throw new InvalidOperationException("Chọn khuyến mãi.");
        var values = EditDialog.Show(this, "Sửa khuyến mãi", [("name", "Tên", old.Name), ("start", "Bắt đầu", old.StartsAt.ToString("yyyy-MM-dd")), ("end", "Kết thúc", old.EndsAt.ToString("yyyy-MM-dd"))]);
        if (values is null) return;
        if (!DateTime.TryParse(values["start"], out var start) || !DateTime.TryParse(values["end"], out var end)) throw new InvalidOperationException("Ngày không hợp lệ.");
        await _services.Manager.UpdatePromotionAsync(old.Id, new UpdatePromotionDetails(values["name"], start, end)); await LoadPromotionsAsync();
    }

    private async Task DeletePromotionAsync()
    {
        var item = Selected<ManagerPromotion>(_promotionsGrid) ?? throw new InvalidOperationException("Chọn khuyến mãi.");
        if (Confirm($"Xóa chương trình {item.Name}?")) { await _services.Manager.DeletePromotionAsync(item.Id); await LoadPromotionsAsync(); }
    }

    private async Task EditVoucherAsync()
    {
        var old = Selected<ManagerVoucher>(_vouchersGrid) ?? throw new InvalidOperationException("Chọn voucher.");
        var values = EditDialog.Show(this, "Cập nhật voucher", [("status", "Active / Inactive", old.Status), ("expires", "Hạn dùng (yyyy-MM-dd, trống = không hạn)", old.ExpiresAt?.ToString("yyyy-MM-dd") ?? ""), ("max", "Số lượt tối đa (trống = không giới hạn)", old.MaxUsageCount?.ToString() ?? "")]);
        if (values is null) return;
        DateTime? expires = DateTime.TryParse(values["expires"], out var date) ? date : null;
        int? max = int.TryParse(values["max"], out var count) ? count : null;
        await _services.Manager.UpdateVoucherAsync(old.Id, new UpdateVoucher(values["status"], expires, max)); await LoadVouchersAsync();
    }

    private async Task DeleteVoucherAsync()
    {
        var item = Selected<ManagerVoucher>(_vouchersGrid) ?? throw new InvalidOperationException("Chọn voucher.");
        if (Confirm($"Xóa voucher {item.Id}?")) { await _services.Manager.DeleteVoucherAsync(item.Id); await LoadVouchersAsync(); }
    }
}

