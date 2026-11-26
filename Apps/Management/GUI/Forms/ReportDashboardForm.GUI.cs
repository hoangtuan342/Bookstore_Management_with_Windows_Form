using TrangSach.Shared.DTO;
using System.Drawing.Drawing2D;

namespace TrangSach.Management.GUI;

public sealed class ReportDashboardForm : Form
{
    private static readonly Color Ink = Color.FromArgb(52,79,61), Green = Color.FromArgb(69,107,80),
        Gold = Color.FromArgb(212,162,104), Muted = Color.FromArgb(129,135,125),
        Line = Color.FromArgb(231,233,226), Canvas = Color.FromArgb(247,248,244);
    private readonly Panel _scroll = new() { Dock=DockStyle.Fill, AutoScroll=true, BackColor=Color.Transparent };
    private readonly FlowLayoutPanel _stack = new() { Dock=DockStyle.Top, FlowDirection=FlowDirection.TopDown, WrapContents=false, Padding=new Padding(16), BackColor=Color.Transparent };
    private readonly Panel _heading = new() { Height=140, Margin=new Padding(0,0,0,10), BackColor=Color.Transparent };
    private readonly TableLayoutPanel _stats = new() { Height=184, Margin=new Padding(0,0,0,8), ColumnCount=3, RowCount=2, BackColor=Color.Transparent };
    private readonly Panel _chartCard = new() { Height=220, Margin=new Padding(0,0,0,10), BackColor=Color.White, BorderStyle=BorderStyle.FixedSingle };
    private readonly Panel _chartHost = new() { Left=12, Top=74, Height=120, Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right, AutoScroll=false, BackColor=Color.White };
    private readonly RevenueChart _chart = new() { Height=244 };
    private readonly Panel _comparison = new() { Height=78, Margin=new Padding(0,0,0,8), BackColor=Color.White, BorderStyle=BorderStyle.FixedSingle };
    private readonly Panel _detailsPanel = new() { Height=390, Margin=new Padding(0,0,0,10), Padding=new Padding(8), BackColor=Color.White, BorderStyle=BorderStyle.FixedSingle };
    private readonly TabControl _detailTabs = new() { Dock=DockStyle.Fill, Font=new Font("Segoe UI",8f) };
    private readonly Dictionary<string,DataGridView> _detailGrids = new(StringComparer.Ordinal);
    private readonly Panel _footer = new() { Height=46, Margin=new Padding(0,0,0,10), BackColor=Color.Transparent };
    private readonly TableLayoutPanel _body = new() { Margin=new Padding(0,0,0,6), ColumnCount=1, RowCount=2, BackColor=Color.Transparent };
    private readonly FlowLayoutPanel _periods = new() { Location=new Point(0,64), Height=31, AutoSize=false, WrapContents=false, BackColor=Color.FromArgb(233,237,231), Padding=new Padding(3) };
    private readonly FlowLayoutPanel _rangeTools = new() { Location=new Point(0,101), Height=32, AutoScroll=true, WrapContents=false, BackColor=Color.Transparent, Padding=Padding.Empty };
    private readonly DateTimePicker _fromDate = new() { Format=DateTimePickerFormat.Short, Width=112, Font=new Font("Segoe UI",8.5f), Margin=new Padding(2,2,4,0) };
    private readonly DateTimePicker _toDate = new() { Format=DateTimePickerFormat.Short, Width=112, Font=new Font("Segoe UI",8.5f), Margin=new Padding(2,2,4,0) };
    // Six independent charts are visible together in the report workspace.
    private readonly TableLayoutPanel _analysisTables = new() { Dock=DockStyle.Fill, ColumnCount=3, RowCount=4, BackColor=Color.Transparent };
    private readonly MiniTrendChart _monthlyChart = new();
    private readonly MiniTrendChart _quarterlyChart = new();
    private readonly MiniBarChart _yearlyChart = new(Color.FromArgb(106,145,162));
    private readonly MiniBarChart _employeeChart = new(Color.FromArgb(122,143,162), horizontal: true);
    private readonly MiniBarChart _productChart = new(Green, horizontal: true);
    private readonly MiniBarChart _inventoryChart = new(Color.FromArgb(212,162,104), horizontal: true);
    private readonly Label _status = new() { AutoSize=true, Location=new Point(440,70), ForeColor=Muted, Font=new Font("Segoe UI",9f) };
    private readonly Button[] _periodButtons = new Button[5];
    private readonly Label[] _values = new Label[6], _notes = new Label[6];
    private readonly BreakdownCard _best = new("HIỆU QUẢ SẢN PHẨM","Top 10 sách bán chạy",Green);
    private readonly MiniDonutChart _orders = new();
    private readonly MiniDonutChart _categories = new();
    private readonly BreakdownCard _customers = new("KHÁCH HÀNG THÂN THIẾT","Khách mua nhiều nhất",Color.FromArgb(122,143,162));
    private Label _revChange=new(), _revNote=new(), _orderChange=new(), _orderNote=new();
    private ManagerReportDto? _report;
    private int _days=1825;
    public event Action<int>? PeriodSelected;
    public event Action<DateOnly, DateOnly>? DateRangeSelected;
    public event Action<ManagerReportDto>? ExportExcelRequested;
    public event Action<ManagerReportDto>? ExportCsvRequested;
    public event Action<ManagerReportDto>? FullDailyTrendRequested;

