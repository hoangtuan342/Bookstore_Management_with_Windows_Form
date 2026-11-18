using System.Drawing.Drawing2D;
using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

public sealed class RevenueTrendDetailForm : Form
{
    private static readonly Color Ink = Color.FromArgb(52,79,61);
    private static readonly Color Green = Color.FromArgb(69,107,80);
    private static readonly Color Gold = Color.FromArgb(212,162,104);
    private static readonly Color Muted = Color.FromArgb(129,135,125);
    private static readonly Color Line = Color.FromArgb(231,233,226);

    public RevenueTrendDetailForm(ManagerReportDto report)
    {
        Text = "Chi tiết doanh thu theo ngày";
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(900, 600);
        BackColor = Color.FromArgb(247,248,244);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(14), BackColor = BackColor };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        var heading = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"DOANH THU THEO NGÀY   ·   {report.FromDate:dd/MM/yyyy} – {report.ToDate:dd/MM/yyyy}      |      Xanh: doanh thu     Vàng: nhập hàng",
            ForeColor = Ink, Font = new Font("Segoe UI", 12, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };
        var chart = new RevenueHistoryChart(report.RevenueByDayDto) { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(0,0,0,8) };
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AutoGenerateColumns = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None, RowHeadersVisible = false, DataSource = report.RevenueByDayDto.OrderBy(item => item.Date).ToList()
        };
        grid.DataBindingComplete += (_, _) =>
        {
            SetHeader(grid, nameof(RevenueByDayDto.Date), "Ngày");
            SetHeader(grid, nameof(RevenueByDayDto.Revenue), "Doanh thu");
            SetHeader(grid, nameof(RevenueByDayDto.PurchaseSpend), "Tiền nhập hàng");
            SetHeader(grid, nameof(RevenueByDayDto.Orders), "Số đơn hoàn tất");
            if (grid.Columns[nameof(RevenueByDayDto.Date)] is { } date) date.DefaultCellStyle.Format = "dd/MM/yyyy";
            foreach (var key in new[] { nameof(RevenueByDayDto.Revenue), nameof(RevenueByDayDto.PurchaseSpend) })
                if (grid.Columns[key] is { } money) money.DefaultCellStyle.Format = "N0 \u20ab";
        };
        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(chart, 0, 1);
        layout.Controls.Add(grid, 0, 2);
        Controls.Add(layout);
    }

    private static void SetHeader(DataGridView grid, string name, string title)
    {
        if (grid.Columns[name] is { } column) column.HeaderText = title;
    }

    private sealed class RevenueHistoryChart(IReadOnlyList<RevenueByDayDto> rows) : Panel
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (rows.Count == 0)
            {
                TextRenderer.DrawText(e.Graphics, "Chưa có dữ liệu doanh thu trong kỳ.", Font, ClientRectangle, Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            const float left = 74, rightPadding = 18, top = 22, bottomPadding = 40;
            var right = Math.Max(left + 100, ClientSize.Width - rightPadding);
            var bottom = Math.Max(top + 80, ClientSize.Height - bottomPadding);
            var height = bottom - top;
            var max = Ceiling(rows.Max(row => Math.Max(row.Revenue, row.PurchaseSpend)));
            using var grid = new Pen(Line, 1);
            using var axis = new SolidBrush(Muted);
            using var green = new SolidBrush(Green);
            using var gold = new SolidBrush(Gold);
            using var font = new Font("Segoe UI", 8);
            using var rightFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            using var centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            for (var tick = 0; tick <= 4; tick++)
            {
                var y = top + height * tick / 4f;
                e.Graphics.DrawLine(grid, left, y, right, y);
                e.Graphics.DrawString(Compact(max * (4 - tick) / 4m), font, axis, new RectangleF(0, y - 8, left - 8, 16), rightFormat);
            }
            var slot = (right - left) / rows.Count;
            var bar = Math.Clamp(slot * .28f, 2f, 12f);
            var labelStep = Math.Max(1, (rows.Count + 11) / 12);
            for (var i = 0; i < rows.Count; i++)
            {
                var x = left + (i + .5f) * slot;
                Draw(rows[i].Revenue, x - bar - 1, green);
                Draw(rows[i].PurchaseSpend, x + 1, gold);
                if (i % labelStep == 0 || i == rows.Count - 1)
                    e.Graphics.DrawString(rows[i].Date.ToString("dd/MM"), font, axis,
                        new RectangleF(x - slot * 1.5f, bottom + 7, slot * 3, 20), centerFormat);
            }
            void Draw(decimal value, float x, Brush brush)
            {
                if (value <= 0) return;
                var barHeight = Math.Max(1, (float)(value / max) * height);
                e.Graphics.FillRectangle(brush, x, bottom - barHeight, Math.Max(1, bar), barHeight);
            }
        }

        private static decimal Ceiling(decimal value)
        {
            if (value <= 0) return 1;
            var magnitude = 1m;
            while (value >= magnitude * 10 && magnitude <= decimal.MaxValue / 10) magnitude *= 10;
            while (value < magnitude) magnitude /= 10;
            var normalized = value / magnitude;
            return (normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 2.5m ? 2.5m : normalized <= 5 ? 5 : 10) * magnitude;
        }

        private static string Compact(decimal value) => value >= 1_000_000 ? $"{value / 1_000_000:0.#}tr" : value >= 1_000 ? $"{value / 1_000:0.#}k" : value.ToString("0");
    }
}
