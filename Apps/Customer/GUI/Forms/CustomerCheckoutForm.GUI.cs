using TrangSach.Shared.DTO;

namespace TrangSach.Customer.GUI;

internal sealed class CustomerCheckoutForm : Form
{
    private static readonly Color Forest = Color.FromArgb(53, 86, 68);
    private static readonly Color Ink = Color.FromArgb(38, 45, 37);
    private static readonly Color Paper = Color.FromArgb(251, 250, 247);

    private sealed record PaymentOption(string Caption, string Code)
    {
        public override string ToString() => Caption;
    }

    private readonly TextBox _address = new();
    private readonly TextBox _phone = new();
    private readonly TextBox _voucher = new();
    private readonly ComboBox _payment = new();
    private readonly List<StoreCartLineDto> _cart;
    public CheckoutDetailsDto? Details { get; private set; }

    public CustomerCheckoutForm(List<StoreCartLineDto> cart, string? address = null, string? phone = null)
    {
        _cart = cart;
        Text = "Thanh toán | Trang Sách";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 660);
        Size = new Size(1020, 740);
        BackColor = Paper;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26), ColumnCount = 1, RowCount = 3, BackColor = Paper };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.Controls.Add(new Panel
        {
            Dock = DockStyle.Fill,
            Controls =
            {
                new Label { Text = "THANH TOÁN", AutoSize = true, Location = new Point(0, 0), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Forest },
                new Label { Text = "Xác nhận hóa đơn", AutoSize = true, Location = new Point(0, 23), Font = new Font("Georgia", 22, FontStyle.Bold), ForeColor = Ink },
                new Label { Text = "Kiểm tra sách và điền thông tin nhận hàng trước khi đặt.", AutoSize = true, Location = new Point(0, 54), Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(105, 113, 102) }
            }
        }, 0, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(0, 0, 0, 0) };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        var cartPanel = BuildCartSummary();
        cartPanel.Margin = new Padding(0, 0, 9, 0);
        var shippingPanel = BuildShippingPanel(address, phone);
        shippingPanel.Margin = new Padding(9, 0, 0, 0);
        body.Controls.Add(cartPanel, 0, 0);
        body.Controls.Add(shippingPanel, 1, 0);
        root.Controls.Add(body, 0, 1);

        var footer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 14, 0, 0) };
        var cancel = new Button
        {
            Text = "Quay lại giỏ", Width = 130, Height = 42, Left = 0, Top = 12,
            FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Forest,
            Font = new Font("Segoe UI", 9, FontStyle.Bold), DialogResult = DialogResult.Cancel
        };
        var confirm = new Button
        {
            Text = "Đặt hàng", Width = 160, Height = 42, Top = 12,
            FlatStyle = FlatStyle.Flat, BackColor = Forest, ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold), Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        confirm.Click += (_, _) => Confirm();
        footer.Controls.AddRange([cancel, confirm]);
        footer.Resize += (_, _) => confirm.Left = footer.ClientSize.Width - confirm.Width;
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
        AcceptButton = confirm;
        CancelButton = cancel;
    }

    private Control BuildCartSummary()
    {
        var panel = CardPanel();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.Controls.Add(SectionHeading("Sản phẩm trong giỏ"), 0, 0);

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, DataSource = _cart.ToList(),
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White, BorderStyle = BorderStyle.None, EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(235, 239, 230), ForeColor = Forest, Font = new Font("Segoe UI", 9, FontStyle.Bold) },
            DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Segoe UI", 9), SelectionBackColor = Color.FromArgb(232, 237, 227), SelectionForeColor = Ink }
        };
        HideColumn(grid, nameof(StoreCartLineDto.BookId));
        HideColumn(grid, nameof(StoreCartLineDto.AvailableStock));
        SetColumnHeader(grid, nameof(StoreCartLineDto.Title), "Sách");
        SetColumnHeader(grid, nameof(StoreCartLineDto.Author), "Tác giả");
        SetColumnHeader(grid, nameof(StoreCartLineDto.UnitPrice), "Đơn giá");
        SetColumnHeader(grid, nameof(StoreCartLineDto.Quantity), "SL");
        SetColumnHeader(grid, nameof(StoreCartLineDto.LineTotal), "Thành tiền");
        layout.Controls.Add(grid, 0, 1);

        var count = _cart.Sum(item => item.Quantity);
        layout.Controls.Add(new Label { Text = $"{count} sản phẩm", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(105, 113, 102), TextAlign = ContentAlignment.MiddleRight }, 0, 2);
        var amount = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(242, 245, 238), Padding = new Padding(12, 5, 12, 5) };
        amount.Controls.Add(new Label { Text = "Tạm tính", Dock = DockStyle.Left, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Ink, TextAlign = ContentAlignment.MiddleLeft });
        amount.Controls.Add(new Label { Text = _cart.Sum(item => item.LineTotal).ToString("N0") + " ₫", Dock = DockStyle.Right, AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Forest, TextAlign = ContentAlignment.MiddleRight });
        layout.Controls.Add(amount, 0, 3);
        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildShippingPanel(string? address, string? phone)
    {
        var panel = CardPanel();
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 9 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(SectionHeading("Thông tin nhận hàng"), 0, 0);

        _address.Text = address ?? string.Empty;
        _address.Multiline = true;
        _address.ScrollBars = ScrollBars.Vertical;
        _address.PlaceholderText = "Số nhà, đường, phường/xã, tỉnh/thành";
        AddField(layout, "Địa chỉ giao hàng", _address, 1, 2);

        _phone.Text = phone ?? string.Empty;
        _phone.PlaceholderText = "Số điện thoại người nhận";
        AddField(layout, "Số điện thoại", _phone, 3, 4);

        _voucher.PlaceholderText = "Nhập mã nếu bạn có";
        AddField(layout, "Mã voucher", _voucher, 5, 6);

        _payment.DropDownStyle = ComboBoxStyle.DropDownList;
        _payment.Items.AddRange([
            new PaymentOption("Thanh toán khi nhận hàng", "CashOnDelivery"),
            new PaymentOption("Chuyển khoản (mô phỏng)", "BankTransfer")]);
        _payment.SelectedIndex = 0;
        AddField(layout, "Phương thức thanh toán", _payment, 7, 8);
        panel.Controls.Add(layout);
        return panel;
    }

    private void Confirm()
    {
        var payment = _payment.SelectedItem as PaymentOption;
        if (string.IsNullOrWhiteSpace(_address.Text) || string.IsNullOrWhiteSpace(_phone.Text))
        {
            MessageBox.Show(this, "Nhập địa chỉ giao hàng và số điện thoại người nhận.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Details = new CheckoutDetailsDto(_address.Text.Trim(), _phone.Text.Trim(), payment?.Code ?? "CashOnDelivery",
            string.IsNullOrWhiteSpace(_voucher.Text) ? null : _voucher.Text.Trim());
        DialogResult = DialogResult.OK;
        Close();
    }

    private static Panel CardPanel() => new() { Dock = DockStyle.Fill, BackColor = Color.White, Margin = Padding.Empty };
    private static Label SectionHeading(string text) => new() { Text = text, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Ink, TextAlign = ContentAlignment.MiddleLeft };

    private static void AddField(TableLayoutPanel parent, string caption, Control field, int captionRow, int fieldRow)
    {
        parent.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Forest, TextAlign = ContentAlignment.BottomLeft }, 0, captionRow);
        field.Dock = DockStyle.Fill;
        field.Font = new Font("Segoe UI", 10);
        parent.Controls.Add(field, 0, fieldRow);
    }

    private static void HideColumn(DataGridView grid, string name)
    {
        if (grid.Columns[name] is { } column) column.Visible = false;
    }

    private static void SetColumnHeader(DataGridView grid, string name, string header)
    {
        if (grid.Columns[name] is { } column) column.HeaderText = header;
    }
}
