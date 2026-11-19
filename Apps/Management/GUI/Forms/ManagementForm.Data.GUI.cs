using System.Globalization;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.GUI;

public partial class ManagementForm
{
    private void LoadDashboard(bool refresh = false)
    {
        var summary = _services.GetDashboard(refresh);
        var trend = _services.Manager.GetReport(7);
        _dashboardView?.ShowDashboard(summary, trend);
        _status.Text = $"Tổng quan đã cập nhật · SQL Server: {_services.DatabaseConnectionCaption}";
    }

    private void LoadBooks(bool refresh = false) => _booksGrid.DataSource = _services.GetBooks(refresh).ToList();

    private void LoadAuthors(bool refresh = false) => _authorsGrid.DataSource = _services.GetAuthors(refresh).ToList();

    private void LoadCustomers(bool refresh = false) => _customersGrid.DataSource = _services.GetCustomers(refresh).ToList();

    private void LoadEmployees(bool refresh = false) => _employeesGrid.DataSource = _services.GetEmployees(refresh).ToList();

    private void LoadOrders(bool refresh = false) => _ordersGrid.DataSource = _services.GetOrders(refresh).ToList();

    private void LoadReceipts(bool refresh = false) => _receiptsGrid.DataSource = _services.GetReceipts(refresh).ToList();

    private void LoadSuppliers(bool refresh = false) => _suppliersGrid.DataSource = _services.GetLookups(refresh).Suppliers.ToList();

    private ManagerLookupsDto GetLookups(bool refresh = false) => _services.GetLookups(refresh);

    private void LoadPromotions(bool refresh = false) => _promotionsGrid.DataSource = _services.GetPromotions(refresh).ToList();

    private void LoadVouchers(bool refresh = false) => _vouchersGrid.DataSource = _services.GetVouchers(refresh).ToList();

    private void LoadLogs(bool refresh = false) => _logsGrid.DataSource = _services.GetAuditLogs(refresh).ToList();

    private void LoadReport(int days)
    {
        if (_reportView is null) return;
        _reportView.SetLoading(days);
        var report = _services.Manager.GetReport(days);
        var history = days >= 1820 ? report : _services.Manager.GetReport(1825);
        _currentReport = report;
        _currentHistoryReport = history;
        _reportView.ShowReport(report, history);
    }

    private void LoadReport(DateOnly fromDate, DateOnly toDate)
    {
        if (_reportView is null) return;
        if (toDate < fromDate) throw new InvalidOperationException("Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.");
        _reportView.SetLoading(fromDate, toDate);
        var report = _services.Manager.GetReport(fromDate, toDate);
        var history = (toDate.DayNumber - fromDate.DayNumber + 1) >= 1820 ? report : _services.Manager.GetReport(1825);
        _currentReport = report;
        _currentHistoryReport = history;
        _reportView.ShowReport(report, history);
    }

