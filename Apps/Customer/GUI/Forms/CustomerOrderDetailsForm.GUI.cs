using TrangSach.Shared.DTO;

namespace TrangSach.Customer.GUI;

internal sealed class CustomerOrderDetailsForm : Form
{
    public CustomerOrderDetailsForm(StoreOrderDto order)
    {
        Text = "Chi tiết hóa đơn";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(720, 560);
        Size = new Size(780, 620);
        BackColor = Color.FromArgb(251, 250, 247);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 7 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.Controls.Add(new Label { Text = "Chi tiết hóa đơn", Dock = DockStyle.Fill, Font = new Font("Georgia", 19, FontStyle.Bold), ForeColor = Color.FromArgb(38, 45, 37), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        root.Controls.Add(new Label { Text = $"Mã hóa đơn: {order.Id}\r\nNgày lập: {order.CreatedAt:dd/MM/yyyy HH:mm}", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(53, 86, 68), TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        root.Controls.Add(MakeInfo($"Trạng thái: {order.Status}    ·    Thanh toán: {order.PaymentStatus} ({PaymentName(order.PaymentMethod)})"), 0, 2);
        root.Controls.Add(MakeInfo($"Người nhận: {order.ReceiverPhone}    ·    Địa chỉ: {order.ShippingAddress}"), 0, 3);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, DataSource = order.Lines.ToList(),
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White, EnableHeadersVisualStyles = false
        };
        grid.Columns[nameof(StoreOrderLineDto.BookId)]!.Visible = false;
        grid.Columns[nameof(StoreOrderLineDto.Title)]!.HeaderText = "Sách";
        grid.Columns[nameof(StoreOrderLineDto.Quantity)]!.HeaderText = "Số lượng";
        grid.Columns[nameof(StoreOrderLineDto.UnitPrice)]!.HeaderText = "Đơn giá";
        grid.Columns[nameof(StoreOrderLineDto.LineTotal)]!.HeaderText = "Thành tiền";
        root.Controls.Add(grid, 0, 4);
        root.Controls.Add(MakeInfo($"Giảm giá: {order.DiscountAmount:N0} ₫"), 0, 5);
        root.Controls.Add(new Label { Text = $"Tổng thanh toán: {order.TotalAmount:N0} ₫", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(53, 86, 68) }, 0, 6);
        Controls.Add(root);
    }

    private static Label MakeInfo(string text) => new() { Text = text, Dock = DockStyle.Fill, AutoEllipsis = true, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(82, 91, 79), TextAlign = ContentAlignment.MiddleLeft };
    private static string PaymentName(string paymentMethod) => paymentMethod switch { "CashOnDelivery" => "Thanh toán khi nhận hàng", "BankTransfer" => "Chuyển khoản", _ => paymentMethod };
}
