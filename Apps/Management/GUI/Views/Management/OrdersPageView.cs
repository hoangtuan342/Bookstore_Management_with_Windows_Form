using BookStore.Shared.Models;

namespace TrangSach.Management.Views;

internal sealed class OrdersPageView : ManagementGridPageView
{
    private sealed record OrderStatusOption(string? Value, string Caption);
    private static readonly OrderStatusOption[] StatusOptions =
    [
        new(null, "Mọi trạng thái"),
        new("Pending", "Chờ xác nhận"),
        new("Confirmed", "Đã xác nhận"),
        new("Shipping", "Đang giao"),
        new("Completed", "Hoàn tất"),
        new("Cancelled", "Đã hủy")
    ];

    private readonly ComboBox _status = new();
    private readonly DateTimePicker _fromDate = CreateDateFilter();
    private readonly DateTimePicker _toDate = CreateDateFilter();

    public OrdersPageView() : base("Đơn hàng")
    {
        SetSearchPlaceholder("Mã đơn, khách hàng, nhân viên, tên sách...");
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Width = 130;
        _status.Font = new Font("Segoe UI", 8.5f);
        _status.DataSource = StatusOptions;
        _status.DisplayMember = nameof(OrderStatusOption.Caption);
        _status.SelectedIndexChanged += (_, _) => RefreshFilter();
        _fromDate.ValueChanged += (_, _) => RefreshFilter();
        _fromDate.CloseUp += (_, _) => RefreshFilter();
        _toDate.ValueChanged += (_, _) => RefreshFilter();
        _toDate.CloseUp += (_, _) => RefreshFilter();
        AddSearchFilterControl(new Label { Text = "Trạng thái", AutoSize = true, Margin = new Padding(3, 8, 2, 0), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(72, 88, 104) });
        AddSearchFilterControl(_status);
        AddSearchFilterControl(new Label { Text = "Từ ngày", AutoSize = true, Margin = new Padding(4, 8, 2, 0), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(72, 88, 104) });
        AddSearchFilterControl(_fromDate);
        AddSearchFilterControl(new Label { Text = "Đến ngày", AutoSize = true, Margin = new Padding(2, 8, 2, 0), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(72, 88, 104) });
        AddSearchFilterControl(_toDate);

        var detailGrid = CreateDetailGrid();
        SetDetailsView(detailGrid, "CHI TIẾT ĐƠN HÀNG · Chọn một đơn phía trên để xem các mặt hàng");
        Grid.SelectionChanged += (_, _) =>
        {
            if (Grid.CurrentRow?.DataBoundItem is not ManagerOrderRow order)
            {
                detailGrid.DataSource = null;
                SetDetailsTitle("CHI TIẾT ĐƠN HÀNG · Chọn một đơn phía trên để xem các mặt hàng");
                return;
            }

            SetDetailsTitle($"CHI TIẾT ĐƠN {order.Id} · {order.CustomerName} · {order.ItemCount} mặt hàng · {order.TotalAmount:N0} ₫");
            detailGrid.DataSource = order.Lines.ToList();
        };
        AddAction("Tải", "load");
        AddAction("Tạo đơn", "create");
        AddAction("Đổi trạng thái", "status");
        AddAction("Sửa thông tin", "edit");
        AddAction("Hủy đơn", "cancel");
    }

    protected override bool HasAdditionalFilters =>
        (_status.SelectedItem as OrderStatusOption)?.Value is not null || _fromDate.Checked || _toDate.Checked;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not ManagerOrderRow order) return true;
        var selectedStatus = (_status.SelectedItem as OrderStatusOption)?.Value;
        if (selectedStatus is not null && !string.Equals(order.OrderStatus, selectedStatus, StringComparison.OrdinalIgnoreCase)) return false;
        var createdDate = order.CreatedAt.ToLocalTime().Date;
        if (_fromDate.Checked && createdDate < _fromDate.Value.Date) return false;
        if (_toDate.Checked && createdDate > _toDate.Value.Date) return false;
        return true;
    }

    protected override IEnumerable<string?> GetAdditionalSearchValues(object item)
    {
        if (item is not ManagerOrderRow order) return [];
        return new[] { order.Id, order.CustomerName, order.EmployeeName, order.ReceiverPhone, order.OrderStatus,
            order.PaymentStatus, order.PaymentMethod, order.ShippingAddress }
            .Concat(order.Lines.SelectMany(line => new[] { line.BookId, line.Title }));
    }

    private static DateTimePicker CreateDateFilter() => new()
    {
        Format = DateTimePickerFormat.Short,
        ShowCheckBox = true,
        Checked = false,
        Width = 112,
        Font = new Font("Segoe UI", 8.5f),
        Margin = new Padding(2, 3, 4, 0)
    };
}