    private void ExportReportExcel(ManagerReportDto report)
    {
        using var dialog = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx|Excel 97-2003 (*.xls)|*.xls", FileName = $"bao-cao-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.xlsx" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        ExportExcel(report, _currentHistoryReport ?? report, dialog.FileName);
        MessageBox.Show(this, "Đã xuất báo cáo Excel với đầy đủ các trang tổng quan, theo ngày, tháng, quý, năm, nhân viên, sản phẩm bán chạy, thể loại, khách hàng và tồn kho.", "Xuất báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportReportCsv(ManagerReportDto report)
    {
        using var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"bao-cao-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var history = _currentHistoryReport ?? report;
        var csv = new System.Text.StringBuilder();

        static string CsvCell(object? val)
        {
            if (val is null) return "";
            var str = val is decimal d ? d.ToString(CultureInfo.InvariantCulture)
                    : val is double db ? db.ToString(CultureInfo.InvariantCulture)
                    : val is float f ? f.ToString(CultureInfo.InvariantCulture)
                    : val.ToString() ?? "";
            if (str.Contains(',') || str.Contains('"') || str.Contains('\n') || str.Contains('\r'))
                return $"\"{str.Replace("\"", "\"\"")}\"";
            return str;
        }

        static void AppendSection(System.Text.StringBuilder sb, string sectionTitle, List<object?[]> rows)
        {
            sb.AppendLine($"# {sectionTitle}");
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(',', row.Select(CsvCell)));
            }
            sb.AppendLine();
        }

        var sheets = GetReportSheets(report, history);
        foreach (var (name, rows) in sheets)
        {
            AppendSection(csv, name, rows);
        }

        File.WriteAllText(dialog.FileName, csv.ToString(), new System.Text.UTF8Encoding(true));
        MessageBox.Show(this, "Đã xuất báo cáo CSV đồng bộ đầy đủ các phần với Excel.", "Xuất báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static List<(string Name, List<object?[]> Rows)> GetReportSheets(ManagerReportDto report, ManagerReportDto history)
    {
        return
        [
            ("Tong quan", BuildSummary(report)),
            ("Theo ngay", [ ["Ngày", "Số đơn hoàn tất", "Doanh thu bán", "Tiền nhập hàng"], .. report.RevenueByDayDto.OrderBy(x => x.Date).Select(row => new object?[] { row.Date.ToString("yyyy-MM-dd"), row.Orders, row.Revenue, row.PurchaseSpend }) ]),
            ("Theo thang", [ ["Năm", "Tháng", "Số đơn bán", "Tiền bán", "Tiền nhập", "Lãi gộp ước tính"], .. (history.RevenueByMonth.Count > 0 ? history.RevenueByMonth : report.RevenueByMonth).Select(row => new object?[] { row.Year, row.Month, row.Orders, row.Revenue, row.PurchaseSpend, row.EstimatedGrossProfit }) ]),
            ("Theo quy", [ ["Năm", "Quý", "Số đơn bán", "Tiền bán", "Tiền nhập", "Lãi gộp ước tính"], .. (history.RevenueByQuarter.Count > 0 ? history.RevenueByQuarter : report.RevenueByQuarter).Select(row => new object?[] { row.Year, row.Quarter, row.Orders, row.Revenue, row.PurchaseSpend, row.EstimatedGrossProfit }) ]),
            ("Theo nam", [ ["Năm", "Số đơn bán", "Tiền bán", "Tiền nhập", "Lãi gộp ước tính"], .. (history.RevenueByYear.Count > 0 ? history.RevenueByYear : report.RevenueByYear).Select(row => new object?[] { row.Year, row.Orders, row.Revenue, row.PurchaseSpend, row.EstimatedGrossProfit }) ]),
            ("Theo nhan vien", [ ["Mã nhân viên", "Nhân viên", "Năm", "Số đơn", "Doanh thu"], .. (history.RevenueByEmployeeYear.Count > 0 ? history.RevenueByEmployeeYear : report.RevenueByEmployeeYear).Select(row => new object?[] { row.EmployeeId, row.EmployeeName, row.Year, row.Orders, row.Revenue }) ]),
            ("Theo san pham", [ ["Mã sách", "Tên sách", "Năm", "Quý", "Số lượng", "Doanh thu"], .. (history.ProductSalesByQuarter.Count > 0 ? history.ProductSalesByQuarter : report.ProductSalesByQuarter).Select(row => new object?[] { row.BookId, row.Title, row.Year, row.Quarter, row.Quantity, row.Revenue }) ]),
            ("Sach ban chay", [ ["Mã sách", "Tên sách", "Số lượng bán", "Doanh thu"], .. report.BestSellers.Select(row => new object?[] { row.BookId, row.Title, row.Quantity, row.Revenue }) ]),
            ("The loai", [ ["Thể loại", "Số lượng bán", "Doanh thu"], .. report.RevenueByCategory.Select(row => new object?[] { row.Category, row.Quantity, row.Revenue }) ]),
            ("Khach hang", [ ["Mã khách hàng", "Tên khách hàng", "Số đơn", "Số cuốn", "Doanh thu"], .. report.TopCustomers.Select(row => new object?[] { row.CustomerId, row.CustomerName, row.Orders, row.Quantity, row.Revenue }) ]),
            ("Ton kho", [ ["Thể loại", "Số đầu sách", "Tổng tồn kho", "Sách sắp hết", "Giá trị tồn kho"], .. report.InventoryByCategory.Select(row => new object?[] { row.Category, row.BookCount, row.StockQuantity, row.LowStockBooks, row.StockValue }) ])
        ];
    }

    private void ExportExcel(ManagerReportDto report, ManagerReportDto history, string path)
    {
        var sheets = GetReportSheets(report, history)
            .Select(s => new ExcelSheetData(s.Name, s.Rows))
            .ToList();
        new ExcelWorkbookService().Write(path, sheets);
    }

    private static List<object?[]> BuildSummary(ManagerReportDto report) =>
    [
        ["Chỉ tiêu", "Giá trị"],
        ["Từ ngày", report.FromDate.ToString("dd/MM/yyyy")], ["Đến ngày", report.ToDate.ToString("dd/MM/yyyy")],
        ["Doanh thu đơn hoàn tất", report.Revenue], ["Tổng đơn", report.TotalOrders],
        ["Đơn hoàn tất", report.CompletedOrders], ["Đơn đang giao", report.ProcessingOrders],
        ["Đơn chờ xác nhận", report.PendingOrders], ["Đơn đã hủy", report.CancelledOrders],
        ["Giá trị đơn trung bình", report.AverageOrderValue], ["Giảm giá", report.Discounts],
        ["Chi nhập hàng", report.PurchaseSpend], ["Lãi gộp ước tính", report.EstimatedGrossProfit]
    ];
}



