using BookStore.Shared.Models;
using System.Drawing.Drawing2D;

namespace TrangSach.WinForms;

public sealed class ReportDashboardView : UserControl
{
    private static readonly Color Ink = Color.FromArgb(52,79,61), Green = Color.FromArgb(69,107,80),
        Gold = Color.FromArgb(212,162,104), Muted = Color.FromArgb(129,135,125),
        Line = Color.FromArgb(231,233,226), Canvas = Color.FromArgb(247,248,244);
    private readonly Panel _scroll = new() { Dock=DockStyle.Fill, AutoScroll=true, BackColor=Canvas };
    private readonly FlowLayoutPanel _stack = new() { Dock=DockStyle.Top, FlowDirection=FlowDirection.TopDown, WrapContents=false, Padding=new Padding(16), BackColor=Canvas };
    private readonly Panel _heading = new() { Height=100, Margin=new Padding(0,0,0,10), BackColor=Canvas };
    private readonly TableLayoutPanel _stats = new() { Height=205, Margin=new Padding(0,0,0,10), ColumnCount=3, RowCount=2, BackColor=Canvas };
    private readonly Panel _chartCard = new() { Height=360, Margin=new Padding(0,0,0,10), BackColor=Color.White, BorderStyle=BorderStyle.FixedSingle };
    private readonly Panel _chartHost = new() { Left=12, Top=88, Height=258, Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right, AutoScroll=true, BackColor=Color.White };
    private readonly RevenueChart _chart = new() { Height=244 };
    private readonly TableLayoutPanel _lists = new() { Height=560, Margin=new Padding(0,0,0,10), ColumnCount=2, RowCount=2, BackColor=Canvas };
    private readonly Panel _comparison = new() { Height=152, Margin=new Padding(0,0,0,8), BackColor=Color.White, BorderStyle=BorderStyle.FixedSingle };
    private readonly Panel _footer = new() { Height=46, Margin=new Padding(0,0,0,10), BackColor=Canvas };
    private readonly FlowLayoutPanel _periods = new() { Location=new Point(0,64), Height=31, AutoSize=false, WrapContents=false, BackColor=Color.FromArgb(233,237,231), Padding=new Padding(3) };
    private readonly Label _status = new() { AutoSize=true, Location=new Point(440,70), ForeColor=Muted, Font=new Font("Segoe UI",9f) };
    private readonly Button[] _periodButtons = new Button[4];
    private readonly Label[] _values = new Label[6], _notes = new Label[6];
    private readonly BreakdownCard _best = new("HIỆU QUẢ SẢN PHẨM","Top 10 sách bán chạy",Green);
    private readonly BreakdownCard _orders = new("TÌNH HÌNH ĐƠN HÀNG","Trạng thái đơn hàng",Color.FromArgb(106,145,162));
    private readonly BreakdownCard _categories = new("CƠ CẤU DOANH THU","Doanh thu theo thể loại",Green);
    private readonly BreakdownCard _customers = new("KHÁCH HÀNG THÂN THIẾT","Khách mua nhiều nhất",Color.FromArgb(122,143,162));
    private Label _revChange=new(), _revNote=new(), _orderChange=new(), _orderNote=new();
    private ManagerReport? _report;
    private int _days=30;
    public event Action<int>? PeriodSelected;

    public ReportDashboardView()
    {
        BackColor=Canvas; Controls.Add(_scroll); _scroll.Controls.Add(_stack);
        BuildHeading(); BuildStats(); BuildChart(); BuildLists(); BuildComparison();
        _footer.Controls.Add(new Label { Text="Lãi gộp là ước tính theo giá vốn bình quân từ phiếu nhập đã lưu; dữ liệu giá vốn chưa đầy đủ sẽ làm độ phủ thấp hơn 100%.", Dock=DockStyle.Fill, ForeColor=Muted, Font=new Font("Segoe UI",8.5f), TextAlign=ContentAlignment.MiddleLeft, AutoEllipsis=true });
        _stack.Controls.AddRange([_heading,_stats,_chartCard,_lists,_comparison,_footer]);
        _scroll.Resize += (_,_)=>ResizePage(); ResizePage(); Empty();
    }

