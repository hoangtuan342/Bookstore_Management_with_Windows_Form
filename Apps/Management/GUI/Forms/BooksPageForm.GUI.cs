namespace TrangSach.Management.GUI;

internal sealed class BooksPageForm : ManagementGridPageForm
{
    private readonly ComboBox _category = ManagementGridPageForm.Choice(155, new ManagementSearchFilterOption(null, "Mọi thể loại"));
    private readonly ComboBox _stock = ManagementGridPageForm.Choice(145,
        new ManagementSearchFilterOption(null, "Mọi tình trạng kho"), new ManagementSearchFilterOption("in", "Còn hàng"), new ManagementSearchFilterOption("out", "Hết hàng"));
    private readonly NumericUpDown _minimumPrice = CreatePriceFilter();
    private readonly NumericUpDown _maximumPrice = CreatePriceFilter();

    public BooksPageForm() : base("Sách")
    {
        SetSearchPlaceholder("Tên sách, tác giả, mã sách, thể loại...");
        _category.SelectedValueChanged += (_, _) => RefreshFilter();
        _stock.SelectedValueChanged += (_, _) => RefreshFilter();
        _minimumPrice.ValueChanged += (_, _) => RefreshFilter();
        _maximumPrice.ValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Thể loại"));
        AddSearchFilterControl(_category);
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Tồn kho"));
        AddSearchFilterControl(_stock);
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Giá từ"));
        AddSearchFilterControl(_minimumPrice);
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("đến"));
        AddSearchFilterControl(_maximumPrice);
        AddAction("Tải", "load");
        AddAction("Thêm", "add");
        AddAction("Sửa", "edit");
        AddAction("Ẩn sách", "hide");
        AddAction("Nhập CSV", "import");
        AddAction("Xuất CSV", "export");
    }

    protected override bool HasAdditionalFilters => ManagementGridPageForm.Value(_category) is not null
        || ManagementGridPageForm.Value(_stock) is not null || _minimumPrice.Value > 0 || _maximumPrice.Value > 0;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not TrangSach.Shared.DTO.BookItemDto book) return true;
        var category = ManagementGridPageForm.Value(_category);
        var stock = ManagementGridPageForm.Value(_stock);
        return (category is null || book.Category == category)
            && (stock is null || (stock == "in" ? book.AvailableStock > 0 : book.AvailableStock <= 0))
            && (_minimumPrice.Value == 0 || book.Price >= _minimumPrice.Value)
            && (_maximumPrice.Value == 0 || book.Price <= _maximumPrice.Value);
    }

    protected override void OnDataSourceRefreshed(IReadOnlyList<object> rows) =>
        ManagementGridPageForm.SetValues(_category, rows.OfType<TrangSach.Shared.DTO.BookItemDto>().Select(book => book.Category), "Mọi thể loại");

    private static NumericUpDown CreatePriceFilter() => new()
    {
        Minimum = 0,
        Maximum = 1_000_000_000,
        DecimalPlaces = 0,
        ThousandsSeparator = true,
        Width = 105,
        Font = new Font("Segoe UI", 8.5f),
        Margin = new Padding(2, 3, 5, 0)
    };
}



