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
        var currentYear = report.ToDate.Year;
        var h = history ?? report;

        // 1. Sheet 5.a: Thống kê tổng tiền HĐ từ ngày a đến ngày b
        var sheet5a = new List<object?[]>
        {
            new object?[] { "BÁO CÁO THỐNG KÊ 5.a) TỔNG TIỀN HÓA ĐƠN TỪ NGÀY a ĐẾN NGÀY b" },
            new object?[] { "Khoảng thời gian đối soát", $"{report.FromDate:dd/MM/yyyy} - {report.ToDate:dd/MM/yyyy}" },
            new object?[] { },
            new object?[] { "Chỉ số thống kê", "Giá trị", "Ghi chú đối soát" },
            new object?[] { "Tổng số hóa đơn phát sinh", report.TotalOrders, $"{report.CompletedOrders} hoàn tất, {report.PendingOrders} chờ, {report.CancelledOrders} hủy" },
            new object?[] { "HĐ (Tổng thu bán hàng)", report.Revenue, "Tổng tiền bán hàng thực tế sau giảm giá" },
            new object?[] { "PNH (Tổng chi nhập hàng)", report.PurchaseSpend, "Tổng giá trị phiếu nhập kho trong cùng kỳ" },
            new object?[] { "Lợi nhuận (Thu - Chi)", report.Revenue - report.PurchaseSpend, "Chênh lệch dòng tiền trực tiếp giữa bán và nhập" }
        };

        // 2. Sheet 5.b: Thống kê tổng tiền HĐ theo Tháng / Quý / Năm
        var quarters = h.RevenueByQuarter.Where(x => x.Year == currentYear).ToList();
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
        var sheet5b = new List<object?[]>
        {
            new object?[] { $"BÁO CÁO THỐNG KÊ 5.b) THEO QUÝ NĂM {currentYear} (THU - CHI - LỢI NHUẬN)" },
            new object?[] { },
            new object?[] { currentYear.ToString(), "Q1", "Q2", "Q3", "Q4", "TC (Tổng cộng)" },
            new object?[] { "HĐ (Tổng thu)", qRev[0], qRev[1], qRev[2], qRev[3], qRev.Sum() },
            new object?[] { "PNH (Tổng chi)", qCost[0], qCost[1], qCost[2], qCost[3], qCost.Sum() },
            new object?[] { "Lợi nhuận", qRev[0] - qCost[0], qRev[1] - qCost[1], qRev[2] - qCost[2], qRev[3] - qCost[3], qRev.Sum() - qCost.Sum() }
        };

        // 3. Sheet 5.c.1: Nhân viên qua các năm
        var empYears = h.RevenueByEmployeeYear.Select(x => x.Year).Distinct().OrderBy(y => y).ToList();
        if (empYears.Count == 0) empYears.Add(currentYear);
        var emps = h.RevenueByEmployeeYear.GroupBy(x => new { x.EmployeeId, x.EmployeeName }).Select(g => g.Key).OrderBy(e => e.EmployeeName).ToList();
        var empTotals = new decimal[empYears.Count];

        var sheet5cEmpHeader = new List<object?> { "NV (Nhân viên)" };
        foreach (var y in empYears) sheet5cEmpHeader.Add(y.ToString());
        sheet5cEmpHeader.Add("TC (Tổng cộng)");

        var sheet5cEmp = new List<object?[]>
        {
            new object?[] { "BÁO CÁO THỐNG KÊ 5.c) MA TRẬN DOANH SỐ HÓA ĐƠN THEO NHÂN VIÊN VÀ NĂM" },
            new object?[] { },
            sheet5cEmpHeader.ToArray()
        };

        foreach (var emp in emps)
        {
            var row = new List<object?> { string.IsNullOrWhiteSpace(emp.EmployeeName) ? emp.EmployeeId : $"{emp.EmployeeName} ({emp.EmployeeId})" };
            decimal rowTotal = 0m;
            for (int i = 0; i < empYears.Count; i++)
            {
                var y = empYears[i];
                var rev = h.RevenueByEmployeeYear.FirstOrDefault(x => x.EmployeeId == emp.EmployeeId && x.Year == y)?.Revenue ?? 0m;
                row.Add(rev);
                rowTotal += rev;
                empTotals[i] += rev;
            }
            row.Add(rowTotal);
            sheet5cEmp.Add(row.ToArray());
        }
        var tcEmpRow = new List<object?> { "TC" };
        foreach (var tot in empTotals) tcEmpRow.Add(tot);
        tcEmpRow.Add(empTotals.Sum());
        sheet5cEmp.Add(tcEmpRow.ToArray());

        // 4. Sheet 5.c.2: Khách hàng qua các năm
        var custYears = h.RevenueByCustomerYear.Select(x => x.Year).Distinct().OrderBy(y => y).ToList();
        if (custYears.Count == 0) custYears.Add(currentYear);
        var custs = h.RevenueByCustomerYear.GroupBy(x => new { x.CustomerId, x.CustomerName }).Select(g => g.Key).OrderBy(c => c.CustomerName).ToList();
        var custTotals = new decimal[custYears.Count];

        var sheet5cCustHeader = new List<object?> { "KH (Khách hàng)" };
        foreach (var y in custYears) sheet5cCustHeader.Add(y.ToString());
        sheet5cCustHeader.Add("TC (Tổng cộng)");

        var sheet5cCust = new List<object?[]>
        {
            new object?[] { "BÁO CÁO THỐNG KÊ 5.c) MA TRẬN DOANH SỐ HÓA ĐƠN THEO KHÁCH HÀNG VÀ NĂM" },
            new object?[] { },
            sheet5cCustHeader.ToArray()
        };

        foreach (var cust in custs)
        {
            var row = new List<object?> { string.IsNullOrWhiteSpace(cust.CustomerName) ? "Khách vãng lai" : $"{cust.CustomerName} ({cust.CustomerId})" };
            decimal rowTotal = 0m;
            for (int i = 0; i < custYears.Count; i++)
            {
                var y = custYears[i];
                var rev = h.RevenueByCustomerYear.FirstOrDefault(x => x.CustomerId == cust.CustomerId && x.Year == y)?.Revenue ?? 0m;
                row.Add(rev);
                rowTotal += rev;
                custTotals[i] += rev;
            }
            row.Add(rowTotal);
            sheet5cCust.Add(row.ToArray());
        }
        var tcCustRow = new List<object?> { "TC" };
        foreach (var tot in custTotals) tcCustRow.Add(tot);
        tcCustRow.Add(custTotals.Sum());
        sheet5cCust.Add(tcCustRow.ToArray());

        // 5. Sheet 5.d: Sản phẩm theo Quý trong Năm
        var prodData = h.ProductSalesByQuarter.Where(x => x.Year == currentYear).ToList();
        var prods = prodData.GroupBy(x => new { x.BookId, x.Title }).Select(g => g.Key).OrderBy(p => p.Title).ToList();
        var prodQTotals = new decimal[4];

        var sheet5d = new List<object?[]>
        {
            new object?[] { $"BÁO CÁO THỐNG KÊ 5.d) MA TRẬN TỔNG TIỀN HĐ THEO SẢN PHẨM & QUÝ NĂM {currentYear}" },
            new object?[] { },
            new object?[] { "SP (Sản phẩm)", "Q1", "Q2", "Q3", "Q4", "TC (Tổng cộng)" }
        };

        foreach (var p in prods)
        {
            decimal rowTotal = 0m;
            var row = new List<object?> { $"{p.Title} ({p.BookId})" };
            for (int q = 1; q <= 4; q++)
            {
                var rev = prodData.FirstOrDefault(x => x.BookId == p.BookId && x.Quarter == q)?.Revenue ?? 0m;
                row.Add(rev);
                rowTotal += rev;
                prodQTotals[q - 1] += rev;
            }
            row.Add(rowTotal);
            sheet5d.Add(row.ToArray());
        }
        sheet5d.Add(new object?[] { "TC", prodQTotals[0], prodQTotals[1], prodQTotals[2], prodQTotals[3], prodQTotals.Sum() });

        return
        [
            ("5a_TuNgayDenNgay", sheet5a),
            ("5b_ThuChiTheoQuy", sheet5b),
            ("5c_NhanVienTheoNam", sheet5cEmp),
            ("5c_KhachHangTheoNam", sheet5cCust),
            ("5d_SanPhamTheoQuy", sheet5d)
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