    public void SetLoading(int days) { _days=days; UpdatePeriods(); _status.Text=$"Đang cập nhật báo cáo {days} ngày…"; }
    public void ShowReport(ManagerReport report)
    {
        _report=report; _days=report.Days; UpdatePeriods(); _status.Text=$"Dữ liệu trong {report.Days} ngày gần nhất";
        SetMetric(0,Money(report.Revenue),"Đơn hoàn tất, sau giảm giá đơn");
        SetMetric(1,report.TotalOrders.ToString("N0"),$"{report.CompletedOrders:N0} hoàn tất · {report.PendingOrders:N0} chờ xử lý");
        SetMetric(2,Money(report.AverageOrderValue),"Tính trên đơn hoàn tất");
        SetMetric(3,Money(report.Discounts),"Tổng giảm giá trên đơn hoàn tất");
        SetMetric(4,Money(report.PurchaseSpend),"Phiếu nhập trong kỳ");
        SetMetric(5,Money(report.EstimatedGrossProfit),$"Độ phủ giá vốn {report.CostCoveragePercent:N1}%");
        _chart.SetReport(report); ResizeChart();
        _best.SetRows(report.BestSellers.Select(x=>new BarItem(x.Title,$"{x.BookId} · {x.Quantity:N0} cuốn",Money(x.Revenue),x.Quantity)).ToArray(),"Chưa có đơn hoàn tất trong kỳ.");
        _orders.SetRows([
            new("Hoàn tất",$"{report.CompletedOrders:N0} đơn",report.CompletedOrders.ToString("N0"),report.CompletedOrders),
            new("Đang xử lý / giao",$"{report.ProcessingOrders:N0} đơn",report.ProcessingOrders.ToString("N0"),report.ProcessingOrders),
            new("Chờ xác nhận",$"{report.PendingOrders:N0} đơn",report.PendingOrders.ToString("N0"),report.PendingOrders),
            new("Đã hủy",$"{report.CancelledOrders:N0} đơn",report.CancelledOrders.ToString("N0"),report.CancelledOrders)
        ],"Chưa có đơn hàng trong kỳ.");
        _categories.SetRows(report.RevenueByCategory.Select(x=>new BarItem(x.Category,$"{x.Quantity:N0} cuốn",Money(x.Revenue),x.Revenue)).ToArray(),"Chưa có doanh thu trong kỳ.");
        _customers.SetRows(report.TopCustomers.Select(x=>new BarItem(x.CustomerName,$"{x.Orders:N0} đơn · {x.Quantity:N0} cuốn",Money(x.Revenue),x.Revenue)).ToArray(),"Chưa có đơn hoàn tất trong kỳ.");
        Change(_revChange,report.RevenueChangePercent); _revNote.Text=$"Kỳ trước: {Money(report.PreviousPeriodRevenue)}";
        Change(_orderChange,report.CompletedOrdersChangePercent); _orderNote.Text=$"Kỳ trước: {report.PreviousPeriodCompletedOrders:N0} đơn";
    }

