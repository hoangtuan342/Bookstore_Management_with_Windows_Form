using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

public sealed class PromotionRuleDisplayItem
{
    public string Type { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Discount { get; set; } = string.Empty;

    public PromotionRuleDisplayItem() { }

    public PromotionRuleDisplayItem(string type, string target, string condition, string discount)
    {
        Type = type;
        Target = target;
        Condition = condition;
        Discount = discount;
    }
}

internal sealed class PromotionsPageForm : ManagementGridPageForm
{
    private readonly ComboBox _period = ManagementGridPageForm.Choice(190,
        new(null, "Mọi chương trình"), new("active", "Đang diễn ra"), new("upcoming", "Sắp diễn ra"), new("ended", "Đã kết thúc"));

    public PromotionsPageForm() : base("Khuyến mãi")
    {
        SetSearchPlaceholder("Tên, mã chương trình, sách hoặc voucher...");
        _period.SelectedValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Thời gian"));
        AddSearchFilterControl(_period);

        var detailGrid = CreateDetailGrid();
        detailGrid.DataBindingComplete += (_, _) =>
        {
            try
            {
                if (detailGrid.Columns["Type"] is { } t) t.HeaderText = "Hình thức ưu đãi";
                if (detailGrid.Columns["Target"] is { } tg) tg.HeaderText = "Đối tượng áp dụng";
                if (detailGrid.Columns["Condition"] is { } cd) cd.HeaderText = "Điều kiện áp dụng";
                if (detailGrid.Columns["Discount"] is { } dc)
                {
                    dc.HeaderText = "Mức giảm";
                    dc.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
            catch { }
        };
        SetDetailsView(detailGrid, "CHI TIẾT ƯU ĐÃI · Chọn một chương trình khuyến mãi phía trên để xem các mức giảm");

        Grid.SelectionChanged += (_, _) =>
        {
            if (Grid.CurrentRow?.DataBoundItem is not ManagerPromotionDto promo)
            {
                detailGrid.DataSource = null;
                SetDetailsTitle("CHI TIẾT ƯU ĐÃI · Chọn một chương trình khuyến mãi phía trên để xem các mức giảm");
                return;
            }

            var items = new List<PromotionRuleDisplayItem>();

            // 1. Khuyến mãi theo sách
            foreach (var b in promo.BookRules)
            {
                var offer = promo.BookOffers.FirstOrDefault(o => o.Contains(b.BookId, StringComparison.OrdinalIgnoreCase)) ?? b.BookId;
                items.Add(new PromotionRuleDisplayItem("1. Giảm theo Sách (KMTheoSach)", offer, "Khi mua sách này", $"{b.DiscountPercent:0.#}%"));
            }

            // 2. Khuyến mãi theo hóa đơn
            foreach (var o in promo.OrderRules)
            {
                items.Add(new PromotionRuleDisplayItem("2. Giảm theo Đơn hàng (KMTheoHD)", $"Hóa đơn từ {o.MinimumOrderAmount:N0} ₫", $"Đơn hàng đạt tối thiểu {o.MinimumOrderAmount:N0} ₫", $"{o.DiscountPercent:0.#}%"));
            }

            // 3. Khuyến mãi theo Voucher
            foreach (var v in promo.VoucherRules)
            {
                items.Add(new PromotionRuleDisplayItem("3. Giảm theo Voucher (KMTheoVoucher)", $"Voucher '{v.VoucherId}'", "Khi nhập mã voucher này", $"{v.DiscountPercent:0.#}%"));
            }

            SetDetailsTitle($"CHI TIẾT ƯU ĐÃI: {promo.Name} ({promo.Id}) · Thời gian: {promo.StartsAt:dd/MM/yyyy} – {promo.EndsAt:dd/MM/yyyy} · {items.Count} quy tắc ưu đãi");
            detailGrid.DataSource = items;
        };

        AddAction("Tải", "load");
        AddAction("Tạo", "create");
        AddAction("Sửa tên/thời gian", "edit");
        AddAction("Sửa quy tắc ưu đãi", "rules");
        AddAction("Xóa", "delete");
    }

    protected override void OnGridDataBound()
    {
        base.OnGridDataBound();
        try
        {
            void SetHeader(string propertyName, string headerText)
            {
                if (Grid.Columns[propertyName] is { } col)
                {
                    col.HeaderText = headerText;
                }
            }

            SetHeader(nameof(ManagerPromotionDto.Id), "Mã khuyến mãi");
            SetHeader(nameof(ManagerPromotionDto.Name), "Tên chương trình");
            SetHeader(nameof(ManagerPromotionDto.StartsAt), "Bắt đầu");
            SetHeader(nameof(ManagerPromotionDto.EndsAt), "Kết thúc");

            if (Grid.Columns[nameof(ManagerPromotionDto.StartsAt)] is { } startCol)
            {
                startCol.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }
            if (Grid.Columns[nameof(ManagerPromotionDto.EndsAt)] is { } endCol)
            {
                endCol.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }
            if (Grid.Columns[nameof(ManagerPromotionDto.BookOffers)] is { } boCol) boCol.Visible = false;
            if (Grid.Columns[nameof(ManagerPromotionDto.OrderOffers)] is { } ooCol) ooCol.Visible = false;
            if (Grid.Columns[nameof(ManagerPromotionDto.Vouchers)] is { } vcCol) vcCol.Visible = false;
            if (Grid.Columns[nameof(ManagerPromotionDto.BookRules)] is { } brCol) brCol.Visible = false;
            if (Grid.Columns[nameof(ManagerPromotionDto.OrderRules)] is { } orCol) orCol.Visible = false;
            if (Grid.Columns[nameof(ManagerPromotionDto.VoucherRules)] is { } vrCol) vrCol.Visible = false;
        }
        catch { }
    }

    protected override bool HasAdditionalFilters => ManagementGridPageForm.Value(_period) is not null;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not ManagerPromotionDto promotion) return true;
        var period = ManagementGridPageForm.Value(_period);
        var now = DateTime.Now;
        return period switch
        {
            "active" => promotion.StartsAt <= now && promotion.EndsAt >= now,
            "upcoming" => promotion.StartsAt > now,
            "ended" => promotion.EndsAt < now,
            _ => true
        };
    }

    protected override IEnumerable<string?> GetAdditionalSearchValues(object item) => item is ManagerPromotionDto promotion
        ? promotion.BookOffers.Concat(promotion.OrderOffers).Concat(promotion.Vouchers)
        : [];
}