    public ReportDashboardForm()
    {
        TopLevel = false;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        BackColor=Canvas;
        Controls.Add(_scroll); _scroll.Controls.Add(_stack);
        BuildHeading(); BuildStats(); BuildChart(); BuildComparison(); BuildAnalysisTables(); BuildDetails();
        _footer.Controls.Add(new Label { Text="Lãi gộp là ước tính theo giá vốn bình quân từ phiếu nhập đã lưu; dữ liệu giá vốn chưa đầy đủ sẽ làm độ phủ thấp hơn 100%.", Dock=DockStyle.Fill, ForeColor=Muted, Font=new Font("Segoe UI",8.5f), TextAlign=ContentAlignment.MiddleLeft, AutoEllipsis=true });
        BuildBody();
        _stack.Controls.AddRange([_heading,_stats,_body,_detailsPanel,_footer]);
        _scroll.Resize += (_,_)=>ResizePage(); ResizePage(); Empty();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: double-buffers all child controls automatically
            return cp;
        }
    }

    public void SetLoading(int days) { _days=days; UpdatePeriods(); _status.Text=$"Đang cập nhật báo cáo {days} ngày…"; }
    public void SetLoading(DateOnly from, DateOnly to) => _status.Text=$"Đang tổng hợp {from:dd/MM/yyyy} đến {to:dd/MM/yyyy}…";
    public void ShowReport(ManagerReportDto report, ManagerReportDto historyReport)
    {
        _report=report; _days=report.Days; _fromDate.Value=report.FromDate; _toDate.Value=report.ToDate; UpdatePeriods(); _status.Text=$"Dữ liệu {report.FromDate:dd/MM/yyyy} – {report.ToDate:dd/MM/yyyy}";
        SetMetric(0,Money(report.Revenue),"Đơn hoàn tất, sau giảm giá đơn");
        SetMetric(1,report.TotalOrders.ToString("N0"),$"{report.CompletedOrders:N0} hoàn tất · {report.PendingOrders:N0} chờ xử lý");
        SetMetric(2,Money(report.AverageOrderValue),"Tính trên đơn hoàn tất");
        SetMetric(3,Money(report.Discounts),"Tổng giảm giá trên đơn hoàn tất");
        SetMetric(4,Money(report.PurchaseSpend),"Phiếu nhập trong kỳ");
        SetMetric(5,Money(report.EstimatedGrossProfit),$"Độ phủ giá vốn {report.CostCoveragePercent:N1}%");
        _chart.SetReport(report); ResizeChart();
        _best.SetRows(report.BestSellers.Select(x=>new BarItem(x.Title,$"{x.BookId} · {x.Quantity:N0} cuốn",Money(x.Revenue),x.Quantity)).ToList(),"Chưa có đơn hoàn tất trong kỳ.");
        _orders.SetItems([
            ("Hoàn tất",report.CompletedOrders),
            ("Đang xử lý",report.ProcessingOrders),
            ("Chờ xác nhận",report.PendingOrders),
            ("Đã hủy",report.CancelledOrders)
        ]);
        _categories.SetItems(report.RevenueByCategory.OrderByDescending(x=>x.Revenue).Select(x=>(x.Category,x.Revenue)));
        _customers.SetRows(report.TopCustomers.Select(x=>new BarItem(x.CustomerName,$"{x.Orders:N0} đơn · {x.Quantity:N0} cuốn",Money(x.Revenue),x.Revenue)).ToList(),"Chưa có đơn hoàn tất trong kỳ.");
        Change(_revChange,report.RevenueChangePercent); _revNote.Text=$"Kỳ trước: {Money(report.PreviousPeriodRevenue)}";
        Change(_orderChange,report.CompletedOrdersChangePercent); _orderNote.Text=$"Kỳ trước: {report.PreviousPeriodCompletedOrders:N0} đơn";
        _monthlyChart.SetItems(
            historyReport.RevenueByMonth.TakeLast(24).Select(x => ($"{x.Month:00}/{x.Year % 100:00}", x.Revenue)),
            historyReport.RevenueByMonth.TakeLast(24).Select(x => ($"{x.Month:00}/{x.Year % 100:00}", x.PurchaseSpend)));
        _quarterlyChart.SetItems(
            historyReport.RevenueByQuarter.Select(x => ($"Q{x.Quarter}/{x.Year % 100:00}", x.Revenue)),
            historyReport.RevenueByQuarter.Select(x => ($"Q{x.Quarter}/{x.Year % 100:00}", x.PurchaseSpend)));
        _yearlyChart.SetItems(historyReport.RevenueByYear.Where(x => x.Revenue > 0 || x.PurchaseSpend > 0).Select(x => (x.Year.ToString(), x.Revenue)));
        _employeeChart.SetItems(historyReport.RevenueByEmployeeYear.OrderByDescending(x => x.Revenue).Take(8).Select(x => ($"{x.EmployeeName} · {x.Year}", x.Revenue)));
        _productChart.SetItems(historyReport.ProductSalesByQuarter.OrderByDescending(x => x.Revenue).Take(8).Select(x => ($"{x.BookId} Q{x.Quarter}", x.Revenue)));
        _inventoryChart.SetItems(report.InventoryByCategory.Select(x => (x.Category, (decimal)x.StockQuantity)));
        BindDetail("books",report.BestSellers);
        BindDetail("categories",report.RevenueByCategory);
        BindDetail("customers",report.TopCustomers);
        BindDetail("inventory",report.InventoryByCategory);
        BindDetail("months",report.RevenueByMonth);
        BindDetail("quarters",report.RevenueByQuarter);
        BindDetail("years",report.RevenueByYear);
        BindDetail("employees",report.RevenueByEmployeeYear);
        BindDetail("products",report.ProductSalesByQuarter);
    }

    private void BuildHeading()
    {
        _heading.Controls.Add(new Label { Text="PHÂN TÍCH KINH DOANH",AutoSize=true,ForeColor=Green,Font=new Font("Segoe UI",8.5f,FontStyle.Bold),Location=new Point(0,0) });
        _heading.Controls.Add(new Label { Text="Báo cáo cửa hàng",AutoSize=true,ForeColor=Ink,Font=new Font("Segoe UI",19f,FontStyle.Bold),Location=new Point(0,17) });
        var periods=new[]{(7,"7 ngày"),(30,"30 ngày"),(90,"90 ngày"),(365,"1 năm"),(1825,"5 năm")};
        for(int i=0;i<periods.Length;i++){var days=periods[i].Item1;var b=new Button{Text=periods[i].Item2,Tag=days,AutoSize=true,Height=27,FlatStyle=FlatStyle.Flat,Margin=new Padding(2,0,2,0),Padding=new Padding(9,1,9,1),Font=new Font("Segoe UI",8.5f,FontStyle.Bold),Cursor=Cursors.Hand};b.FlatAppearance.BorderSize=0;b.Click+=(_,_)=>PeriodSelected?.Invoke(days);_periodButtons[i]=b;_periods.Controls.Add(b);}
        _heading.Controls.Add(_periods);_heading.Controls.Add(_status);
        _fromDate.Value=DateTime.Today.AddDays(-1824); _toDate.Value=DateTime.Today;
        _rangeTools.Controls.Add(new Label{Text="Từ ngày",AutoSize=true,Margin=new Padding(2,8,2,0),ForeColor=Muted,Font=new Font("Segoe UI",8.5f,FontStyle.Bold)});
        _rangeTools.Controls.Add(_fromDate);
        _rangeTools.Controls.Add(new Label{Text="Đến ngày",AutoSize=true,Margin=new Padding(5,8,2,0),ForeColor=Muted,Font=new Font("Segoe UI",8.5f,FontStyle.Bold)});
        _rangeTools.Controls.Add(_toDate);
        var apply=CreateActionButton("Thống kê kỳ này");
        apply.Click+=(_,_)=>DateRangeSelected?.Invoke(DateOnly.FromDateTime(_fromDate.Value),DateOnly.FromDateTime(_toDate.Value));
        var excel=CreateActionButton("Xuất Excel"); excel.Click+=(_,_)=>{if(_report is not null)ExportExcelRequested?.Invoke(_report);};
        var csv=CreateActionButton("Xuất CSV"); csv.Click+=(_,_)=>{if(_report is not null)ExportCsvRequested?.Invoke(_report);};
        _rangeTools.Controls.AddRange([apply,excel,csv]); _heading.Controls.Add(_rangeTools);
    }
    private void BuildAnalysisTables()
    {
        for (var column = 0; column < 3; column++) _analysisTables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        for (var row = 0; row < 4; row++) _analysisTables.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        _analysisTables.Height = 4 * 218;
        AddChartControl(_chartCard, 0, 0);
        AddAnalysisCard("DOANH THU THEO THÁNG", _monthlyChart, 1, 0);
        AddAnalysisCard("DOANH THU THEO QUÝ", _quarterlyChart, 2, 0);
        AddAnalysisCard("DOANH THU THEO NĂM", _yearlyChart, 0, 1);
        AddAnalysisCard("DOANH THU THEO NHÂN VIÊN / NĂM", _employeeChart, 1, 1);
        AddAnalysisCard("SẢN PHẨM THEO QUÝ / NĂM", _productChart, 2, 1);
        AddAnalysisCard("TỒN KHO THEO THỂ LOẠI", _inventoryChart, 0, 2);
        AddChartControl(_best, 1, 2);
        AddDonutCard("TÌNH TRẠNG HÓA ĐƠN", _orders, 2, 2);
        AddDonutCard("TỶ TRỌNG DOANH THU THEO THỂ LOẠI", _categories, 0, 3);
        AddChartControl(_customers, 1, 3);
    }
    private void AddAnalysisCard(string title, Control chart, int column, int row)
    {
        var card = new Panel { Dock = DockStyle.Fill, Margin = new Padding(2), Padding = new Padding(6,2,6,2), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        var heading = new Label { Text = title, Dock = DockStyle.Top, Height = 25, Padding = new Padding(3,4,0,0), ForeColor = Green, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), AutoEllipsis = true };
        chart.Dock = DockStyle.Fill; chart.Margin = new Padding(2);
        card.Controls.Add(chart);
        card.Controls.Add(heading);
        _analysisTables.Controls.Add(card, column, row);
    }
    private void AddChartControl(Control control, int column, int row)
    {
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(2);
        _analysisTables.Controls.Add(control, column, row);
    }
    private void AddDonutCard(string title, MiniDonutChart chart, int column, int row)
    {
        var card=new Panel{Dock=DockStyle.Fill,Margin=new Padding(2),Padding=new Padding(7,4,7,5),BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle};
        card.Controls.Add(chart); chart.Dock=DockStyle.Fill;
        card.Controls.Add(new Label{Text=title,Dock=DockStyle.Top,Height=25,Padding=new Padding(3,4,0,0),ForeColor=Green,Font=new Font("Segoe UI",7.5f,FontStyle.Bold),AutoEllipsis=true});
        _analysisTables.Controls.Add(card,column,row);
    }
    private void BuildBody()
    {
        _body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _body.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        _body.RowStyles.Add(new RowStyle(SizeType.Absolute, _analysisTables.Height));
        _comparison.Dock = DockStyle.Fill;
        _analysisTables.Dock = DockStyle.Fill;
        _body.Controls.Add(_comparison, 0, 0);
        _body.Controls.Add(_analysisTables, 0, 1);
    }
    private void BuildDetails()
    {
        _detailsPanel.Controls.Add(_detailTabs);
        _detailsPanel.Controls.Add(new Label{Text="SỐ LIỆU CHI TIẾT",Dock=DockStyle.Top,Height=28,Padding=new Padding(4,2,0,0),ForeColor=Green,Font=new Font("Segoe UI",8.5f,FontStyle.Bold)});
        AddDetailTab("books","Sách bán chạy");
        AddDetailTab("categories","Doanh thu thể loại");
        AddDetailTab("customers","Khách hàng");
        AddDetailTab("inventory","Tồn kho");
        AddDetailTab("months","Theo tháng");
        AddDetailTab("quarters","Theo quý");
        AddDetailTab("years","Theo năm");
        AddDetailTab("employees","Nhân viên / năm");
        AddDetailTab("products","Sản phẩm / quý");
    }
    private void AddDetailTab(string key,string title)
    {
        var grid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AllowUserToResizeRows=false,RowHeadersVisible=false,AutoGenerateColumns=true,SelectionMode=DataGridViewSelectionMode.FullRowSelect,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,GridColor=Line,EnableHeadersVisualStyles=false,ColumnHeadersHeight=30,RowTemplate={Height=28}};
        grid.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(240,246,247);grid.ColumnHeadersDefaultCellStyle.ForeColor=Color.FromArgb(84,105,117);grid.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",7.5f,FontStyle.Bold);
        grid.DefaultCellStyle.Font=new Font("Segoe UI",8f);grid.DefaultCellStyle.ForeColor=Ink;grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(220,239,235);grid.DefaultCellStyle.SelectionForeColor=Ink;grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(249,251,251);
        _detailGrids[key]=grid;var tab=new TabPage(title){BackColor=Color.White};tab.Controls.Add(grid);_detailTabs.TabPages.Add(tab);
    }
    private void BindDetail<T>(string key,IEnumerable<T> rows)
    {
        var grid=_detailGrids[key];grid.DataSource=rows.ToList();
        var headers=new Dictionary<string,string>(StringComparer.Ordinal)
        {
            ["BookId"]="Mã sách",["Title"]="Tên sách",["Quantity"]="Số lượng",["Revenue"]="Doanh thu",
            ["Category"]="Thể loại",["CustomerId"]="Mã khách",["CustomerName"]="Khách hàng",["Orders"]="Số đơn",
            ["BookCount"]="Số sách",["StockQuantity"]="Tồn kho",["LowStockBooks"]="Sắp hết",["StockValue"]="Giá trị tồn",
            ["Year"]="Năm",["Month"]="Tháng",["Quarter"]="Quý",["PurchaseSpend"]="Chi nhập",
            ["EstimatedGrossProfit"]="Lãi gộp ước tính",["EmployeeId"]="Mã nhân viên",["EmployeeName"]="Nhân viên"
        };
        foreach(DataGridViewColumn column in grid.Columns)
        {
            if(headers.TryGetValue(column.DataPropertyName,out var caption))column.HeaderText=caption;
            if(column.ValueType==typeof(decimal)||column.ValueType==typeof(int))column.DefaultCellStyle.Format="N0";
            column.SortMode=DataGridViewColumnSortMode.Automatic;
        }
    }
    private void BuildStats()
    {
        for(int i=0;i<3;i++)_stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
        for(int i=0;i<2;i++)_stats.RowStyles.Add(new RowStyle(SizeType.Percent,50));
        var captions=new[]{"DOANH THU THUẦN","HÓA ĐƠN","GIÁ TRỊ ĐƠN TRUNG BÌNH","GIẢM GIÁ","CHI NHẬP HÀNG","LÃI GỘP ƯỚC TÍNH"};
        var notes=new[]{"Đơn hoàn tất, sau giảm giá đơn","Đơn hoàn tất và đang chờ","Tính trên đơn hoàn tất","Tổng giảm giá trên đơn hoàn tất","Phiếu nhập trong kỳ","Theo giá vốn bình quân"};
        for(int i=0;i<6;i++){var p=Card();p.Margin=new Padding(3);p.Padding=new Padding(8,7,8,4);
            var note=new Label{Text=notes[i],Dock=DockStyle.Bottom,Height=22,ForeColor=Muted,Font=new Font("Segoe UI",7f),AutoEllipsis=true};
            var value=new Label{Text="—",Dock=DockStyle.Top,Height=27,ForeColor=Ink,Font=new Font("Segoe UI",13f,FontStyle.Bold),AutoEllipsis=true};
            var cap=new Label{Text=captions[i],Dock=DockStyle.Top,Height=17,ForeColor=Muted,Font=new Font("Segoe UI",7f,FontStyle.Bold),AutoEllipsis=true};
            p.Controls.Add(note);p.Controls.Add(value);p.Controls.Add(cap);_values[i]=value;_notes[i]=note;_stats.Controls.Add(p,i%3,i/3);}
    }
    private void BuildChart()
    {
        _chart.Dock=DockStyle.Top;_chartHost.Controls.Add(_chart);
        var legend=new FlowLayoutPanel{AutoSize=true,WrapContents=false,FlowDirection=FlowDirection.LeftToRight,Anchor=AnchorStyles.Top|AnchorStyles.Right,Location=new Point(500,29),BackColor=Color.White};
        legend.Controls.Add(Legend("Doanh thu",Green));legend.Controls.Add(Legend("Nhập hàng",Gold));
        var fullView=CreateActionButton("Xem toàn bộ");
        fullView.Location=new Point(14,54);fullView.Click+=(_,_)=>{if(_report is not null)FullDailyTrendRequested?.Invoke(_report);};
        _chartCard.Controls.Add(_chartHost);_chartCard.Controls.Add(Kicker("DÒNG TIỀN BÁN HÀNG",new Point(14,10)));
        _chartCard.Controls.Add(Title("14 ngày gần nhất",new Point(14,27)));
        _chartCard.Controls.Add(legend);_chartCard.Controls.Add(fullView);
        _chartCard.Controls.Add(new Label{Text="Biểu đồ cả kỳ",AutoSize=true,ForeColor=Muted,Font=new Font("Segoe UI",7.5f),Location=new Point(14,58)});
        _chartCard.Resize+=(_,_)=>{legend.Left=Math.Max(170,_chartCard.ClientSize.Width-legend.Width-16);fullView.Left=Math.Max(110,_chartCard.ClientSize.Width-fullView.Width-12);_chartHost.Width=Math.Max(180,_chartCard.ClientSize.Width-24);_chartHost.Height=Math.Max(72,_chartCard.ClientSize.Height-88);ResizeChart();};
    }
    private void BuildComparison()
    {
        _comparison.Controls.Add(Kicker("SO VỚI KỲ LIỀN TRƯỚC",new Point(10,7)));
        var grid=new TableLayoutPanel{Left=8,Top=25,Height=52,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,ColumnCount=2,RowCount=1,BackColor=Color.White};
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        grid.Controls.Add(ChangeMetric("Doanh thu thuần",out _revChange,out _revNote),0,0);grid.Controls.Add(ChangeMetric("Đơn hoàn tất",out _orderChange,out _orderNote),1,0);
        _comparison.Controls.Add(grid);_comparison.Resize+=(_,_)=>{grid.Width=Math.Max(160,_comparison.ClientSize.Width-16);grid.Height=Math.Max(42,_comparison.ClientSize.Height-30);};
    }
    private void ResizePage()
    {
        int full=Math.Max(420,_scroll.ClientSize.Width-2);_stack.Width=full;int inner=Math.Max(360,full-_stack.Padding.Horizontal);
        foreach(Control c in _stack.Controls)c.Width=inner;
        _heading.Height = inner < 700 ? 150 : 140;
        _stats.Height = inner < 700 ? 200 : 184;
        _body.Height = 86 + _analysisTables.Height;
        _analysisTables.Width = inner;
        _periods.Width=Math.Min(inner,420);_status.Left=_periods.Right+12;_status.MaximumSize=new Size(Math.Max(120,inner-_status.Left),0);
        _rangeTools.Width=inner;
        for (var i=0;i<_analysisTables.ColumnStyles.Count;i++) _analysisTables.ColumnStyles[i].Width = 100f / _analysisTables.ColumnStyles.Count;
        _stack.Height=_stack.Controls.Cast<Control>().Sum(c=>c.Height+c.Margin.Vertical)+_stack.Padding.Vertical+4;
        _scroll.AutoScrollMinSize=new Size(0,_stack.Height);ResizeChart();
    }
    private void ResizeChart(){if(_chartHost.ClientSize.Width>0){_chart.Width=Math.Max(120,_chartHost.ClientSize.Width-4);_chart.Height=Math.Max(70,_chartHost.ClientSize.Height-4);}}
    private void SetMetric(int i,string value,string note){_values[i].Text=value;_notes[i].Text=note;}
    private void UpdatePeriods(){foreach(var b in _periodButtons){bool selected=b.Tag is int d&&d==_days;b.BackColor=selected?Color.White:Color.FromArgb(233,237,231);b.ForeColor=selected?Ink:Color.FromArgb(102,113,100);}}
    private void Empty(){foreach(var v in _values)v.Text="—";_status.Text="Chọn kỳ báo cáo để tải số liệu.";_best.SetRows([],"Chưa tải dữ liệu.");_orders.SetItems([]);_categories.SetItems([]);_customers.SetRows([],"Chưa tải dữ liệu.");}
    private static void Change(Label label,decimal? value){label.Text=value is null?"Chưa có dữ liệu kỳ trước":$"{(value>=0?"+":"")}{value.Value:N1}%";label.ForeColor=value is null?Muted:value>=0?Green:Color.FromArgb(180,92,76);}
    private static Panel ChangeMetric(string caption,out Label value,out Label note){var p=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(248,248,244),Margin=new Padding(2),Padding=new Padding(4)};p.Controls.Add(new Label{Text=caption,AutoSize=true,Location=new Point(4,0),ForeColor=Muted,Font=new Font("Segoe UI",7f)});value=new Label{Text="—",AutoSize=true,Location=new Point(4,14),ForeColor=Ink,Font=new Font("Segoe UI",9f,FontStyle.Bold)};note=new Label{Text="",AutoSize=true,Location=new Point(4,31),ForeColor=Muted,Font=new Font("Segoe UI",6.5f)};p.Controls.Add(value);p.Controls.Add(note);return p;}
    private static Panel Card()=>new(){BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle};
    private static Label Kicker(string text,Point p)=>new(){Text=text,AutoSize=true,ForeColor=Green,Font=new Font("Segoe UI",8f,FontStyle.Bold),Location=p};
    private static Label Title(string text,Point p)=>new(){Text=text,AutoSize=true,ForeColor=Ink,Font=new Font("Segoe UI",11.5f,FontStyle.Bold),Location=p};
    private static Control Legend(string text,Color color){var p=new FlowLayoutPanel{AutoSize=true,WrapContents=false,FlowDirection=FlowDirection.LeftToRight,Margin=new Padding(6,2,4,0),BackColor=Color.White};p.Controls.Add(new Panel{Width=10,Height=10,BackColor=color,Margin=new Padding(0,4,5,0)});p.Controls.Add(new Label{Text=text,AutoSize=true,ForeColor=Muted,Font=new Font("Segoe UI",8f)});return p;}
    private static string Money(decimal n) => n.ToString("N0") + " ₫";
    private static Button CreateActionButton(string caption)
    {
        var button=new Button{Text=caption,AutoSize=true,Height=29,FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=Ink,Font=new Font("Segoe UI",8.5f,FontStyle.Bold),Margin=new Padding(4,1,3,0),Padding=new Padding(7,1,7,1),Cursor=Cursors.Hand};
        button.FlatAppearance.BorderColor=Line; return button;
    }
    private sealed record BarItem(string Name,string Detail,string Amount,decimal Value);

    private sealed class MiniDonutChart : Panel
    {
        private static readonly Color[] Palette=[Green,Gold,Color.FromArgb(81,125,153),Color.FromArgb(189,109,90),Color.FromArgb(141,121,168),Color.FromArgb(114,166,160),Color.FromArgb(166,173,92),Color.FromArgb(119,130,139)];
        private (string Label,decimal Value)[] _items=[];
        public MiniDonutChart(){BackColor=Color.White;SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.UserPaint,true);}
        public void SetItems(IEnumerable<(string Label,decimal Value)> items){_items=items.Where(x=>x.Value>0).Take(8).ToArray();Invalidate();}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            var total=_items.Sum(x=>x.Value);var size=Math.Clamp(ClientSize.Height-22f,78f,135f);size=Math.Min(size,Math.Max(72f,ClientSize.Width*.43f));
            var bounds=new RectangleF(8,(ClientSize.Height-size)/2f,size,size);var start=-90f;
            if(total<=0){using var empty=new Pen(Color.FromArgb(232,237,239),15);e.Graphics.DrawEllipse(empty,bounds.X+9,bounds.Y+9,bounds.Width-18,bounds.Height-18);}
            else for(var i=0;i<_items.Length;i++)
            {
                var sweep=(float)(_items[i].Value*360m/total);using var brush=new SolidBrush(Palette[i%Palette.Length]);e.Graphics.FillPie(brush,bounds,start,sweep);start+=sweep;
            }
            var hole=new RectangleF(bounds.X+size*.25f,bounds.Y+size*.25f,size*.5f,size*.5f);using(var center=new SolidBrush(Color.White))e.Graphics.FillEllipse(center,hole);
            using var totalFont=new Font("Segoe UI",8f,FontStyle.Bold);using var captionFont=new Font("Segoe UI",6.5f);using var textBrush=new SolidBrush(Ink);using var mutedBrush=new SolidBrush(Muted);
            e.Graphics.DrawString(total.ToString("N0"),totalFont,textBrush,new RectangleF(hole.X,hole.Y+size*.11f,hole.Width,hole.Height*.36f),new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter});
            e.Graphics.DrawString("Tổng",captionFont,mutedBrush,new RectangleF(hole.X,hole.Y+size*.48f,hole.Width,hole.Height*.28f),new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center});
            var legendX=bounds.Right+13;var legendWidth=Math.Max(45,ClientSize.Width-legendX-4);var rowHeight=Math.Min(23,Math.Max(17,(ClientSize.Height-10f)/Math.Max(1,_items.Length)));
            for(var i=0;i<_items.Length;i++)
            {
                var y=6+i*rowHeight;using var swatch=new SolidBrush(Palette[i%Palette.Length]);e.Graphics.FillRectangle(swatch,legendX,y+4,8,8);
                var value=_items[i].Value>=1_000_000?$"{_items[i].Value/1_000_000:0.#}tr":_items[i].Value>=1_000?$"{_items[i].Value/1_000:0.#}k":_items[i].Value.ToString("0");
                e.Graphics.DrawString(_items[i].Label,captionFont,mutedBrush,new RectangleF(legendX+13,y,Math.Max(30,legendWidth-42),rowHeight),new StringFormat{Trimming=StringTrimming.EllipsisCharacter,LineAlignment=StringAlignment.Center});
                e.Graphics.DrawString(value,captionFont,textBrush,new RectangleF(ClientSize.Width-31,y,29,rowHeight),new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center});
            }
        }
    }

    private sealed class MiniTrendChart : Panel
    {
        private (string Label, decimal Value)[] _revenue = [];
        private (string Label, decimal Value)[] _purchases = [];
        public MiniTrendChart() { BackColor=Color.White; SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.UserPaint,true); }
        public void SetItems(IEnumerable<(string Label, decimal Value)> revenue, IEnumerable<(string Label, decimal Value)> purchases)
        {
            _revenue=revenue.ToArray(); _purchases=purchases.ToArray(); Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_revenue.Length==0) { TextRenderer.DrawText(e.Graphics,"Chưa có dữ liệu",Font,ClientRectangle,Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter); return; }
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            float left=28,right=Math.Max(left+40,ClientSize.Width-8),top=20,bottom=ClientSize.Height-20,height=Math.Max(20,bottom-top);
            decimal max=Math.Max(1m,_revenue.Select(x=>x.Value).Concat(_purchases.Select(x=>x.Value)).DefaultIfEmpty(0).Max());
            using var grid=new Pen(Color.FromArgb(233,238,239)); using var revenuePen=new Pen(Green,2.5f); using var purchasePen=new Pen(Gold,2f);
            using var revenueBrush=new SolidBrush(Green); using var purchaseBrush=new SolidBrush(Gold); using var mutedBrush=new SolidBrush(Muted);
            using var smallFont=new Font("Segoe UI",6.5f); using var labelFont=new Font("Segoe UI",6f);
            using var center=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Near,Trimming=StringTrimming.EllipsisCharacter};
            for(int i=0;i<3;i++){float y=top+i*height/2f;e.Graphics.DrawLine(grid,left,y,right,y);}
            e.Graphics.FillEllipse(revenueBrush,left,3,7,7);e.Graphics.DrawString("Doanh thu",smallFont,mutedBrush,left+10,0);
            e.Graphics.FillEllipse(purchaseBrush,left+82,3,7,7);e.Graphics.DrawString("Nhập hàng",smallFont,mutedBrush,left+92,0);
            Draw(_revenue,revenuePen,revenueBrush); Draw(_purchases,purchasePen,purchaseBrush);
            void Draw((string Label,decimal Value)[] values,Pen pen,Brush dot)
            {
                var points=values.Select((item,index)=>new PointF(values.Length==1?(left+right)/2:left+index*(right-left)/(values.Length-1),bottom-(float)(item.Value/max)*height)).ToArray();
                if(points.Length>1)e.Graphics.DrawLines(pen,points);
                var step=Math.Max(1,(int)Math.Ceiling(values.Length/7d));
                for(int i=0;i<points.Length;i++)
                {
                    e.Graphics.FillEllipse(dot,points[i].X-2.5f,points[i].Y-2.5f,5,5);
                    if(i%step==0||i==points.Length-1)e.Graphics.DrawString(values[i].Label,labelFont,mutedBrush,new RectangleF(points[i].X-22,bottom+3,44,15),center);
                }
            }
        }
    }

    private sealed class MiniBarChart : Panel
    {
        private (string Label, decimal Value)[] _items = [];
        private readonly Color _barColor;
        private readonly bool _horizontal;
        public MiniBarChart(Color color, bool horizontal = false)
        {
            _barColor = color;
            _horizontal = horizontal;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.White;
        }
        public void SetItems(IEnumerable<(string Label, decimal Value)> items)
        {
            _items = items.TakeLast(24).ToArray();
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_items.Length == 0)
            {
                TextRenderer.DrawText(e.Graphics, "Chưa có dữ liệu", Font, ClientRectangle, Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                return;
            }
            if (_horizontal)
            {
                PaintHorizontal(e.Graphics);
                return;
            }
            var max = Math.Max(1m, _items.Max(item => item.Value));
            var bottom = Math.Max(18, ClientSize.Height - 16);
            var chartHeight = Math.Max(12, bottom - 3);
            var slot = ClientSize.Width / (float)_items.Length;
            var barWidth = Math.Clamp(slot * .56f, 3f, 16f);
            using var brush = new SolidBrush(_barColor);
            using var labelBrush = new SolidBrush(Muted);
            using var font = new Font("Segoe UI", 6.5f);
            using var valueFont = new Font("Segoe UI", 6f);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter };
            var labelStep = Math.Max(1, (int)Math.Ceiling(_items.Length / 8d));
            for (var i = 0; i < _items.Length; i++)
            {
                var item = _items[i];
                var height = Math.Max(2f, (float)(item.Value / max) * chartHeight);
                var x = i * slot + (slot - barWidth) / 2f;
                e.Graphics.FillRectangle(brush, x, bottom - height, barWidth, height);
                if (i % labelStep == 0 || i == _items.Length - 1)
                {
                    var label = item.Label.Length > 9 ? item.Label[..9] : item.Label;
                    e.Graphics.DrawString(label, font, labelBrush, new RectangleF(i * slot, bottom + 2, slot * labelStep, 13), format);
                }
                if (_items.Length <= 8)
                {
                    var value = item.Value >= 1_000_000 ? $"{item.Value / 1_000_000:0.#}tr" : item.Value >= 1_000 ? $"{item.Value / 1_000:0.#}k" : item.Value.ToString("0");
                    e.Graphics.DrawString(value, valueFont, labelBrush, new RectangleF(i * slot, Math.Max(0, bottom - height - 10), slot, 9), format);
                }
            }
        }
        private void PaintHorizontal(Graphics graphics)
        {
            var max = Math.Max(1m, _items.Max(item => item.Value));
            var rowHeight = ClientSize.Height / (float)_items.Length;
            var labelWidth = Math.Max(70, ClientSize.Width * .57f);
            using var bar = new SolidBrush(_barColor);
            using var track = new SolidBrush(Color.FromArgb(237, 241, 242));
            using var text = new SolidBrush(Muted);
            using var labelFont = new Font("Segoe UI", 6.5f);
            using var valueFont = new Font("Segoe UI", 6.5f, FontStyle.Bold);
            using var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, LineAlignment = StringAlignment.Center };
            for (var i = 0; i < _items.Length; i++)
            {
                var item = _items[i]; var y = i * rowHeight;
                graphics.DrawString(item.Label, labelFont, text, new RectangleF(2, y, labelWidth, Math.Max(12, rowHeight - 5)), format);
                var value = item.Value >= 1_000_000 ? $"{item.Value / 1_000_000:0.#}tr" : item.Value >= 1_000 ? $"{item.Value / 1_000:0.#}k" : item.Value.ToString("0");
                graphics.DrawString(value, valueFont, text, new RectangleF(labelWidth + 2, y, ClientSize.Width - labelWidth - 5, Math.Max(12, rowHeight - 5)), format);
                var trackY = y + rowHeight - 5; var trackWidth = Math.Max(20, ClientSize.Width - 8);
                graphics.FillRectangle(track, 2, trackY, trackWidth, 3);
                graphics.FillRectangle(bar, 2, trackY, Math.Max(2, trackWidth * (float)(item.Value / max)), 3);
            }
        }
    }

    private sealed class BreakdownCard:Panel
    {
        private readonly FlowLayoutPanel _rows=new(){Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(8,46,8,3),BackColor=Color.White};
        private readonly Color _color;
        public BreakdownCard(string kicker,string title,Color color)
        {
            _color=color;BackColor=Color.White;BorderStyle=BorderStyle.FixedSingle;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Controls.Add(_rows);Controls.Add(Kicker(kicker,new Point(13,9)));Controls.Add(Title(title,new Point(13,25)));
            Resize+=(_,_)=>{foreach(Control r in _rows.Controls)r.Width=Math.Max(110,_rows.ClientSize.Width-8);};
        }
        public void SetRows(List<BarItem> rows,string empty){_rows.SuspendLayout();_rows.Controls.Clear();decimal max=rows.Select(x=>x.Value).DefaultIfEmpty(0).Max();if(rows.Count==0)_rows.Controls.Add(new Label{Text=empty,AutoSize=true,ForeColor=Muted,Font=new Font("Segoe UI",8.5f),Margin=new Padding(0,18,0,0)});foreach(var item in rows)_rows.Controls.Add(MakeRow(item,max));_rows.ResumeLayout();}
        private Control MakeRow(BarItem item,decimal max){var row=new Panel{Height=43,Width=Math.Max(110,_rows.ClientSize.Width-8),Margin=new Padding(0,0,0,2),BackColor=Color.White};
            var name=new Label{Text=item.Name,Left=0,Top=0,Width=row.Width-130,Height=17,AutoEllipsis=true,ForeColor=Color.FromArgb(70,80,68),Font=new Font("Segoe UI",8f,FontStyle.Bold)};
            var amount=new Label{Text=item.Amount,Left=row.Width-126,Top=0,Width=124,Height=17,AutoEllipsis=true,TextAlign=ContentAlignment.TopRight,ForeColor=Ink,Font=new Font("Segoe UI",8f,FontStyle.Bold)};
            var detail=new Label{Text=item.Detail,Left=0,Top=16,Width=row.Width,Height=12,AutoEllipsis=true,ForeColor=Muted,Font=new Font("Segoe UI",7f)};
            var track=new Panel{Left=0,Top=31,Width=row.Width,Height=5,BackColor=Color.FromArgb(238,240,235)};var fill=new Panel{Dock=DockStyle.Left,BackColor=_color};track.Controls.Add(fill);
            row.Controls.Add(name);row.Controls.Add(amount);row.Controls.Add(detail);row.Controls.Add(track);
            row.Resize+=(_,_)=>{int nw=Math.Max(65,row.ClientSize.Width-132);name.Width=nw;amount.Left=nw+5;amount.Width=Math.Max(35,row.ClientSize.Width-amount.Left);detail.Width=row.ClientSize.Width;track.Width=row.ClientSize.Width;fill.Width=max<=0?0:Math.Max(2,(int)(row.ClientSize.Width*Math.Clamp(item.Value/max,0m,1m)));};return row;}
    }

    private sealed class RevenueChart:Panel
    {
        private ManagerReportDto? _report;
        public void SetReport(ManagerReportDto? report){_report=report;Invalidate();}
        public RevenueChart(){BackColor=Color.White;SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.UserPaint,true);}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var days=_report?.RevenueByDayDto.TakeLast(14).ToArray();if(days is null||days.Length==0){TextRenderer.DrawText(e.Graphics,"Chưa có dữ liệu trong kỳ.",Font,ClientRectangle,Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float left=72,right=Math.Max(132,ClientSize.Width-16),top=12,bottom=ClientSize.Height-38,height=Math.Max(40,bottom-top);decimal max=Ceiling(days.Select(x=>Math.Max(x.Revenue,x.PurchaseSpend)).DefaultIfEmpty(0m).Max());
            using var grid=new Pen(Line,1);using var axis=new SolidBrush(Muted);using var green=new SolidBrush(Green);using var gold=new SolidBrush(Gold);using var font=new Font("Segoe UI",8f);using var rightFmt=new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center};using var centerFmt=new StringFormat{Alignment=StringAlignment.Center};
            for(int t=0;t<=4;t++){float y=top+height*t/4f;e.Graphics.DrawLine(grid,left,y,right,y);e.Graphics.DrawString(Compact(max*(4-t)/4m),font,axis,new RectangleF(0,y-8,left-8,16),rightFmt);}
            float slot=(right-left)/days.Length,bar=Math.Clamp(slot*.28f,3f,12f);int step=Math.Max(1,(days.Length+7)/8);
            for(int i=0;i<days.Length;i++){float x=left+(i+.5f)*slot;Draw(days[i].Revenue,x-bar-1,green);Draw(days[i].PurchaseSpend,x+1,gold);if(i%step==0||i==days.Length-1)e.Graphics.DrawString(days[i].Date.ToString("dd/MM"),font,axis,new RectangleF(x-slot*1.5f,bottom+6,slot*3,18),centerFmt);}
            void Draw(decimal value,float x,Brush brush){if(value<=0)return;float h=Math.Max(1,(float)(value/max)*height);e.Graphics.FillRectangle(brush,x,bottom-h,Math.Max(1,bar),h);}
        }
        private static decimal Ceiling(decimal v){if(v<=0)return 1;decimal m=1;while(v>=m*10&&m<=decimal.MaxValue/10)m*=10;while(v<m)m/=10;decimal n=v/m;decimal s=n<=1?1:n<=2?2:n<=2.5m?2.5m:n<=5?5:10;return s*m;}
        private static string Compact(decimal v)=>v>=1_000_000?$"{v/1_000_000:0.#}tr":v>=1_000?$"{v/1_000:0.#}k":v.ToString("0");
    }
}



