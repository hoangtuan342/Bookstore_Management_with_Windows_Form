using BookStore.Shared.Models;

namespace TrangSach.Management.Views;

internal sealed class ReceiptsPageView : ManagementGridPageView
{
    private readonly ComboBox _status = ManagementSearchFilterControls.Choice(130, new ManagementSearchFilterOption(null, "Mọi trạng thái"));
    private readonly ComboBox _supplier = ManagementSearchFilterControls.Choice(150, new ManagementSearchFilterOption(null, "Mọi nhà cung cấp"));
    private readonly DateTimePicker _fromDate = ManagementSearchFilterControls.Date();
    private readonly DateTimePicker _toDate = ManagementSearchFilterControls.Date();

    public ReceiptsPageView() : base("Phiếu nhập")
    {
        SetSearchPlaceholder("Mã phiếu, nhà cung cấp, tên hoặc mã sách...");
        _status.SelectedValueChanged += (_, _) => RefreshFilter();
        _supplier.SelectedValueChanged += (_, _) => RefreshFilter();
        _fromDate.ValueChanged += (_, _) => RefreshFilter();
        _toDate.ValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Trạng thái"));
        AddSearchFilterControl(_status);
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Nhà cung cấp"));
        AddSearchFilterControl(_supplier);
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Từ"));
        AddSearchFilterControl(_fromDate);
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Đến"));
        AddSearchFilterControl(_toDate);
        var detailGrid = CreateDetailGrid();
        SetDetailsView(detailGrid, "CHI TIẾT PHIẾU NHẬP · Chọn phiếu phía trên để xem các mặt hàng");
        Grid.SelectionChanged += (_, _) =>
        {
            if (Grid.CurrentRow?.DataBoundItem is not ManagerReceipt receipt)
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

    protected override bool HasAdditionalFilters => ManagementSearchFilterControls.Value(_status) is not null
        || ManagementSearchFilterControls.Value(_supplier) is not null || _fromDate.Checked || _toDate.Checked;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not ManagerReceipt receipt) return true;
        var status = ManagementSearchFilterControls.Value(_status);
        var supplier = ManagementSearchFilterControls.Value(_supplier);
        var date = receipt.CreatedAt.ToLocalTime().Date;
        return (status is null || receipt.Status == status)
            && (supplier is null || receipt.SupplierId == supplier)
            && (!_fromDate.Checked || date >= _fromDate.Value.Date)
            && (!_toDate.Checked || date <= _toDate.Value.Date);
    }

    protected override IEnumerable<string?> GetAdditionalSearchValues(object item) => item is ManagerReceipt receipt
        ? receipt.Lines.SelectMany(line => new[] { line.BookId, line.BookTitle })
        : [];

    protected override void OnDataSourceRefreshed(IReadOnlyList<object> rows)
    {
        var receipts = rows.OfType<ManagerReceipt>().ToArray();
        ManagementSearchFilterControls.SetValues(_status, receipts.Select(receipt => receipt.Status), "Mọi trạng thái", ManagementSearchFilterControls.StatusCaption);
        var suppliers = receipts.Select(receipt => new ManagementSearchFilterOption(receipt.SupplierId, receipt.SupplierName))
            .GroupBy(item => item.Value).Select(group => group.First()).ToList();
        var selected = ManagementSearchFilterControls.Value(_supplier);
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


