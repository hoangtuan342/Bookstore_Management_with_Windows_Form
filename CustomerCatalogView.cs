using BookStore.Shared.Models;
using System.Drawing.Drawing2D;

namespace TrangSach.WinForms;

/// <summary>Customer storefront styled after the original Trang Sách web catalog.</summary>
public sealed class CustomerCatalogView : UserControl
{
    private readonly FlowLayoutPanel _items = new() { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, BackColor = Color.FromArgb(251, 250, 247), Padding = new Padding(0, 4, 0, 20) };
    private readonly FlowLayoutPanel _chips = new() { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.FromArgb(251, 250, 247) };
    private readonly TextBox _search = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), PlaceholderText = "⌕  Tìm tên sách, tác giả..." };
    private readonly Label _count = new() { AutoSize = true, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(115, 121, 111) };
    private readonly List<BookItem> _books = [];
    private string _category = "Tất cả";
    public event Action<BookItem>? AddToCartRequested;

    public CustomerCatalogView()
    {
        BackColor = Color.FromArgb(251, 250, 247); AutoScroll = true;
        var page = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(28, 20, 28, 36), BackColor = BackColor };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var hero = new Panel { Dock = DockStyle.Top, Height = 260, BackColor = Color.FromArgb(232, 237, 227), Margin = new Padding(0, 0, 0, 30) };
        hero.Paint += (_, e) => DrawHero(hero, e.Graphics);
        var headline = new Label { Text = "Mỗi cuốn sách,\nmột thế giới mới.", AutoSize = true, Location = new Point(30, 48), Font = new Font("Georgia", 29), ForeColor = Color.FromArgb(38, 45, 37), BackColor = Color.Transparent };
        var eyebrow = new Label { Text = "✳  MỘT GÓC NHỎ DÀNH CHO NGƯỜI YÊU SÁCH", AutoSize = true, Location = new Point(32, 22), Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(112, 123, 110), BackColor = Color.Transparent };
        var desc = new Label { Text = "Chọn một cuốn sách hay, dành cho mình một khoảng lặng và bắt đầu chuyến đi của riêng bạn.", Size = new Size(420, 40), Location = new Point(32, 136), Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(115, 121, 111), BackColor = Color.Transparent };
        hero.Controls.AddRange([headline, eyebrow, desc]);
        var heading = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = BackColor };
        heading.Controls.Add(new Label { Text = "ĐƯỢC BẠN ĐỌC YÊU THÍCH", AutoSize = true, Location = new Point(0, 0), Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(129, 135, 125) });
        heading.Controls.Add(new Label { Text = "Sách dành cho bạn", AutoSize = true, Location = new Point(0, 19), Font = new Font("Georgia", 20), ForeColor = Color.FromArgb(38, 45, 37) });
        var filter = new TableLayoutPanel { Dock = DockStyle.Top, Height = 46, ColumnCount = 2 };
        filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); filter.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 285));
        filter.Controls.Add(_chips, 0, 0); filter.Controls.Add(_search, 1, 0); _search.Margin = new Padding(4, 1, 0, 8);
        _search.TextChanged += (_, _) => Render();
        var meta = new Panel { Dock = DockStyle.Top, Height = 33 }; _count.Location = new Point(1, 7); meta.Controls.Add(_count);
        page.Controls.Add(hero, 0, 0); page.Controls.Add(heading, 0, 1); page.Controls.Add(filter, 0, 2); page.Controls.Add(meta, 0, 3); page.Controls.Add(_items, 0, 4);
        Controls.Add(page); Resize += (_, _) => ResizeCards();
    }

    public void SetBooks(IEnumerable<BookItem> books) { _books.Clear(); _books.AddRange(books); BuildChips(); Render(); }

    private void BuildChips()
    {
        _chips.Controls.Clear();
        foreach (var name in new[] { "Tất cả" }.Concat(_books.Select(b => b.Category).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c)))
        {
            var active = name == _category;
            var chip = new Button { Text = name, AutoSize = true, Height = 31, Margin = new Padding(0, 1, 7, 2), Padding = new Padding(10, 0, 10, 0), FlatStyle = FlatStyle.Flat, BackColor = active ? Color.FromArgb(53, 86, 68) : BackColor, ForeColor = active ? Color.White : Color.FromArgb(82, 91, 79), Font = new Font("Segoe UI", 8), Cursor = Cursors.Hand };
            chip.FlatAppearance.BorderColor = Color.FromArgb(224, 225, 217); chip.Click += (_, _) => { _category = name; BuildChips(); Render(); }; _chips.Controls.Add(chip);
        }
    }

    private void Render()
    {
        var found = _books.Where(b => (_category == "Tất cả" || b.Category == _category) && (string.IsNullOrWhiteSpace(_search.Text) || b.Title.Contains(_search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase) || b.Author.Contains(_search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase))).ToArray();
        _count.Text = $"{found.Length} cuốn sách"; _items.SuspendLayout(); _items.Controls.Clear();
        foreach (var book in found) _items.Controls.Add(Card(book));
        if (found.Length == 0) _items.Controls.Add(new Label { Text = "Chưa tìm thấy sách phù hợp. Thử từ khóa hoặc thể loại khác nhé.", AutoSize = true, Margin = new Padding(2, 30, 2, 50), ForeColor = Color.Gray });
        ResizeCards(); _items.ResumeLayout();
    }

    private Control Card(BookItem b)
    {
        var card = new Panel { Width = 205, Height = 366, Margin = new Padding(0, 0, 18, 18), BackColor = BackColor };
        var cover = new Panel { Location = new Point(1, 1), Size = new Size(198, 235), BackColor = ColorFor(b.CoverStyle) };
        cover.Paint += (_, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using var brush = new LinearGradientBrush(cover.ClientRectangle, ControlPaint.Light(cover.BackColor, .2f), ControlPaint.Dark(cover.BackColor, .14f), 135); e.Graphics.FillRectangle(brush, cover.ClientRectangle); using var p = new Pen(Color.FromArgb(90, 255, 255, 255), 2); e.Graphics.DrawLine(p, 12, 5, 12, cover.Height - 5); };
        cover.Controls.Add(new Label { Text = "T R A N G   S Á C H", Location = new Point(18, 14), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 7, FontStyle.Bold), BackColor = Color.Transparent });
        cover.Controls.Add(new Label { Text = b.Label, Location = new Point(126, 12), AutoSize = true, Padding = new Padding(4), BackColor = Color.FromArgb(245, 251, 250, 240), ForeColor = Color.FromArgb(70, 80, 68), Font = new Font("Segoe UI", 7, FontStyle.Bold) });
        cover.Controls.Add(new Label { Text = b.Title, Location = new Point(19, 112), Size = new Size(162, 78), ForeColor = Color.White, Font = new Font("Georgia", 15, FontStyle.Bold), BackColor = Color.Transparent });
        cover.Controls.Add(new Label { Text = b.Author, Location = new Point(20, 204), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 8), BackColor = Color.Transparent });
        card.Controls.Add(cover);
        card.Controls.Add(new Label { Text = b.Category.ToUpperInvariant(), Location = new Point(2, 245), AutoSize = true, ForeColor = Color.FromArgb(137, 142, 131), Font = new Font("Segoe UI", 7, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = b.Title, Location = new Point(2, 263), Size = new Size(198, 22), AutoEllipsis = true, ForeColor = Color.FromArgb(38, 45, 37), Font = new Font("Georgia", 11, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = b.Author, Location = new Point(2, 286), Size = new Size(198, 19), AutoEllipsis = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8) });
        card.Controls.Add(new Label { Text = b.Price.ToString("N0") + " ₫", Location = new Point(2, 315), AutoSize = true, ForeColor = Color.FromArgb(53, 86, 68), Font = new Font("Segoe UI", 10, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = b.AvailableStock > 0 ? $"Còn {b.AvailableStock} cuốn" : "Tạm hết hàng", Location = new Point(2, 340), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8) });
        var add = new Button { Text = b.AvailableStock > 0 ? "Thêm vào giỏ  +" : "Tạm hết hàng", Location = new Point(88, 312), Size = new Size(112, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(53, 86, 68), ForeColor = Color.White, Font = new Font("Segoe UI", 8, FontStyle.Bold), Enabled = b.AvailableStock > 0, Cursor = Cursors.Hand };
        add.Click += (_, _) => AddToCartRequested?.Invoke(b); card.Controls.Add(add); return card;
    }

    private void ResizeCards()
    {
        var width = Math.Max(600, ClientSize.Width - 95); var columns = Math.Max(1, width / 225); var cardWidth = Math.Max(190, (width - columns * 18) / columns);
        foreach (Control c in _items.Controls) if (c is Panel p && p.Height > 300) p.Width = cardWidth;
    }

    private static Color ColorFor(string style) => style switch { "cover-sand" => Color.FromArgb(190, 154, 100), "cover-coral" => Color.FromArgb(184, 102, 82), "cover-ink" => Color.FromArgb(57, 75, 80), "cover-blue" => Color.FromArgb(91, 120, 145), "cover-plum" => Color.FromArgb(121, 92, 102), _ => Color.FromArgb(104, 130, 115) };

    private static void DrawHero(Control panel, Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias; using var sun = new SolidBrush(Color.FromArgb(225, 217, 189)); g.FillEllipse(sun, panel.Width - 390, 22, 215, 215);
        var colors = new[] { Color.FromArgb(191, 121, 92), Color.FromArgb(70, 100, 86), Color.FromArgb(214, 189, 131) };
        var titles = new[] { "THE QUIET\nMIND", "NHỮNG NGÀY\nTHƠ ẤU", "ĐI TÌM MỘT\nBÌNH YÊN" };
        for (var i = 0; i < 3; i++) { var rect = new Rectangle(panel.Width - 365 + i * 72, 52 + (i == 1 ? 9 : 0), 108, 168); using var br = new SolidBrush(colors[i]); g.FillRectangle(br, rect); using var pen = new Pen(Color.FromArgb(65, 30, 40, 32), 2); g.DrawLine(pen, rect.Left + 9, rect.Top + 4, rect.Left + 9, rect.Bottom - 4); TextRenderer.DrawText(g, titles[i], new Font("Georgia", 11, FontStyle.Bold), Rectangle.Inflate(rect, -10, -12), i == 2 ? Color.FromArgb(41, 59, 49) : Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
        TextRenderer.DrawText(g, "GỢI Ý CHO MỘT NGÀY DỊU DÀNG", new Font("Segoe UI", 8, FontStyle.Bold), new Rectangle(panel.Width - 390, 232, 300, 18), Color.FromArgb(112, 123, 110), TextFormatFlags.HorizontalCenter);
    }
}
