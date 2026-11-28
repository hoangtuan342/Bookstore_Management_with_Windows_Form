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

    private static readonly List<(string Id, string Name)> YesNoOptions = new()
    {
        ("no", "Không chọn áp dụng"),
        ("yes", "Có chọn áp dụng")
    };

    private void CreatePromotion()
    {
        var books = _services.GetBooks();

        using var editor = new PromotionEditorForm(books);
        if (editor.ShowDialog(this) == DialogResult.OK && editor.ResultDto != null)
        {
            _services.Manager.CreatePromotion(editor.ResultDto);
            LoadPromotions(true);
        }
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

        using var editor = new PromotionRulesEditorForm(old, books);
        if (editor.ShowDialog(this) == DialogResult.OK && editor.ResultDto != null)
        {
            _services.Manager.UpdatePromotionRules(old.Id, editor.ResultDto);
            LoadPromotions(true);
        }
    }

    private void DeletePromotion()
    {
        var item = Selected<ManagerPromotionDto>(_promotionsGrid) ?? throw new InvalidOperationException("Chọn khuyến mãi.");
        if (Confirm($"Xóa chương trình {item.Name}?")) { _services.Manager.DeletePromotion(item.Id); LoadPromotions(true); }
    }

    private void CreateVoucher()
    {
        var promos = _services.GetPromotions();
        if (promos.Count == 0)
        {
            throw new InvalidOperationException("Chưa có chương trình khuyến mãi nào trong hệ thống. Vui lòng tạo CTKM trước khi tạo Voucher.");
        }

        var promoOptions = promos.Select(p => (p.Id, $"{p.Name} ({p.Id})")).ToList();

        var fields = new List<EditDialogField>
        {
            new("code", "Mã voucher (để trống sẽ tự sinh)", $"VCH{DateTime.UtcNow:yyMMddHHmm}"),
            new("status", "Trạng thái voucher", "Active", VoucherStatusOptions),
            new("expires", "Hạn dùng (yyyy-MM-dd, để trống = không hạn)", DateTime.Today.AddDays(30).ToString("yyyy-MM-dd")),
            new("max", "Số lượt dùng tối đa", "100"),
            new("promo", "Chương trình khuyến mãi (bắt buộc)", promoOptions[0].Id, promoOptions),
            new("rate", "→ % giảm giá áp dụng (0.1 - 100)", "10")
        };

        var values = EditDialog.Show(this, "Thêm voucher mới", fields);
        if (values is null) return;

        var code = EmptyToNull(values["code"]);
        var status = string.IsNullOrWhiteSpace(values["status"]) ? "Active" : values["status"].Trim();
        DateTime? expires = DateTime.TryParse(values["expires"], out var d) ? d : null;
        int? max = int.TryParse(values["max"], out var m) ? m : null;

        var promoId = EmptyToNull(values["promo"]);
        if (string.IsNullOrWhiteSpace(promoId))
        {
            throw new InvalidOperationException("Vui lòng chọn chương trình khuyến mãi cho voucher.");
        }

        if (!decimal.TryParse(values["rate"], NumberStyles.Any, CultureInfo.InvariantCulture, out var r) &&
            !decimal.TryParse(values["rate"], out r))
        {
            throw new InvalidOperationException("% giảm giá qua voucher không hợp lệ.");
        }
        if (r is <= 0 or > 100) throw new InvalidOperationException("% giảm giá qua voucher phải từ 0.1% đến 100%.");
        decimal? discountPercent = r;

        _services.Manager.CreateVoucher(new CreateVoucherDto(code, status, expires, max, promoId, discountPercent));
        LoadVouchers(true);
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
