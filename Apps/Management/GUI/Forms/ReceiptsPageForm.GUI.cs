using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

internal sealed class ReceiptsPageForm : ManagementGridPageForm
{
    private readonly ComboBox _status = ManagementGridPageForm.Choice(130, new ManagementSearchFilterOption(null, "Mọi trạng thái"));
    private readonly ComboBox _supplier = ManagementGridPageForm.Choice(150, new ManagementSearchFilterOption(null, "Mọi nhà cung cấp"));
    private readonly DateTimePicker _fromDate = ManagementGridPageForm.Date();
    private readonly DateTimePicker _toDate = ManagementGridPageForm.Date();

    public ReceiptsPageForm() : base("Phiếu nhập")
    {
        SetSearchPlaceholder("Mã phiếu, nhà cung cấp, tên hoặc mã sách...");
        _status.SelectedValueChanged += (_, _) => RefreshFilter();
        _supplier.SelectedValueChanged += (_, _) => RefreshFilter();
        _fromDate.ValueChanged += (_, _) => RefreshFilter();
        _toDate.ValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Trạng thái"));
        AddSearchFilterControl(_status);
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Nhà cung cấp"));
        AddSearchFilterControl(_supplier);
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Từ"));
        AddSearchFilterControl(_fromDate);
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Đến"));
        AddSearchFilterControl(_toDate);
        var detailGrid = CreateDetailGrid();
        SetDetailsView(detailGrid, "CHI TIẾT PHIẾU NHẬP · Chọn phiếu phía trên để xem các mặt hàng");
        Grid.SelectionChanged += (_, _) =>
        {
            if (Grid.CurrentRow?.DataBoundItem is not ManagerReceiptDto receipt)
            {
                detailGrid.DataSource = null;
                SetDetailsTitle("CHI TIẾT PHIẾU NHẬP · Chọn phiếu phía trên để xem các mặt hàng");
                return;
            }

            SetDetailsTitle($"CHI TIẾT PHIẾU {receipt.Id} · {receipt.SupplierName} · {receipt.Lines.Count} mặt hàng · {receipt.TotalAmount:N0} ₫");
            detailGrid.DataSource = receipt.Lines.ToList();
        };
        AddAction("Tải", "load");
        AddAction("Tạo phiếu nhập", "create");
        AddAction("Đảo phiếu", "cancel");
    }

    protected override bool HasAdditionalFilters => ManagementGridPageForm.Value(_status) is not null
        || ManagementGridPageForm.Value(_supplier) is not null || _fromDate.Checked || _toDate.Checked;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not ManagerReceiptDto receipt) return true;
        var status = ManagementGridPageForm.Value(_status);
        var supplier = ManagementGridPageForm.Value(_supplier);
        var date = receipt.CreatedAt.ToLocalTime().Date;
        return (status is null || receipt.Status == status)
            && (supplier is null || receipt.SupplierId == supplier)
            && (!_fromDate.Checked || date >= _fromDate.Value.Date)
            && (!_toDate.Checked || date <= _toDate.Value.Date);
    }

    protected override IEnumerable<string?> GetAdditionalSearchValues(object item) => item is ManagerReceiptDto receipt
        ? receipt.Lines.SelectMany(line => new[] { line.BookId, line.BookTitle })
        : [];

    protected override void OnDataSourceRefreshed(List<object> rows)
    {
        var receipts = rows.OfType<ManagerReceiptDto>().ToArray();
        ManagementGridPageForm.SetValues(_status, receipts.Select(receipt => receipt.Status), "Mọi trạng thái", ManagementGridPageForm.StatusCaption);
        var suppliers = receipts.Select(receipt => new ManagementSearchFilterOption(receipt.SupplierId, receipt.SupplierName))
            .GroupBy(item => item.Value).Select(group => group.First()).ToList();
        var selected = ManagementGridPageForm.Value(_supplier);
        _supplier.BeginUpdate();
        try
        {
            _supplier.Items.Clear();
            _supplier.Items.Add(new ManagementSearchFilterOption(null, "Mọi nhà cung cấp"));
            foreach (var option in suppliers.OrderBy(item => item.Caption)) _supplier.Items.Add(option);
            _supplier.SelectedItem = _supplier.Items.Cast<ManagementSearchFilterOption>().FirstOrDefault(item => item.Value == selected) ?? _supplier.Items[0];
        }
        finally { _supplier.EndUpdate(); }
    }
}




