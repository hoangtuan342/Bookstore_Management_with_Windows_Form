namespace TrangSach.Management.Views;

internal sealed class PromotionsPageView : ManagementGridPageView
{
    private readonly ComboBox _period = ManagementSearchFilterControls.Choice(190,
        new(null, "Mọi chương trình"), new("active", "Đang diễn ra"), new("upcoming", "Sắp diễn ra"), new("ended", "Đã kết thúc"));

    public PromotionsPageView() : base("Khuyến mãi")
    {
        SetSearchPlaceholder("Tên, mã chương trình, sách hoặc voucher...");
        _period.SelectedValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Thời gian"));
        AddSearchFilterControl(_period);
        AddAction("Tải", "load");
        AddAction("Tạo", "create");
        AddAction("Sửa tên/thời gian", "edit");
        AddAction("Xóa", "delete");
    }

    protected override bool HasAdditionalFilters => ManagementSearchFilterControls.Value(_period) is not null;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not BookStore.Shared.Models.ManagerPromotion promotion) return true;
        var period = ManagementSearchFilterControls.Value(_period);
        var now = DateTime.Now;
        return period switch
        {
            "active" => promotion.StartsAt <= now && promotion.EndsAt >= now,
            "upcoming" => promotion.StartsAt > now,
            "ended" => promotion.EndsAt < now,
            _ => true
        };
    }

    protected override IEnumerable<string?> GetAdditionalSearchValues(object item) => item is BookStore.Shared.Models.ManagerPromotion promotion
        ? promotion.BookOffers.Concat(promotion.OrderOffers).Concat(promotion.Vouchers)
        : [];
}

