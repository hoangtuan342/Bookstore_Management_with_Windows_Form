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
        SetSearchPlaceholder("Mã phiếu, nhà cung cấp, nhân viên, tên hoặc mã sách...");
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
        detailGrid.DataBindingComplete += (_, _) =>
        {
            try
            {
                if (detailGrid.Columns["BookId"] is { } bId) bId.HeaderText = "Mã sách";
                if (detailGrid.Columns["BookTitle"] is { } title) title.HeaderText = "Tên sách";
                if (detailGrid.Columns["Quantity"] is { } qty)
                {
                    qty.HeaderText = "Số lượng nhập";
                    qty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (detailGrid.Columns["UnitCost"] is { } cost)
                {
                    cost.HeaderText = "Giá vốn (VNĐ)";
                    cost.DefaultCellStyle.Format = "N0";
                    cost.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (detailGrid.Columns["LineTotal"] is { } total)
                {
                    total.HeaderText = "Thành tiền (VNĐ)";
                    total.DefaultCellStyle.Format = "N0";
                    total.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
            catch { }
        };
        SetDetailsView(detailGrid, "CHI TIẾT PHIẾU NHẬP · Chọn phiếu phía trên để xem các mặt hàng");
        Grid.SelectionChanged += (_, _) =>
        {
            if (Grid.CurrentRow?.DataBoundItem is not ManagerReceiptDto receipt)
            {
                detailGrid.DataSource = null;
                SetDetailsTitle("CHI TIẾT PHIẾU NHẬP · Chọn phiếu phía trên để xem các mặt hàng");
                return;
            }

            var empInfo = string.IsNullOrWhiteSpace(receipt.EmployeeName) ? "" : $" · NV: {receipt.EmployeeName}";
            SetDetailsTitle($"CHI TIẾT PHIẾU {receipt.Id} · {receipt.SupplierName}{empInfo} · {receipt.Lines.Count} mặt hàng · {receipt.TotalAmount:N0} ₫");
            detailGrid.DataSource = receipt.Lines.ToList();
        };
        AddAction("Tải", "load");
        AddAction("Tạo phiếu nhập", "create");
        AddAction("Đảo phiếu", "cancel");

        Grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.Value is null) return;
            var colName = Grid.Columns[e.ColumnIndex].DataPropertyName;
            if (colName == nameof(ManagerReceiptDto.Status))
            {
                e.Value = OrderStatusFormatter.FormatReceiptStatus(e.Value.ToString());
                e.FormattingApplied = true;
            }
        };
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

            SetHeader(nameof(ManagerReceiptDto.Id), "Mã phiếu nhập");
            SetHeader(nameof(ManagerReceiptDto.CreatedAt), "Ngày nhập");
            SetHeader(nameof(ManagerReceiptDto.SupplierName), "Nhà cung cấp");
            SetHeader(nameof(ManagerReceiptDto.EmployeeName), "Nhân viên nhập");
            SetHeader(nameof(ManagerReceiptDto.Status), "Trạng thái");
            SetHeader(nameof(ManagerReceiptDto.TotalAmount), "Tổng tiền nhập (VNĐ)");

            if (Grid.Columns[nameof(ManagerReceiptDto.CreatedAt)] is { } createdCol)
            {
                createdCol.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }
            if (Grid.Columns[nameof(ManagerReceiptDto.TotalAmount)] is { } totalCol)
            {
                totalCol.DefaultCellStyle.Format = "N0";
                totalCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (Grid.Columns[nameof(ManagerReceiptDto.SupplierId)] is { } supIdCol)
            {
                supIdCol.Visible = false;
            }
            if (Grid.Columns[nameof(ManagerReceiptDto.EmployeeId)] is { } empIdCol)
            {
                empIdCol.Visible = false;
            }
            if (Grid.Columns[nameof(ManagerReceiptDto.Lines)] is { } linesCol)
            {
                linesCol.Visible = false;
            }
        }
        catch { }
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
        ? receipt.Lines.SelectMany(line => new[] { line.BookId, line.BookTitle }).Concat(new[] { receipt.EmployeeId, receipt.EmployeeName })
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