    private void BuildHeading()
    {
        _heading.Controls.Add(new Label { Text="PHÂN TÍCH KINH DOANH",AutoSize=true,ForeColor=Green,Font=new Font("Segoe UI",8.5f,FontStyle.Bold),Location=new Point(0,0) });
        _heading.Controls.Add(new Label { Text="Báo cáo cửa hàng",AutoSize=true,ForeColor=Ink,Font=new Font("Segoe UI",19f,FontStyle.Bold),Location=new Point(0,17) });
        var periods=new[]{(7,"7 ngày"),(30,"30 ngày"),(90,"90 ngày"),(365,"1 năm")};
        for(int i=0;i<periods.Length;i++){var days=periods[i].Item1;var b=new Button{Text=periods[i].Item2,Tag=days,AutoSize=true,Height=27,FlatStyle=FlatStyle.Flat,Margin=new Padding(2,0,2,0),Padding=new Padding(9,1,9,1),Font=new Font("Segoe UI",8.5f,FontStyle.Bold),Cursor=Cursors.Hand};b.FlatAppearance.BorderSize=0;b.Click+=(_,_)=>PeriodSelected?.Invoke(days);_periodButtons[i]=b;_periods.Controls.Add(b);}
        _heading.Controls.Add(_periods);_heading.Controls.Add(_status);
    }
    private void BuildStats()
    {
        for(int i=0;i<3;i++)_stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
        for(int i=0;i<2;i++)_stats.RowStyles.Add(new RowStyle(SizeType.Percent,50));
        var captions=new[]{"DOANH THU THUẦN","ĐƠN HÀNG","GIÁ TRỊ ĐƠN TRUNG BÌNH","GIẢM GIÁ","CHI NHẬP HÀNG","LÃI GỘP ƯỚC TÍNH"};
        var notes=new[]{"Đơn hoàn tất, sau giảm giá đơn","Đơn hoàn tất và đang chờ","Tính trên đơn hoàn tất","Tổng giảm giá trên đơn hoàn tất","Phiếu nhập trong kỳ","Theo giá vốn bình quân"};
        for(int i=0;i<6;i++){var p=Card();p.Margin=new Padding(5);p.Padding=new Padding(11,9,11,5);
            var note=new Label{Text=notes[i],Dock=DockStyle.Bottom,Height=28,ForeColor=Muted,Font=new Font("Segoe UI",8f),AutoEllipsis=true};
            var value=new Label{Text="—",Dock=DockStyle.Top,Height=35,ForeColor=Ink,Font=new Font("Segoe UI",16f,FontStyle.Bold),AutoEllipsis=true};
            var cap=new Label{Text=captions[i],Dock=DockStyle.Top,Height=18,ForeColor=Muted,Font=new Font("Segoe UI",8f,FontStyle.Bold),AutoEllipsis=true};
            p.Controls.Add(note);p.Controls.Add(value);p.Controls.Add(cap);_values[i]=value;_notes[i]=note;_stats.Controls.Add(p,i%3,i/3);}
    }
    private void BuildChart()
    {
        _chart.Dock=DockStyle.Top;_chartHost.Controls.Add(_chart);
        var legend=new FlowLayoutPanel{AutoSize=true,WrapContents=false,FlowDirection=FlowDirection.LeftToRight,Anchor=AnchorStyles.Top|AnchorStyles.Right,Location=new Point(500,29),BackColor=Color.White};
        legend.Controls.Add(Legend("Doanh thu",Green));legend.Controls.Add(Legend("Nhập hàng",Gold));
        _chartCard.Controls.Add(_chartHost);_chartCard.Controls.Add(Kicker("DÒNG TIỀN BÁN HÀNG",new Point(14,10)));
        _chartCard.Controls.Add(Title("Doanh thu và nhập hàng theo ngày",new Point(14,27)));
        _chartCard.Controls.Add(legend);_chartCard.Controls.Add(new Label{Text="Doanh thu từ đơn hoàn tất · Chi nhập hàng từ phiếu đã ghi nhận",AutoSize=true,ForeColor=Muted,Font=new Font("Segoe UI",8f),Location=new Point(14,52)});
        _chartCard.Resize+=(_,_)=>{legend.Left=Math.Max(310,_chartCard.ClientSize.Width-legend.Width-16);_chartHost.Width=Math.Max(300,_chartCard.ClientSize.Width-28);};
    }
    private void BuildLists()
    {
        for(int i=0;i<2;i++){_lists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));_lists.RowStyles.Add(new RowStyle(SizeType.Percent,50));}
        var cards=new[]{_best,_orders,_categories,_customers};
        for(int i=0;i<cards.Length;i++){cards[i].Margin=new Padding(5);_lists.Controls.Add(cards[i],i%2,i/2);}
    }
    private void BuildComparison()
    {
        _comparison.Controls.Add(Kicker("SO SÁNH HIỆU QUẢ",new Point(14,9)));_comparison.Controls.Add(Title("So với kỳ liền trước",new Point(14,25)));
        var grid=new TableLayoutPanel{Left=14,Top=51,Height=91,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,ColumnCount=2,RowCount=1,BackColor=Color.White};
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        grid.Controls.Add(ChangeMetric("Doanh thu thuần",out _revChange,out _revNote),0,0);grid.Controls.Add(ChangeMetric("Đơn hoàn tất",out _orderChange,out _orderNote),1,0);
        _comparison.Controls.Add(grid);_comparison.Resize+=(_,_)=>grid.Width=Math.Max(200,_comparison.ClientSize.Width-28);
    }
    private void ResizePage()
    {
        int full=Math.Max(420,_scroll.ClientSize.Width-2);_stack.Width=full;int inner=Math.Max(360,full-_stack.Padding.Horizontal);
        foreach(Control c in _stack.Controls)c.Width=inner;
        _periods.Width=Math.Min(inner,420);_status.Left=_periods.Right+12;_status.MaximumSize=new Size(Math.Max(120,inner-_status.Left),0);
        foreach(Control c in _lists.Controls)c.Width=Math.Max(150,(inner-20)/2);
        _stack.Height=_stack.Controls.Cast<Control>().Sum(c=>c.Height+c.Margin.Vertical)+_stack.Padding.Vertical+4;
        _scroll.AutoScrollMinSize=new Size(0,_stack.Height);ResizeChart();
    }
    private void ResizeChart(){if(_chartHost.ClientSize.Width>0)_chart.Width=Math.Max(_chartHost.ClientSize.Width-4,(_report?.RevenueByDay.Count??_days)*25+90);}
    private void SetMetric(int i,string value,string note){_values[i].Text=value;_notes[i].Text=note;}
    private void UpdatePeriods(){foreach(var b in _periodButtons){bool selected=b.Tag is int d&&d==_days;b.BackColor=selected?Color.White:Color.FromArgb(233,237,231);b.ForeColor=selected?Ink:Color.FromArgb(102,113,100);}}
    private void Empty(){foreach(var v in _values)v.Text="—";_status.Text="Chọn kỳ báo cáo để tải số liệu.";_best.SetRows([],"Chưa tải dữ liệu.");_orders.SetRows([],"Chưa tải dữ liệu.");_categories.SetRows([],"Chưa tải dữ liệu.");_customers.SetRows([],"Chưa tải dữ liệu.");}
    private static void Change(Label label,decimal? value){label.Text=value is null?"Chưa có dữ liệu kỳ trước":$"{(value>=0?"+":"")}{value.Value:N1}%";label.ForeColor=value is null?Muted:value>=0?Green:Color.FromArgb(180,92,76);}
    private static Panel ChangeMetric(string caption,out Label value,out Label note){var p=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(248,248,244),Margin=new Padding(3),Padding=new Padding(7)};p.Controls.Add(new Label{Text=caption,AutoSize=true,Location=new Point(7,3),ForeColor=Muted,Font=new Font("Segoe UI",8f)});value=new Label{Text="—",AutoSize=true,Location=new Point(7,20),ForeColor=Ink,Font=new Font("Segoe UI",11.5f,FontStyle.Bold)};note=new Label{Text="",AutoSize=true,Location=new Point(7,44),ForeColor=Muted,Font=new Font("Segoe UI",7.5f)};p.Controls.Add(value);p.Controls.Add(note);return p;}
    private static Panel Card()=>new(){BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle};
    private static Label Kicker(string text,Point p)=>new(){Text=text,AutoSize=true,ForeColor=Green,Font=new Font("Segoe UI",8f,FontStyle.Bold),Location=p};
    private static Label Title(string text,Point p)=>new(){Text=text,AutoSize=true,ForeColor=Ink,Font=new Font("Segoe UI",11.5f,FontStyle.Bold),Location=p};
    private static Control Legend(string text,Color color){var p=new FlowLayoutPanel{AutoSize=true,WrapContents=false,FlowDirection=FlowDirection.LeftToRight,Margin=new Padding(6,2,4,0),BackColor=Color.White};p.Controls.Add(new Panel{Width=10,Height=10,BackColor=color,Margin=new Padding(0,4,5,0)});p.Controls.Add(new Label{Text=text,AutoSize=true,ForeColor=Muted,Font=new Font("Segoe UI",8f)});return p;}
    private static string Money(decimal n)=>MoneyFormatter.ToVnd(n);
    private sealed record BarItem(string Name,string Detail,string Amount,decimal Value);

    private sealed class BreakdownCard:Panel
    {
        private readonly FlowLayoutPanel _rows=new(){Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(12,54,12,6),BackColor=Color.White};
        private readonly Color _color;
        public BreakdownCard(string kicker,string title,Color color){_color=color;BackColor=Color.White;BorderStyle=BorderStyle.FixedSingle;Controls.Add(_rows);Controls.Add(Kicker(kicker,new Point(13,9)));Controls.Add(Title(title,new Point(13,25)));Resize+=(_,_)=>{foreach(Control r in _rows.Controls)r.Width=Math.Max(110,_rows.ClientSize.Width-8);};}
        public void SetRows(IReadOnlyList<BarItem> rows,string empty){_rows.SuspendLayout();_rows.Controls.Clear();decimal max=rows.Select(x=>x.Value).DefaultIfEmpty(0).Max();if(rows.Count==0)_rows.Controls.Add(new Label{Text=empty,AutoSize=true,ForeColor=Muted,Font=new Font("Segoe UI",8.5f),Margin=new Padding(0,18,0,0)});foreach(var item in rows)_rows.Controls.Add(MakeRow(item,max));_rows.ResumeLayout();}
        private Control MakeRow(BarItem item,decimal max){var row=new Panel{Height=51,Width=Math.Max(110,_rows.ClientSize.Width-8),Margin=new Padding(0,0,0,4),BackColor=Color.White};
            var name=new Label{Text=item.Name,Left=0,Top=0,Width=row.Width-130,Height=17,AutoEllipsis=true,ForeColor=Color.FromArgb(70,80,68),Font=new Font("Segoe UI",8f,FontStyle.Bold)};
            var amount=new Label{Text=item.Amount,Left=row.Width-126,Top=0,Width=124,Height=17,AutoEllipsis=true,TextAlign=ContentAlignment.TopRight,ForeColor=Ink,Font=new Font("Segoe UI",8f,FontStyle.Bold)};
            var detail=new Label{Text=item.Detail,Left=0,Top=17,Width=row.Width,Height=14,AutoEllipsis=true,ForeColor=Muted,Font=new Font("Segoe UI",7.5f)};
            var track=new Panel{Left=0,Top=36,Width=row.Width,Height=7,BackColor=Color.FromArgb(238,240,235)};var fill=new Panel{Dock=DockStyle.Left,BackColor=_color};track.Controls.Add(fill);
            row.Controls.Add(name);row.Controls.Add(amount);row.Controls.Add(detail);row.Controls.Add(track);
            row.Resize+=(_,_)=>{int nw=Math.Max(65,row.ClientSize.Width-132);name.Width=nw;amount.Left=nw+5;amount.Width=Math.Max(35,row.ClientSize.Width-amount.Left);detail.Width=row.ClientSize.Width;track.Width=row.ClientSize.Width;fill.Width=max<=0?0:Math.Max(2,(int)(row.ClientSize.Width*Math.Clamp(item.Value/max,0m,1m)));};return row;}
    }

    private sealed class RevenueChart:Panel
    {
        private ManagerReport? _report;
        public void SetReport(ManagerReport? report){_report=report;Invalidate();}
        public RevenueChart(){BackColor=Color.White;SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.UserPaint,true);}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var days=_report?.RevenueByDay;if(days is null||days.Count==0){TextRenderer.DrawText(e.Graphics,"Chưa có dữ liệu trong kỳ.",Font,ClientRectangle,Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;}
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float left=72,right=Math.Max(132,ClientSize.Width-16),top=12,bottom=ClientSize.Height-38,height=Math.Max(40,bottom-top);decimal max=Ceiling(days.Select(x=>Math.Max(x.Revenue,x.PurchaseSpend)).DefaultIfEmpty(0m).Max());
            using var grid=new Pen(Line,1);using var axis=new SolidBrush(Muted);using var green=new SolidBrush(Green);using var gold=new SolidBrush(Gold);using var font=new Font("Segoe UI",8f);using var rightFmt=new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center};using var centerFmt=new StringFormat{Alignment=StringAlignment.Center};
            for(int t=0;t<=4;t++){float y=top+height*t/4f;e.Graphics.DrawLine(grid,left,y,right,y);e.Graphics.DrawString(Compact(max*(4-t)/4m),font,axis,new RectangleF(0,y-8,left-8,16),rightFmt);}
            float slot=(right-left)/days.Count,bar=Math.Clamp(slot*.28f,3f,12f);int step=Math.Max(1,(days.Count+7)/8);
            for(int i=0;i<days.Count;i++){float x=left+(i+.5f)*slot;Draw(days[i].Revenue,x-bar-1,green);Draw(days[i].PurchaseSpend,x+1,gold);if(i%step==0||i==days.Count-1)e.Graphics.DrawString(days[i].Date.ToString("dd/MM"),font,axis,new RectangleF(x-slot*1.5f,bottom+6,slot*3,18),centerFmt);}
            void Draw(decimal value,float x,Brush brush){if(value<=0)return;float h=Math.Max(1,(float)(value/max)*height);e.Graphics.FillRectangle(brush,x,bottom-h,Math.Max(1,bar),h);}
        }
        private static decimal Ceiling(decimal v){if(v<=0)return 1;decimal m=1;while(v>=m*10&&m<=decimal.MaxValue/10)m*=10;while(v<m)m/=10;decimal n=v/m;decimal s=n<=1?1:n<=2?2:n<=2.5m?2.5m:n<=5?5:10;return s*m;}
        private static string Compact(decimal v)=>v>=1_000_000?$"{v/1_000_000:0.#}tr":v>=1_000?$"{v/1_000:0.#}k":v.ToString("0");
    }
}
