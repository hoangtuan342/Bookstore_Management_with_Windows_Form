using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

internal sealed class DashboardPageForm : Form
{
    private static readonly Color Ink = Color.FromArgb(35, 56, 72);
    private static readonly Color Muted = Color.FromArgb(116, 132, 143);
    private static readonly Color Canvas = Color.FromArgb(243, 247, 248);
    private readonly Label[] _values = new Label[4];
    private readonly Label[] _notes = new Label[4];
    private readonly RevenueWeekChart _trend = new();
    public DataGridView Grid { get; } = new();
    public event EventHandler? RefreshRequested;

    public DashboardPageForm()
    {
        TopLevel = false;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        Dock = DockStyle.Fill;
        BackColor = Canvas;
        BackgroundImage = ManagementForm.SharedBackgroundArtwork;
        BackgroundImageLayout = ImageLayout.Stretch;
        Padding = new Padding(18, 14, 18, 16);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Color.Transparent, Margin = Padding.Empty };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 236));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heading = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        heading.Controls.Add(new Label { Text = "Tổng quan", AutoSize = true, Location = new Point(2, 0), Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Ink });
        heading.Controls.Add(new Label { Text = "Theo dõi nhanh tình hình kinh doanh của cửa hàng.", AutoSize = true, Location = new Point(4, 32), Font = new Font("Segoe UI", 8.5f), ForeColor = Muted });
        var refresh = new Button { Text = "Làm mới", Width = 102, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right, Top = 8, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.FromArgb(51, 101, 123), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
        refresh.FlatAppearance.BorderColor = Color.FromArgb(207, 220, 225);
        refresh.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        heading.Controls.Add(refresh);
        heading.Resize += (_, _) => refresh.Left = heading.ClientSize.Width - refresh.Width - 2;

        var stats = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Color.Transparent, Margin = new Padding(-3, 0, -3, 4) };
        for (var i = 0; i < 4; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        stats.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var labels = new[] { "DOANH THU THÁNG NÀY", "ĐƠN HÀNG HÔM NAY", "SÁCH ĐANG BÁN", "KHÁCH HÀNG" };
        var accents = new[] { Color.FromArgb(48, 145, 132), Color.FromArgb(75, 137, 179), Color.FromArgb(224, 153, 78), Color.FromArgb(151, 113, 177) };
        for (var i = 0; i < 4; i++)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(4), Padding = new Padding(12, 9, 10, 6), BorderStyle = BorderStyle.FixedSingle };
            card.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accents[i] });
            card.Controls.Add(new Label { Text = labels[i], AutoSize = true, Location = new Point(15, 11), Font = new Font("Segoe UI", 7.3f, FontStyle.Bold), ForeColor = Muted });
            _values[i] = new Label { Text = "—", AutoSize = true, Location = new Point(15, 32), Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Ink };
            _notes[i] = new Label { Text = "Đang tải dữ liệu", AutoSize = true, Location = new Point(15, 73), Font = new Font("Segoe UI", 7.5f), ForeColor = Muted };
            card.Controls.Add(_values[i]); card.Controls.Add(_notes[i]); stats.Controls.Add(card, i, 0);
        }

        var trendCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14, 10, 14, 8), Margin = new Padding(1, 3, 1, 8), BorderStyle = BorderStyle.FixedSingle };
        trendCard.Controls.Add(_trend);
        _trend.Dock = DockStyle.Fill;
        var trendTitle = new Label { Text = "Doanh thu 7 ngày gần đây", Dock = DockStyle.Top, Height = 24, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Ink };
        trendCard.Controls.Add(trendTitle);
        var openReport = new LinkLabel { Text = "Mở báo cáo", AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Top = 13, LinkColor = Color.FromArgb(42, 117, 147), Font = new Font("Segoe UI", 8, FontStyle.Bold) };
        openReport.Tag = "reports";
        openReport.LinkClicked += (_, _) => OpenReportRequested?.Invoke(this, EventArgs.Empty);
        trendCard.Controls.Add(openReport);
        trendCard.Resize += (_, _) => openReport.Left = trendCard.ClientSize.Width - openReport.Width - 14;

        var ordersCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12, 9, 12, 10), Margin = new Padding(1, 0, 1, 0), BorderStyle = BorderStyle.FixedSingle };
        var orderHeading = new Label { Text = "Đơn hàng mới", Dock = DockStyle.Top, Height = 29, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Ink };
        Grid.Dock = DockStyle.Fill;
        Grid.ReadOnly = true; Grid.AllowUserToAddRows = false; Grid.AllowUserToDeleteRows = false; Grid.AllowUserToResizeRows = false;
        Grid.RowHeadersVisible = false; Grid.AutoGenerateColumns = false; Grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        Grid.BackgroundColor = Color.White; Grid.BorderStyle = BorderStyle.None; Grid.GridColor = Color.FromArgb(232, 238, 240);
        Grid.EnableHeadersVisualStyles = false; Grid.ColumnHeadersHeight = 32; Grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 246, 247); Grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(84, 105, 117); Grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8, FontStyle.Bold);
        Grid.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f); Grid.DefaultCellStyle.ForeColor = Ink; Grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 239, 235); Grid.DefaultCellStyle.SelectionForeColor = Ink;
        Grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 251); Grid.RowTemplate.Height = 31;
        Grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ManagerRecentOrderDto.Id), HeaderText = "MÃ ĐƠN", Width = 130 });
        Grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ManagerRecentOrderDto.CustomerName), HeaderText = "KHÁCH HÀNG", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 150 });
        Grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ManagerRecentOrderDto.CreatedAt), HeaderText = "NGÀY ĐẶT", Width = 145, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
        Grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ManagerRecentOrderDto.OrderStatus), HeaderText = "TRẠNG THÁI", Width = 130 });
        Grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(ManagerRecentOrderDto.TotalAmount), HeaderText = "TỔNG TIỀN", Width = 145, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } });
        ordersCard.Controls.Add(Grid); ordersCard.Controls.Add(orderHeading);

        layout.Controls.Add(heading, 0, 0); layout.Controls.Add(stats, 0, 1); layout.Controls.Add(trendCard, 0, 2); layout.Controls.Add(ordersCard, 0, 3);
        Controls.Add(layout);
    }

    public event EventHandler? OpenReportRequested;

    public void ShowDashboard(ManagerDashboardDto summary, ManagerReportDto trend)
    {
        _values[0].Text = summary.RevenueThisMonth.ToString("N0") + " ₫";
        _notes[0].Text = "Đơn hàng đã hoàn tất";
        _values[1].Text = summary.OrdersToday.ToString("N0");
        _notes[1].Text = $"{summary.PendingOrders:N0} đơn đang chờ";
        _values[2].Text = summary.ActiveBooks.ToString("N0");
        _notes[2].Text = $"{summary.LowStockBooks:N0} sách sắp hết";
        _values[3].Text = summary.CustomerCount.ToString("N0");
        _notes[3].Text = "Tổng khách trong hệ thống";
        Grid.DataSource = summary.RecentOrders.ToList();
        _trend.SetItems(trend.RevenueByDayDto.OrderBy(x => x.Date).TakeLast(7).ToArray());
    }

    private sealed class RevenueWeekChart : Panel
    {
        private IReadOnlyList<RevenueByDayDto> _items = [];
        private static readonly Color Bar = Color.FromArgb(68, 137, 112);
        public RevenueWeekChart() { BackColor = Color.White; SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true); }
        public void SetItems(IReadOnlyList<RevenueByDayDto> items) { _items = items; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_items.Count == 0) { TextRenderer.DrawText(e.Graphics, "Chưa có dữ liệu trong 7 ngày gần đây.", Font, ClientRectangle, Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); return; }
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var max = Math.Max(1m, _items.Max(x => x.Revenue)); var bottom = ClientSize.Height - 24; var chartHeight = Math.Max(20, bottom - 8);
            var slot = ClientSize.Width / (float)_items.Count; var barWidth = Math.Clamp(slot * .48f, 12f, 50f);
            using var brush = new SolidBrush(Bar); using var grid = new Pen(Color.FromArgb(235, 240, 241)); using var valueFont = new Font("Segoe UI", 7.5f); using var dateFont = new Font("Segoe UI", 7.5f); using var textBrush = new SolidBrush(Muted); using var format = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            for (var line = 0; line < 3; line++) { var y = 6 + line * chartHeight / 2f; e.Graphics.DrawLine(grid, 0, y, ClientSize.Width, y); }
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i]; var height = Math.Max(2f, (float)(item.Revenue / max) * (chartHeight - 5)); var x = i * slot + (slot - barWidth) / 2;
                e.Graphics.FillRectangle(brush, x, bottom - height, barWidth, height);
                var amount = item.Revenue >= 1_000_000 ? $"{item.Revenue / 1_000_000:0.#} tr" : item.Revenue >= 1_000 ? $"{item.Revenue / 1_000:0.#} k" : item.Revenue.ToString("0");
                e.Graphics.DrawString(amount, valueFont, textBrush, new RectangleF(i * slot, Math.Max(0, bottom - height - 16), slot, 14), format);
                e.Graphics.DrawString(item.Date.ToString("dd/MM"), dateFont, textBrush, new RectangleF(i * slot, bottom + 5, slot, 15), format);
            }
        }
    }
}
