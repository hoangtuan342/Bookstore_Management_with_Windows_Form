using TrangSach.Shared.DTO;
using TrangSach.Shared.GUI;
using System.Globalization;

namespace TrangSach.Management.GUI;

public partial class ManagementForm
{
    private static readonly List<(string Id, string Name)> VoucherStatusOptions = new()
    {
        ("Active", "Đang hiệu lực (Active)"),
        ("Inactive", "Ngưng sử dụng (Inactive)")
    };

    private void CreatePromotion()
    {
        var books = _services.GetBooks();
        var bookOptions = new List<(string Id, string Name)> { ("", "-- Không áp dụng sách cụ thể --") };
        bookOptions.AddRange(books.Select(b => (b.Id, $"{b.Title} ({b.Id})")));

        var fields = new List<EditDialogField>
        {
            new("name", "Tên chương trình", ""),
            new("start", "Bắt đầu (yyyy-MM-dd)", DateTime.Today.ToString("yyyy-MM-dd")),
            new("end", "Kết thúc (yyyy-MM-dd)", DateTime.Today.AddDays(7).ToString("yyyy-MM-dd")),
            new("book", "Sách giảm giá", "", bookOptions),
            new("bookRate", "% giảm theo sách (0-90)", ""),
            new("minimum", "Đơn tối thiểu để giảm đơn (VNĐ)", ""),
            new("orderRate", "% giảm theo đơn (0-90)", ""),
            new("voucher", "Mã voucher (tùy chọn)", ""),
            new("voucherRate", "% giảm voucher (0-90)", ""),
            new("maxUses", "Số lượt dùng voucher tối đa", "")
        };

        var values = EditDialog.Show(this, "Tạo chương trình khuyến mãi", fields);
        if (values is null) return;

        if (!DateTime.TryParse(values["start"], out var start) || !DateTime.TryParse(values["end"], out var end) || end < start)
            throw new InvalidOperationException("Ngày bắt đầu hoặc kết thúc khuyến mãi không hợp lệ.");

        decimal? dec(string key) => decimal.TryParse(values[key], NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;
        int? integer(string key) => int.TryParse(values[key], out var n) ? n : null;

        _services.Manager.CreatePromotion(new CreatePromotionDto(
            values["name"], start, end,
            EmptyToNull(values["book"]), dec("bookRate"),
            dec("minimum"), dec("orderRate"),
            EmptyToNull(values["voucher"]), dec("voucherRate"), integer("maxUses")));

        LoadPromotions(true);
    }

    private void EditPromotion()
    {
        var old = Selected<ManagerPromotionDto>(_promotionsGrid) ?? throw new InvalidOperationException("Chọn khuyến mãi cần sửa.");
        var fields = new List<EditDialogField>
        {
            new("name", "Tên chương trình", old.Name),
            new("start", "Bắt đầu (yyyy-MM-dd)", old.StartsAt.ToString("yyyy-MM-dd")),
            new("end", "Kết thúc (yyyy-MM-dd)", old.EndsAt.ToString("yyyy-MM-dd"))
        };
        var values = EditDialog.Show(this, $"Sửa khuyến mãi {old.Id}", fields);
        if (values is null) return;
        if (!DateTime.TryParse(values["start"], out var start) || !DateTime.TryParse(values["end"], out var end) || end < start)
            throw new InvalidOperationException("Ngày không hợp lệ.");
        _services.Manager.UpdatePromotion(old.Id, new UpdatePromotionDetailsDto(values["name"], start, end));
        LoadPromotions(true);
    }

    private void EditPromotionRules()
    {
        var old = Selected<ManagerPromotionDto>(_promotionsGrid) ?? throw new InvalidOperationException("Chọn chương trình khuyến mãi cần sửa quy tắc.");
        var books = _services.GetBooks();
        var bookRule = old.BookRules.FirstOrDefault();
        var orderRule = old.OrderRules.FirstOrDefault();
        var voucherRule = old.VoucherRules.FirstOrDefault();

        var bookOptions = new List<(string Id, string Name)> { ("", "-- Không giảm theo sách --") };
        bookOptions.AddRange(books.Select(b => (b.Id, $"{b.Title} ({b.Id})")));

        var fields = new List<EditDialogField>
        {
            new("book", "1. Sách giảm giá (KMTheoSach)", bookRule?.BookId ?? "", bookOptions),
            new("bookRate", "% giảm theo sách (0-90)", bookRule?.DiscountPercent.ToString(CultureInfo.InvariantCulture) ?? ""),
            new("minimum", "2. Đơn tối thiểu (KMTheoHD)", orderRule?.MinimumOrderAmount.ToString(CultureInfo.InvariantCulture) ?? ""),
            new("orderRate", "% giảm theo đơn (0-90)", orderRule?.DiscountPercent.ToString(CultureInfo.InvariantCulture) ?? ""),
            new("voucher", "3. Mã voucher (KMTheoVoucher)", voucherRule?.VoucherId ?? ""),
            new("voucherRate", "% giảm voucher (0-90)", voucherRule?.DiscountPercent.ToString(CultureInfo.InvariantCulture) ?? "")
        };

        var values = EditDialog.Show(this, $"Cập nhật quy tắc ưu đãi: {old.Name}", fields);
        if (values is null) return;

        decimal? dec(string key) => decimal.TryParse(values[key], NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;

        var bookRules = new List<PromotionBookRuleDto>();
        if (!string.IsNullOrWhiteSpace(values["book"]) && dec("bookRate") is { } bRate && bRate > 0)
            bookRules.Add(new PromotionBookRuleDto(values["book"], bRate));

        var orderRules = new List<PromotionOrderRuleDto>();
        if (dec("minimum") is { } minAmount && dec("orderRate") is { } oRate && oRate > 0)
            orderRules.Add(new PromotionOrderRuleDto(minAmount, oRate));

        var voucherRules = new List<PromotionVoucherRuleDto>();
        if (!string.IsNullOrWhiteSpace(values["voucher"]) && dec("voucherRate") is { } vRate && vRate > 0)
            voucherRules.Add(new PromotionVoucherRuleDto(values["voucher"].Trim(), vRate));

        _services.Manager.UpdatePromotionRules(old.Id, new UpdatePromotionRulesDto(bookRules, orderRules, voucherRules));
        LoadPromotions(true);
    }

    private void DeletePromotion()
    {
        var item = Selected<ManagerPromotionDto>(_promotionsGrid) ?? throw new InvalidOperationException("Chọn khuyến mãi.");
        if (Confirm($"Xóa chương trình {item.Name}?")) { _services.Manager.DeletePromotion(item.Id); LoadPromotions(true); }
    }

    private void EditVoucher()
    {
        var old = Selected<ManagerVoucherDto>(_vouchersGrid) ?? throw new InvalidOperationException("Chọn voucher.");
        var fields = new List<EditDialogField>
        {
            new("status", "Trạng thái voucher", old.Status, VoucherStatusOptions),
            new("expires", "Hạn dùng (yyyy-MM-dd, để trống = không hạn)", old.ExpiresAt?.ToString("yyyy-MM-dd") ?? ""),
            new("max", "Số lượt tối đa (để trống = không giới hạn)", old.MaxUsageCount?.ToString() ?? "")
        };
        var values = EditDialog.Show(this, $"Cập nhật voucher {old.Id}", fields);
        if (values is null) return;
        DateTime? expires = DateTime.TryParse(values["expires"], out var date) ? date : null;
        int? max = int.TryParse(values["max"], out var count) ? count : null;
        _services.Manager.UpdateVoucher(old.Id, new UpdateVoucherDto(values["status"], expires, max));
        LoadVouchers(true);
    }

    private void DeleteVoucher()
    {
        var item = Selected<ManagerVoucherDto>(_vouchersGrid) ?? throw new InvalidOperationException("Chọn voucher.");
        if (Confirm($"Xóa voucher {item.Id}?")) { _services.Manager.DeleteVoucher(item.Id); LoadVouchers(true); }
    }
}
