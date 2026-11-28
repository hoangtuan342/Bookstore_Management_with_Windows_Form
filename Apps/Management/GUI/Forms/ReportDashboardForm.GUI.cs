using System.Data;
using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

public sealed class ReportDashboardForm : Form
{
    private static readonly Color Ink = Color.FromArgb(52, 79, 61), Green = Color.FromArgb(69, 107, 80),
        Muted = Color.FromArgb(129, 135, 125), Line = Color.FromArgb(231, 233, 226), Canvas = Color.FromArgb(247, 248, 244);

    // Scrollable container for seamless single-page layout (Không dùng TabControl)
    private readonly Panel _scrollContainer = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Canvas };
    private readonly FlowLayoutPanel _contentFlow = new()
    {
        Dock = DockStyle.Top,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(16, 12, 16, 24),
        BackColor = Color.Transparent
    };

    // Header Panel chứa Tiêu đề và Các nút xuất dữ liệu rõ ràng bên phải
    private readonly Panel _topPanel = new() { Height = 64, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 0, 12) };

    // 5.a Card & Grid
    private readonly Panel _card5a = new() { Height = 190, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 12) };
    private readonly DateTimePicker _dtpFrom = new() { Format = DateTimePickerFormat.Short, Width = 110, Font = new Font("Segoe UI", 9f) };
    private readonly DateTimePicker _dtpTo = new() { Format = DateTimePickerFormat.Short, Width = 110, Font = new Font("Segoe UI", 9f) };
    private readonly Label _lblSummary5a = new() { AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Green, Margin = new Padding(10, 6, 0, 0) };
    private readonly DataGridView _grid5a = CreateStyledGrid();

    // 5.b Card & Grid: Chiều cao khít với 3 dòng dữ liệu (Thu, Chi, Lợi nhuận)
    private readonly Panel _card5b = new() { Height = 150, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 12) };
    private readonly ComboBox _cboTimeView = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180, Font = new Font("Segoe UI", 9f) };
    private readonly NumericUpDown _numYear5b = new() { Minimum = 2000, Maximum = 2100, Value = DateTime.Today.Year, Width = 70, Font = new Font("Segoe UI", 9f) };
    private readonly DataGridView _grid5b = CreateStyledGrid();

    // 5.c Card & Grids (NV và KH tách riêng 2 card xếp dọc)
    private readonly Panel _card5cEmp = new() { Height = 250, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 12) };
    private readonly DataGridView _grid5cEmp = CreateStyledGrid();

    private readonly Panel _card5cCust = new() { Height = 250, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 12) };
    private readonly DataGridView _grid5cCust = CreateStyledGrid();

    // 5.d Card & Grid (Sản phẩm)
    private readonly Panel _card5d = new() { Height = 320, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(12), Margin = new Padding(0, 0, 0, 16) };
    private readonly NumericUpDown _numYear5d = new() { Minimum = 2000, Maximum = 2100, Value = DateTime.Today.Year, Width = 70, Font = new Font("Segoe UI", 9f) };
    private readonly DataGridView _grid5d = CreateStyledGrid();

    private ManagerReportDto? _report;
    private ManagerReportDto? _historyReport;

    public event Action<int>? PeriodSelected;
    public event Action<DateOnly, DateOnly>? DateRangeSelected;
    public event Action<ManagerReportDto>? FullDailyTrendRequested;
    public event Action<ManagerReportDto>? ExportExcelRequested;
    public event Action<ManagerReportDto>? ExportCsvRequested;

    public ReportDashboardForm()
    {
        TopLevel = false;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        BackColor = Canvas;

        BuildTopHeader();
        Build5aSection();
        Build5bSection();
        Build5cSections();
        Build5dSection();

        _contentFlow.Controls.AddRange([_topPanel, _card5a, _card5b, _card5cEmp, _card5cCust, _card5d]);
        _scrollContainer.Controls.Add(_contentFlow);
        Controls.Add(_scrollContainer);

        _scrollContainer.Resize += (_, _) => UpdateCardWidths();

        _dtpFrom.Value = DateTime.Today.AddDays(-30);
        _dtpTo.Value = DateTime.Today;
    }

    private void UpdateCardWidths()
    {
        var targetWidth = Math.Max(780, _scrollContainer.ClientSize.Width - 42);
        _topPanel.Width = targetWidth;
        _card5a.Width = targetWidth;
        _card5b.Width = targetWidth;
        _card5cEmp.Width = targetWidth;
        _card5cCust.Width = targetWidth;
        _card5d.Width = targetWidth;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x02000000;
            return cp;
        }
    }

    private void BuildTopHeader()
    {
        var lblKicker = new Label { Text = "MỤC 5. THỐNG KÊ DOANH THU & ĐỐI SOÁT TÀI CHÍNH", AutoSize = true, ForeColor = Green, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Location = new Point(14, 7) };
        var lblTitle = new Label { Text = "Báo cáo thống kê cửa hàng", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI", 15f, FontStyle.Bold), Location = new Point(12, 23) };

        // Thanh nút bấm luôn căn sát lề phải màn hình để người dùng luôn thấy
        var actionsBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 16, 12, 0)
        };

        var btnTrend = CreateBtn("Diễn biến ngày", Color.FromArgb(240, 244, 241), Ink);
        btnTrend.Click += (_, _) => { if (_report is not null) FullDailyTrendRequested?.Invoke(_report); };
        actionsBar.Controls.Add(btnTrend);

        var btnExcel = CreateBtn("Xuất Excel", Color.FromArgb(232, 243, 235), Green);
        btnExcel.Click += (_, _) => { if (_report is not null) ExportExcelRequested?.Invoke(_report); };
        actionsBar.Controls.Add(btnExcel);

        var btnCsv = CreateBtn("Xuất CSV", Color.FromArgb(240, 244, 241), Ink);
        btnCsv.Click += (_, _) => { if (_report is not null) ExportCsvRequested?.Invoke(_report); };
        actionsBar.Controls.Add(btnCsv);

        _topPanel.Controls.AddRange([actionsBar, lblKicker, lblTitle]);
    }

    private void Build5aSection()
    {
        var header5a = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false, BackColor = Color.Transparent };

        var lbl5a = new Label
        {
            Text = "5.a) THỐNG KÊ TỔNG TIỀN HÓA ĐƠN TỪ NGÀY a ĐẾN NGÀY b",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Green,
            Margin = new Padding(0, 5, 8, 0)
        };
        header5a.Controls.Add(lbl5a);

        header5a.Controls.Add(new Label { Text = "Từ ngày:", AutoSize = true, Margin = new Padding(6, 5, 2, 0), ForeColor = Muted, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        header5a.Controls.Add(_dtpFrom);
        header5a.Controls.Add(new Label { Text = "Đến ngày:", AutoSize = true, Margin = new Padding(6, 5, 2, 0), ForeColor = Muted, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        header5a.Controls.Add(_dtpTo);

        var btnFilter = CreateBtn("Xem", Green, Color.White);
        btnFilter.Click += (_, _) => DateRangeSelected?.Invoke(DateOnly.FromDateTime(_dtpFrom.Value), DateOnly.FromDateTime(_dtpTo.Value));
        header5a.Controls.Add(btnFilter);

        header5a.Controls.Add(_lblSummary5a);

        _card5a.Controls.Add(_grid5a);
        _card5a.Controls.Add(header5a);
    }

    private void Build5bSection()
    {
        var top5b = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32, WrapContents = false, BackColor = Color.Transparent };
        var lbl5b = new Label
        {
            Text = "5.b) THỐNG KÊ TỔNG TIỀN HĐ THEO THÁNG / QUÝ / NĂM / CÁC NĂM (THU - CHI - LỢI NHUẬN)",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Green,
            Margin = new Padding(0, 5, 8, 0)
        };
        top5b.Controls.Add(lbl5b);

        _cboTimeView.Items.AddRange(["Theo Quý (Chuẩn đề bài)", "Theo Tháng trong năm", "Theo Các năm"]);
        _cboTimeView.SelectedIndex = 0;
        _cboTimeView.SelectedIndexChanged += (_, _) => RenderTimeMatrix();
        top5b.Controls.Add(new Label { Text = "Xem:", AutoSize = true, Margin = new Padding(8, 5, 2, 0), Font = new Font("Segoe UI", 9f) });
        top5b.Controls.Add(_cboTimeView);

        top5b.Controls.Add(new Label { Text = "Năm:", AutoSize = true, Margin = new Padding(8, 5, 2, 0), Font = new Font("Segoe UI", 9f) });
        _numYear5b.ValueChanged += (_, _) => RenderTimeMatrix();
        top5b.Controls.Add(_numYear5b);

        _card5b.Controls.Add(_grid5b);
        _card5b.Controls.Add(top5b);
    }

    private void Build5cSections()
    {
        // 5.c.1 Nhân viên
        var lblEmp = new Label
        {
            Text = "5.c) THỐNG KÊ TỔNG TIỀN HĐ THEO NHÂN VIÊN VÀ NĂM (MA TRẬN NV x NĂM)",
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Green
        };
        _card5cEmp.Controls.Add(_grid5cEmp);
        _card5cEmp.Controls.Add(lblEmp);

        // 5.c.2 Khách hàng đặt ngay bên dưới
        var lblCust = new Label
        {
            Text = "5.c) THỐNG KÊ TỔNG TIỀN HĐ THEO KHÁCH HÀNG VÀ NĂM (TƯƠNG TỰ CHO KHÁCH HÀNG - MA TRẬN KH x NĂM)",
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Green
        };
        _card5cCust.Controls.Add(_grid5cCust);
        _card5cCust.Controls.Add(lblCust);
    }

    private void Build5dSection()
    {
        var top5d = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32, WrapContents = false, BackColor = Color.Transparent };
        var lbl5d = new Label
        {
            Text = "5.d) THỐNG KÊ TỔNG TIỀN HĐ THEO SẢN PHẨM & QUÝ - NĂM",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Green,
            Margin = new Padding(0, 5, 8, 0)
        };
        top5d.Controls.Add(lbl5d);

        top5d.Controls.Add(new Label { Text = "Chọn năm:", AutoSize = true, Margin = new Padding(8, 5, 2, 0), Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
        _numYear5d.ValueChanged += (_, _) => RenderProductMatrix();
        top5d.Controls.Add(_numYear5d);

        _card5d.Controls.Add(_grid5d);
        _card5d.Controls.Add(top5d);
    }

    public void SetLoading(int days)
    {
        _lblSummary5a.Text = $"Đang tải dữ liệu {days} ngày...";
    }

    public void SetLoading(DateOnly from, DateOnly to)
    {
        _lblSummary5a.Text = $"Đang tải từ {from:dd/MM/yyyy} đến {to:dd/MM/yyyy}...";
    }

    public void ShowReport(ManagerReportDto report, ManagerReportDto historyReport)
    {
        _report = report;
        _historyReport = historyReport;

        _dtpFrom.Value = report.FromDate;
        _dtpTo.Value = report.ToDate;
        _numYear5b.Value = Math.Clamp(report.ToDate.Year, 2000, 2100);
        _numYear5d.Value = Math.Clamp(report.ToDate.Year, 2000, 2100);

        _lblSummary5a.Text = $"Thu: {report.Revenue:N0} đ | Đơn: {report.CompletedOrders:N0} | Chi: {report.PurchaseSpend:N0} đ";

        UpdateCardWidths();

        Render5aGrid(report);
        RenderTimeMatrix();
        RenderEmployeeMatrix();
        RenderCustomerMatrix();
        RenderProductMatrix();
    }

    private void Render5aGrid(ManagerReportDto r)
    {
        var dt = new DataTable();
        dt.Columns.Add("Chỉ số thống kê", typeof(string));
        dt.Columns.Add("Giá trị", typeof(string));
        dt.Columns.Add("Ghi chú đối soát", typeof(string));

        dt.Rows.Add("Tổng số hóa đơn phát sinh", r.TotalOrders.ToString("N0"), $"{r.CompletedOrders} đơn hoàn tất, {r.PendingOrders} chờ xử lý, {r.CancelledOrders} đã hủy");
        dt.Rows.Add("HĐ (Tổng thu bán hàng)", $"{r.Revenue:N0} đ", "Tổng tiền bán hàng thực tế sau giảm giá");
        dt.Rows.Add("PNH (Tổng chi nhập hàng)", $"{r.PurchaseSpend:N0} đ", "Tổng giá trị phiếu nhập kho trong cùng kỳ");
        dt.Rows.Add("Lợi nhuận (Thu - Chi)", $"{(r.Revenue - r.PurchaseSpend):N0} đ", "Chênh lệch dòng tiền trực tiếp giữa bán và nhập");

        _grid5a.DataSource = dt;
        FormatGridColumns(_grid5a);
    }

    private void RenderTimeMatrix()
    {
        var h = _historyReport ?? _report;
        if (h is null) return;

        var dt = new DataTable();
        var mode = _cboTimeView.SelectedIndex; // 0: Quý, 1: Tháng, 2: Các năm
        var selectedYear = (int)_numYear5b.Value;

        if (mode == 0)
        {
            dt.Columns.Add($"{selectedYear}", typeof(string));
            dt.Columns.Add("Q1", typeof(string));
            dt.Columns.Add("Q2", typeof(string));
            dt.Columns.Add("Q3", typeof(string));
            dt.Columns.Add("Q4", typeof(string));
            dt.Columns.Add("TC", typeof(string));

            var quarters = h.RevenueByQuarter.Where(x => x.Year == selectedYear).ToList();
            var qRev = new decimal[4];
            var qCost = new decimal[4];

            foreach (var q in quarters)
            {
                if (q.Quarter is >= 1 and <= 4)
                {
                    qRev[q.Quarter - 1] = q.Revenue;
                    qCost[q.Quarter - 1] = q.PurchaseSpend;
                }
            }

            var sumRev = qRev.Sum();
            var sumCost = qCost.Sum();

            dt.Rows.Add("HĐ (Tổng thu)", $"{qRev[0]:N0}", $"{qRev[1]:N0}", $"{qRev[2]:N0}", $"{qRev[3]:N0}", $"{sumRev:N0}");
            dt.Rows.Add("PNH (Tổng chi)", $"{qCost[0]:N0}", $"{qCost[1]:N0}", $"{qCost[2]:N0}", $"{qCost[3]:N0}", $"{sumCost:N0}");
            dt.Rows.Add("Lợi nhuận", $"{(qRev[0] - qCost[0]):N0}", $"{(qRev[1] - qCost[1]):N0}", $"{(qRev[2] - qCost[2]):N0}", $"{(qRev[3] - qCost[3]):N0}", $"{(sumRev - sumCost):N0}");
        }
        else if (mode == 1)
        {
            dt.Columns.Add($"Năm {selectedYear}", typeof(string));
            for (int m = 1; m <= 12; m++) dt.Columns.Add($"T{m}", typeof(string));
            dt.Columns.Add("TC", typeof(string));

            var months = h.RevenueByMonth.Where(x => x.Year == selectedYear).ToList();
            var mRev = new decimal[12];
            var mCost = new decimal[12];

            foreach (var m in months)
            {
                if (m.Month is >= 1 and <= 12)
                {
                    mRev[m.Month - 1] = m.Revenue;
                    mCost[m.Month - 1] = m.PurchaseSpend;
                }
            }

            var rRow = dt.NewRow();
            rRow[0] = "HĐ (Tổng thu)";
            for (int m = 0; m < 12; m++) rRow[m + 1] = $"{mRev[m]:N0}";
            rRow[13] = $"{mRev.Sum():N0}";
            dt.Rows.Add(rRow);

            var cRow = dt.NewRow();
            cRow[0] = "PNH (Tổng chi)";
            for (int m = 0; m < 12; m++) cRow[m + 1] = $"{mCost[m]:N0}";
            cRow[13] = $"{mCost.Sum():N0}";
            dt.Rows.Add(cRow);

            var pRow = dt.NewRow();
            pRow[0] = "Lợi nhuận";
            for (int m = 0; m < 12; m++) pRow[m + 1] = $"{(mRev[m] - mCost[m]):N0}";
            pRow[13] = $"{(mRev.Sum() - mCost.Sum()):N0}";
            dt.Rows.Add(pRow);
        }
        else
        {
            var years = h.RevenueByYear.Select(x => x.Year).Distinct().OrderBy(y => y).ToList();
            if (years.Count == 0) years.Add(selectedYear);

            dt.Columns.Add("Chỉ tiêu", typeof(string));
            foreach (var y in years) dt.Columns.Add($"{y}", typeof(string));
            dt.Columns.Add("TC", typeof(string));

            var revList = years.Select(y => h.RevenueByYear.FirstOrDefault(x => x.Year == y)?.Revenue ?? 0m).ToList();
            var costList = years.Select(y => h.RevenueByYear.FirstOrDefault(x => x.Year == y)?.PurchaseSpend ?? 0m).ToList();

            var rRow = dt.NewRow();
            rRow[0] = "HĐ (Tổng thu)";
            for (int i = 0; i < years.Count; i++) rRow[i + 1] = $"{revList[i]:N0}";
            rRow[years.Count + 1] = $"{revList.Sum():N0}";
            dt.Rows.Add(rRow);

            var cRow = dt.NewRow();
            cRow[0] = "PNH (Tổng chi)";
            for (int i = 0; i < years.Count; i++) cRow[i + 1] = $"{costList[i]:N0}";
            cRow[years.Count + 1] = $"{costList.Sum():N0}";
            dt.Rows.Add(cRow);

            var pRow = dt.NewRow();
            pRow[0] = "Lợi nhuận";
            for (int i = 0; i < years.Count; i++) pRow[i + 1] = $"{(revList[i] - costList[i]):N0}";
            pRow[years.Count + 1] = $"{(revList.Sum() - costList.Sum()):N0}";
            dt.Rows.Add(pRow);
        }

        _grid5b.DataSource = dt;
        FormatGridColumns(_grid5b);
    }

    private void RenderEmployeeMatrix()
    {
        var h = _historyReport ?? _report;
        if (h is null) return;

        var dt = new DataTable();
        dt.Columns.Add("NV", typeof(string));

        var empData = h.RevenueByEmployeeYear;
        var years = empData.Select(x => x.Year).Distinct().OrderBy(y => y).ToList();
        if (years.Count == 0) years.Add(DateTime.Today.Year);

        foreach (var y in years) dt.Columns.Add($"{y}", typeof(string));
        dt.Columns.Add("TC", typeof(string));

        var employees = empData.GroupBy(x => new { x.EmployeeId, x.EmployeeName })
            .Select(g => g.Key).OrderBy(e => e.EmployeeName).ToList();

        var yearTotals = new decimal[years.Count];

        foreach (var emp in employees)
        {
            var row = dt.NewRow();
            row[0] = string.IsNullOrWhiteSpace(emp.EmployeeName) ? emp.EmployeeId : $"{emp.EmployeeName} ({emp.EmployeeId})";
            decimal rowTotal = 0m;

            for (int i = 0; i < years.Count; i++)
            {
                var y = years[i];
                var rev = empData.FirstOrDefault(x => x.EmployeeId == emp.EmployeeId && x.Year == y)?.Revenue ?? 0m;
                row[i + 1] = $"{rev:N0}";
                rowTotal += rev;
                yearTotals[i] += rev;
            }

            row[years.Count + 1] = $"{rowTotal:N0}";
            dt.Rows.Add(row);
        }

        var totalRow = dt.NewRow();
        totalRow[0] = "TC";
        for (int i = 0; i < years.Count; i++) totalRow[i + 1] = $"{yearTotals[i]:N0}";
        totalRow[years.Count + 1] = $"{yearTotals.Sum():N0}";
        dt.Rows.Add(totalRow);

        _grid5cEmp.DataSource = dt;
        FormatGridColumns(_grid5cEmp);
    }

    private void RenderCustomerMatrix()
    {
        var h = _historyReport ?? _report;
        if (h is null) return;

        var dt = new DataTable();
        dt.Columns.Add("KH", typeof(string));

        var custData = h.RevenueByCustomerYear;
        var years = custData.Select(x => x.Year).Distinct().OrderBy(y => y).ToList();
        if (years.Count == 0) years.Add(DateTime.Today.Year);

        foreach (var y in years) dt.Columns.Add($"{y}", typeof(string));
        dt.Columns.Add("TC", typeof(string));

        var customers = custData.GroupBy(x => new { x.CustomerId, x.CustomerName })
            .Select(g => g.Key).OrderBy(c => c.CustomerName).ToList();

        var yearTotals = new decimal[years.Count];

        foreach (var cust in customers)
        {
            var row = dt.NewRow();
            var name = string.IsNullOrWhiteSpace(cust.CustomerName) ? "Khách vãng lai" : cust.CustomerName;
            row[0] = $"{name} ({cust.CustomerId})";
            decimal rowTotal = 0m;

            for (int i = 0; i < years.Count; i++)
            {
                var y = years[i];
                var rev = custData.FirstOrDefault(x => x.CustomerId == cust.CustomerId && x.Year == y)?.Revenue ?? 0m;
                row[i + 1] = $"{rev:N0}";
                rowTotal += rev;
                yearTotals[i] += rev;
            }

            row[years.Count + 1] = $"{rowTotal:N0}";
            dt.Rows.Add(row);
        }

        var totalRow = dt.NewRow();
        totalRow[0] = "TC";
        for (int i = 0; i < years.Count; i++) totalRow[i + 1] = $"{yearTotals[i]:N0}";
        totalRow[years.Count + 1] = $"{yearTotals.Sum():N0}";
        dt.Rows.Add(totalRow);

        _grid5cCust.DataSource = dt;
        FormatGridColumns(_grid5cCust);
    }

    private void RenderProductMatrix()
    {
        var h = _historyReport ?? _report;
        if (h is null) return;

        var selectedYear = (int)_numYear5d.Value;
        var dt = new DataTable();

        dt.Columns.Add("SP", typeof(string));
        dt.Columns.Add("Q1", typeof(string));
        dt.Columns.Add("Q2", typeof(string));
        dt.Columns.Add("Q3", typeof(string));
        dt.Columns.Add("Q4", typeof(string));
        dt.Columns.Add("TC", typeof(string));

        var prodData = h.ProductSalesByQuarter.Where(x => x.Year == selectedYear).ToList();
        var products = prodData.GroupBy(x => new { x.BookId, x.Title }).Select(g => g.Key).OrderBy(p => p.Title).ToList();

        var qTotals = new decimal[4];

        foreach (var p in products)
        {
            var row = dt.NewRow();
            row[0] = $"{p.Title} ({p.BookId})";
            decimal rowTotal = 0m;

            for (int q = 1; q <= 4; q++)
            {
                var rev = prodData.FirstOrDefault(x => x.BookId == p.BookId && x.Quarter == q)?.Revenue ?? 0m;
                row[q] = $"{rev:N0}";
                rowTotal += rev;
                qTotals[q - 1] += rev;
            }

            row[5] = $"{rowTotal:N0}";
            dt.Rows.Add(row);
        }

        var totalRow = dt.NewRow();
        totalRow[0] = "TC";
        for (int q = 0; q < 4; q++) totalRow[q + 1] = $"{qTotals[q]:N0}";
        totalRow[5] = $"{qTotals.Sum():N0}";
        dt.Rows.Add(totalRow);

        _grid5d.DataSource = dt;
        FormatGridColumns(_grid5d);
    }

    private static DataGridView CreateStyledGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoGenerateColumns = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            GridColor = Color.FromArgb(220, 226, 220),
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 30,
            RowTemplate = { Height = 28 }
        };

        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 246, 243);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(52, 79, 61);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        grid.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f);
        grid.DefaultCellStyle.ForeColor = Color.FromArgb(40, 40, 40);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 239, 235);
        grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 250);

        return grid;
    }

    private static void FormatGridColumns(DataGridView grid)
    {
        if (grid.Columns.Count == 0) return;

        grid.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        grid.Columns[0].FillWeight = 160;

        for (int i = 1; i < grid.Columns.Count; i++)
        {
            grid.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.Columns[i].FillWeight = 100;
        }

        if (grid.Rows.Count > 0)
        {
            var lastRow = grid.Rows[^1];
            var firstCell = lastRow.Cells[0].Value?.ToString();
            if (firstCell is "TC" or "Lợi nhuận")
            {
                lastRow.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                lastRow.DefaultCellStyle.BackColor = Color.FromArgb(235, 245, 238);
                lastRow.DefaultCellStyle.ForeColor = Green;
            }
        }
    }

    private static Button CreateBtn(string text, Color backColor, Color textColor)
    {
        return new Button
        {
            Text = text,
            BackColor = backColor,
            ForeColor = textColor,
            FlatStyle = FlatStyle.Flat,
            Height = 28,
            AutoSize = true,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Margin = new Padding(4, 3, 4, 3),
            Padding = new Padding(10, 0, 10, 0)
        };
    }
}
