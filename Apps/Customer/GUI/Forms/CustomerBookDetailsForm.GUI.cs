using TrangSach.Shared.DTO;

namespace TrangSach.Customer.GUI;

internal sealed class CustomerBookDetailsForm : Form
{
    private static readonly Color Forest = Color.FromArgb(53, 86, 68);
    private static readonly Color Ink = Color.FromArgb(38, 45, 37);
    private static readonly Color Paper = Color.FromArgb(251, 250, 247);
    private readonly BookItemDto _book;
    private TableLayoutPanel _root = null!;
    private static readonly Size DefaultClientSize = new(1040, 700);

    public bool BuyNowRequested { get; private set; }

    public CustomerBookDetailsForm(BookItemDto book)
    {
        _book = book;
        Text = "Chi tiết sản phẩm | Trang Sách";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        MinimumSize = new Size(900, 620);
        ClientSize = DefaultClientSize;
        BackColor = Paper;

        _root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26), ColumnCount = 1, RowCount = 3, BackColor = Paper };
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        _root.Controls.Add(new Label
        {
            Text = "TRANG SÁCH     /     CHI TIẾT SẢN PHẨM", Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Forest,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(0, 10, 0, 14) };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var cover = CreateCover(book);
        content.Controls.Add(cover, 0, 0);

        var details = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8, Padding = new Padding(28, 0, 0, 0) };
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        details.Controls.Add(Info(book.Category.ToUpperInvariant(), 9, FontStyle.Bold, Forest), 0, 0);
        details.Controls.Add(new Label
        {
            Text = book.Title, Dock = DockStyle.Fill, AutoEllipsis = true,
            Font = new Font("Georgia", 23, FontStyle.Bold), ForeColor = Ink, TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);
        details.Controls.Add(Info("Tác giả     " + book.Author, 10, FontStyle.Regular, Color.FromArgb(82, 91, 79)), 0, 2);
        details.Controls.Add(Info("Nhà xuất bản     " + book.Publisher, 10, FontStyle.Regular, Color.FromArgb(82, 91, 79)), 0, 3);
        details.Controls.Add(new Label
        {
            Text = book.Price.ToString("N0") + " ₫", Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 21, FontStyle.Bold), ForeColor = Forest, TextAlign = ContentAlignment.MiddleLeft
        }, 0, 4);
        details.Controls.Add(Info(book.AvailableStock > 0 ? $"Còn hàng · {book.AvailableStock} cuốn" : "Tạm hết hàng", 10, FontStyle.Bold,
            book.AvailableStock > 0 ? Forest : Color.FromArgb(161, 73, 58)), 0, 5);
        var description = new Label
        {
            Text = string.IsNullOrWhiteSpace(book.Description) ? "Chưa có mô tả cho sản phẩm này." : book.Description,
            Dock = DockStyle.Fill, AutoEllipsis = true, Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(82, 91, 79), Padding = new Padding(0, 10, 0, 0)
        };
        details.Controls.Add(description, 0, 6);
        details.Controls.Add(Info("Mã sản phẩm     " + book.Id, 8, FontStyle.Regular, Color.Gray), 0, 7);
        content.Controls.Add(details, 1, 0);
        _root.Controls.Add(content, 0, 1);

        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12, 10, 12, 8) };
        var add = new Button
        {
            Text = "Thêm vào giỏ", Width = 160, Height = 40, Left = 0, Top = 2,
            Enabled = book.AvailableStock > 0, FlatStyle = FlatStyle.Flat,
            BackColor = Color.White, ForeColor = Forest, Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        add.Left = footer.ClientSize.Width - 340;
        add.Click += (_, _) => CloseWithAction(false);
        var buy = new Button
        {
            Text = "Mua ngay", Width = 150, Height = 40, Left = footer.ClientSize.Width - 166, Top = 2,
            Enabled = book.AvailableStock > 0, FlatStyle = FlatStyle.Flat,
            BackColor = Forest, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        buy.Click += (_, _) => CloseWithAction(true);
        var cancel = new Button { Text = "Đóng", Width = 70, Height = 40, Left = 0, Top = 2, DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Top | AnchorStyles.Left };
        footer.Controls.AddRange([cancel, add, buy]);
        footer.Resize += (_, _) =>
        {
            buy.Left = footer.ClientSize.Width - buy.Width;
            add.Left = buy.Left - add.Width - 10;
        };
        _root.Controls.Add(footer, 0, 2);
        Controls.Add(_root);
        CancelButton = cancel;
    }

    private void CloseWithAction(bool buyNow)
    {
        BuyNowRequested = buyNow;
        DialogResult = DialogResult.OK;
        Close();
    }

    private Panel CreateCover(BookItemDto book)
    {
        var color = book.CoverStyle switch
        {
            "cover-sand" => Color.FromArgb(190, 154, 100), "cover-coral" => Color.FromArgb(184, 102, 82),
            "cover-ink" => Color.FromArgb(57, 75, 80), "cover-blue" => Color.FromArgb(91, 120, 145),
            "cover-plum" => Color.FromArgb(121, 92, 102), _ => Color.FromArgb(104, 130, 115)
        };
        var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 10, 0), BackColor = color };
        panel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(panel.ClientRectangle,
                ControlPaint.Light(color, .16f), ControlPaint.Dark(color, .12f), 135);
            e.Graphics.FillRectangle(gradient, panel.ClientRectangle);
            using var spine = new Pen(Color.FromArgb(90, 255, 255, 255), 3);
            e.Graphics.DrawLine(spine, 18, 8, 18, panel.Height - 8);
            TextRenderer.DrawText(e.Graphics, "T R A N G   S Á C H", new Font("Segoe UI", 8, FontStyle.Bold),
                new Rectangle(28, 20, panel.Width - 48, 26), Color.White, TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, book.Title, new Font("Georgia", 21, FontStyle.Bold),
                new Rectangle(30, 100, panel.Width - 54, 170), Color.White,
                TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            TextRenderer.DrawText(e.Graphics, book.Author, new Font("Segoe UI", 10),
                new Rectangle(30, panel.Height - 74, panel.Width - 52, 42), Color.White,
                TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        };
        return panel;
    }

    private static Label Info(string text, float size, FontStyle style, Color color) => new()
    {
        Text = text, Dock = DockStyle.Fill, AutoEllipsis = true,
        Font = new Font("Segoe UI", size, style), ForeColor = color, TextAlign = ContentAlignment.MiddleLeft
    };
}
