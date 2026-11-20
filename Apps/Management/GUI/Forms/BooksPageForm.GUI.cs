using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

internal sealed class BooksPageForm : ManagementGridPageForm
{
    private readonly ComboBox _category = ManagementGridPageForm.Choice(155, new ManagementSearchFilterOption(null, "Mọi thể loại"));
    private readonly ComboBox _stock = ManagementGridPageForm.Choice(145,
        new ManagementSearchFilterOption(null, "Mọi tình trạng kho"), new ManagementSearchFilterOption("in", "Còn hàng"), new ManagementSearchFilterOption("out", "Hết hàng"));
    private readonly NumericUpDown _minimumPrice = CreatePriceFilter();
    private readonly NumericUpDown _maximumPrice = CreatePriceFilter();

    public BooksPageForm(Func<string, List<ManagerAuthorDto>>? authorLookup = null) : base("Sách")
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

        var detailGrid = CreateDetailGrid();
        detailGrid.DataBindingComplete += (_, _) =>
        {
            try
            {
                if (detailGrid.Columns["Id"] is { } idCol) idCol.HeaderText = "Mã tác giả";
                if (detailGrid.Columns["Name"] is { } nameCol) nameCol.HeaderText = "Tên tác giả";
                if (detailGrid.Columns["Biography"] is { } bioCol) bioCol.HeaderText = "Tiểu sử";
            }
            catch { }
        };
        SetDetailsView(detailGrid, "CHI TIẾT TÁC GIẢ · Chọn một cuốn sách phía trên để xem danh sách tác giả");

        Grid.SelectionChanged += (_, _) =>
        {
            if (Grid.CurrentRow?.DataBoundItem is not TrangSach.Shared.DTO.BookItemDto book)
            {
                detailGrid.DataSource = null;
                SetDetailsTitle("CHI TIẾT TÁC GIẢ · Chọn một cuốn sách phía trên để xem danh sách tác giả");
                return;
            }

            var authors = authorLookup?.Invoke(book.Id) ?? new List<ManagerAuthorDto>();
            SetDetailsTitle($"CHI TIẾT SÁCH: {book.Title} ({book.Id}) · Tồn kho: {book.AvailableStock:N0} · {authors.Count} tác giả");
            detailGrid.DataSource = authors;
        };

        AddAction("Tải", "load");
        AddAction("Thêm", "add");
        AddAction("Sửa", "edit");
        AddAction("Ẩn sách", "hide");
        AddAction("Nhập sách (CSV/Excel)", "import");
        AddAction("Xuất sách (Excel/CSV)", "export");
    }

    protected override void OnGridDataBound()
    {
        base.OnGridDataBound();
        try
        {
            ConfigureBookGridColumns();
        }
        catch
        {
            // Bỏ qua lỗi định dạng cột nếu DataGridView chưa render layout xong
        }
    }

    private void ConfigureBookGridColumns()
    {
        if (Grid.Columns.Count == 0) return;

        void SetHeader(string propertyName, string headerText)
        {
            if (Grid.Columns[propertyName] is { } col)
            {
                col.HeaderText = headerText;
            }
        }

        SetHeader(nameof(TrangSach.Shared.DTO.BookItemDto.Id), "Mã sách");
        SetHeader(nameof(TrangSach.Shared.DTO.BookItemDto.Title), "Tên sách");
        SetHeader(nameof(TrangSach.Shared.DTO.BookItemDto.Author), "Tác giả");
        SetHeader(nameof(TrangSach.Shared.DTO.BookItemDto.Category), "Thể loại");
        SetHeader(nameof(TrangSach.Shared.DTO.BookItemDto.Price), "Giá bán (VNĐ)");
        SetHeader(nameof(TrangSach.Shared.DTO.BookItemDto.CoverStyle), "Đơn vị tính");
        SetHeader(nameof(TrangSach.Shared.DTO.BookItemDto.Label), "Trạng thái");
        SetHeader(nameof(TrangSach.Shared.DTO.BookItemDto.AvailableStock), "Tồn kho");
        SetHeader(nameof(TrangSach.Shared.DTO.BookItemDto.Publisher), "Nhà xuất bản");
        SetHeader(nameof(TrangSach.Shared.DTO.BookItemDto.Isbn), "ISBN");
        SetHeader(nameof(TrangSach.Shared.DTO.BookItemDto.CoverImagePath), "Ảnh bìa");
        SetHeader(nameof(TrangSach.Shared.DTO.BookItemDto.Description), "Mô tả");

        if (Grid.Columns[nameof(TrangSach.Shared.DTO.BookItemDto.Price)] is { } priceCol)
        {
            priceCol.DefaultCellStyle.Format = "N0";
            priceCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        if (Grid.Columns[nameof(TrangSach.Shared.DTO.BookItemDto.AvailableStock)] is { } stockCol)
        {
            stockCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
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

    protected override void OnDataSourceRefreshed(List<object> rows) =>
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



