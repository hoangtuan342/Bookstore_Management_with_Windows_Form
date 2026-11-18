namespace TrangSach.Management.GUI;

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
        AddAction("Tải", "load");
        AddAction("Tạo", "create");
        AddAction("Sửa tên/thời gian", "edit");
        AddAction("Xóa", "delete");
    }

    protected override bool HasAdditionalFilters => ManagementGridPageForm.Value(_period) is not null;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not TrangSach.Shared.DTO.ManagerPromotionDto promotion) return true;
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

    protected override IEnumerable<string?> GetAdditionalSearchValues(object item) => item is TrangSach.Shared.DTO.ManagerPromotionDto promotion
        ? promotion.BookOffers.Concat(promotion.OrderOffers).Concat(promotion.Vouchers)
        : [];
}



