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
        var bookOptions = new List<(string Id, string Name)> { ("", "-- Chọn sách cần giảm --") };
        bookOptions.AddRange(books.Select(b => (b.Id, $"{b.Title} ({b.Id})")));

        var fields = new List<EditDialogField>
        {
            new("name", "Tên chương trình", ""),
            new("start", "Bắt đầu (yyyy-MM-dd)", DateTime.Today.ToString("yyyy-MM-dd")),
            new("end", "Kết thúc (yyyy-MM-dd)", DateTime.Today.AddDays(7).ToString("yyyy-MM-dd")),

            // Hình thức 1: Giảm theo sách
            new("enableBook", "1. Áp dụng giảm theo Sách?", "no", YesNoOptions),
            new("book", "→ Chọn sách giảm giá", "", bookOptions),
            new("bookRate", "→ % giảm cho sách này (0-90)", "10"),

            // Hình thức 2: Giảm theo hóa đơn
            new("enableOrder", "2. Áp dụng giảm theo Hóa đơn?", "no", YesNoOptions),
            new("minimum", "→ Mức đơn tối thiểu (VNĐ)", "200000"),
            new("orderRate", "→ % giảm cho đơn hàng (0-90)", "5"),

            // Hình thức 3: Giảm qua Voucher
            new("enableVoucher", "3. Áp dụng tạo Voucher kèm theo?", "no", YesNoOptions),
            new("voucher", "→ Mã voucher", $"KM{DateTime.UtcNow:yyMMdd}"),
            new("voucherRate", "→ % giảm qua voucher (0-90)", "10"),
            new("maxUses", "→ Số lượt dùng tối đa", "100")
        };

        var values = EditDialog.Show(this, "Tạo chương trình khuyến mãi mới", fields);
        if (values is null) return;

        if (!DateTime.TryParse(values["start"], out var start) || !DateTime.TryParse(values["end"], out var end) || end < start)
            throw new InvalidOperationException("Ngày bắt đầu hoặc kết thúc khuyến mãi không hợp lệ.");

        decimal? dec(string key) => decimal.TryParse(values[key], NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;
        int? integer(string key) => int.TryParse(values[key], out var n) ? n : null;

        var hasBook = values["enableBook"] == "yes";
        var hasOrder = values["enableOrder"] == "yes";
        var hasVoucher = values["enableVoucher"] == "yes";

        if (!hasBook && !hasOrder && !hasVoucher)
            throw new InvalidOperationException("Vui lòng chọn áp dụng ít nhất một trong ba hình thức khuyến mãi (theo sách, theo hóa đơn hoặc voucher).");

        string? chosenBook = null;
        decimal? chosenBookRate = null;
        if (hasBook)
        {
            chosenBook = EmptyToNull(values["book"]);
            if (string.IsNullOrWhiteSpace(chosenBook))
                throw new InvalidOperationException("Bạn đã chọn áp dụng giảm theo Sách, vui lòng chọn một cuốn sách từ danh sách.");
            chosenBookRate = dec("bookRate");
            if (chosenBookRate is null or <= 0 or > 90)
                throw new InvalidOperationException("% giảm cho sách phải từ 0.1% đến 90%.");
        }

        decimal? chosenMinOrder = null;
        decimal? chosenOrderRate = null;
        if (hasOrder)
        {
            chosenMinOrder = dec("minimum");
            if (chosenMinOrder is null or < 0)
                throw new InvalidOperationException("Mức đơn tối thiểu phải là số không âm.");
            chosenOrderRate = dec("orderRate");
            if (chosenOrderRate is null or <= 0 or > 90)
                throw new InvalidOperationException("% giảm cho đơn hàng phải từ 0.1% đến 90%.");
        }

        string? chosenVoucher = null;
        decimal? chosenVoucherRate = null;
        int? chosenMaxUses = null;
        if (hasVoucher)
        {
            chosenVoucher = EmptyToNull(values["voucher"]);
            if (string.IsNullOrWhiteSpace(chosenVoucher))
                throw new InvalidOperationException("Bạn đã chọn áp dụng tạo Voucher, vui lòng nhập mã voucher.");
            chosenVoucherRate = dec("voucherRate");
            if (chosenVoucherRate is null or <= 0 or > 90)
                throw new InvalidOperationException("% giảm qua voucher phải từ 0.1% đến 90%.");
            chosenMaxUses = integer("maxUses");
        }

        _services.Manager.CreatePromotion(new CreatePromotionDto(
            values["name"], start, end,
            chosenBook, chosenBookRate,
            chosenMinOrder, chosenOrderRate,
            chosenVoucher, chosenVoucherRate, chosenMaxUses));

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
        var promoOptions = new List<(string Id, string Name)> { ("", "-- Không gắn vào CTKM nào --") };
        promoOptions.AddRange(promos.Select(p => (p.Id, $"{p.Name} ({p.Id})")));

        var fields = new List<EditDialogField>
        {
            new("code", "Mã voucher (để trống sẽ tự sinh)", $"VCH{DateTime.UtcNow:yyMMddHHmm}"),
            new("status", "Trạng thái voucher", "Active", VoucherStatusOptions),
            new("expires", "Hạn dùng (yyyy-MM-dd, để trống = không hạn)", DateTime.Today.AddDays(30).ToString("yyyy-MM-dd")),
            new("max", "Số lượt dùng tối đa", "100"),
            new("promo", "Gắn vào CTKM", "", promoOptions),
            new("rate", "→ % giảm giá (bắt buộc nếu chọn CTKM, 0.1-100)", "10")
        };

        var values = EditDialog.Show(this, "Thêm voucher mới", fields);
        if (values is null) return;

        var code = EmptyToNull(values["code"]);
        var status = string.IsNullOrWhiteSpace(values["status"]) ? "Active" : values["status"].Trim();
        DateTime? expires = DateTime.TryParse(values["expires"], out var d) ? d : null;
        int? max = int.TryParse(values["max"], out var m) ? m : null;

        var promoId = EmptyToNull(values["promo"]);
        decimal? discountPercent = null;
        if (!string.IsNullOrWhiteSpace(promoId))
        {
            if (!decimal.TryParse(values["rate"], NumberStyles.Any, CultureInfo.InvariantCulture, out var r) &&
                !decimal.TryParse(values["rate"], out r))
            {
                throw new InvalidOperationException("% giảm giá qua voucher không hợp lệ.");
            }
            if (r is <= 0 or > 100) throw new InvalidOperationException("% giảm giá qua voucher phải từ 0.1% đến 100%.");
            discountPercent = r;
        }

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
