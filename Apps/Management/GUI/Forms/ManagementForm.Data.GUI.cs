using System.Globalization;
using TrangSach.Shared.DTO;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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

    private void ExportReportPdf(ManagerReportDto report)
    {
        using var dialog = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = $"bao-cao-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.pdf" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        ExportPdf(report, _currentHistoryReport ?? report, dialog.FileName);
        MessageBox.Show(this, "Đã xuất báo cáo PDF.", "Xuất báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        var csv = new System.Text.StringBuilder("Mục báo cáo;Cột 1;Cột 2;Cột 3;Cột 4;Cột 5;Cột 6\r\n");

        csv.AppendLine("1. TỔNG QUAN;;;;;");
        csv.AppendLine($"Từ ngày;{report.FromDate:dd/MM/yyyy};Đến ngày;{report.ToDate:dd/MM/yyyy};;");
        csv.AppendLine($"Doanh thu đơn hoàn tất;{report.Revenue.ToString(CultureInfo.InvariantCulture)};Tổng đơn;{report.TotalOrders};Đơn hoàn tất;{report.CompletedOrders}");
        csv.AppendLine($"Chi nhập hàng;{report.PurchaseSpend.ToString(CultureInfo.InvariantCulture)};Lãi gộp ước tính;{report.EstimatedGrossProfit.ToString(CultureInfo.InvariantCulture)};Giá trị TB/đơn;{report.AverageOrderValue.ToString(CultureInfo.InvariantCulture)}");
        csv.AppendLine();

        csv.AppendLine("2. DOANH THU THEO NGÀY;Ngày;Số đơn;Doanh thu;Chi nhập;");
        foreach (var row in report.RevenueByDayDto.OrderBy(x => x.Date))
        {
            csv.AppendLine(string.Join(';', "Theo ngày", row.Date.ToString("yyyy-MM-dd"), row.Orders.ToString(CultureInfo.InvariantCulture), row.Revenue.ToString(CultureInfo.InvariantCulture), row.PurchaseSpend.ToString(CultureInfo.InvariantCulture), ""));
        }
        csv.AppendLine();

        csv.AppendLine("3. TOP SÁCH BÁN CHẠY;Mã sách;Tên sách;Số lượng bán;Doanh thu;");
        foreach (var row in report.BestSellers)
        {
            csv.AppendLine(string.Join(';', "Bán chạy", row.BookId, row.Title.Replace(';', ','), row.Quantity.ToString(CultureInfo.InvariantCulture), row.Revenue.ToString(CultureInfo.InvariantCulture), ""));
        }
        csv.AppendLine();

        csv.AppendLine("4. DOANH THU THEO THỂ LOẠI;Thể loại;Số lượng bán;Doanh thu;;");
        foreach (var row in report.RevenueByCategory)
        {
            csv.AppendLine(string.Join(';', "Thể loại", row.Category.Replace(';', ','), row.Quantity.ToString(CultureInfo.InvariantCulture), row.Revenue.ToString(CultureInfo.InvariantCulture), "", ""));
        }
        csv.AppendLine();

        csv.AppendLine("5. KHÁCH HÀNG MUA NHIỀU;Mã KH;Tên khách hàng;Số đơn;Số cuốn;Doanh thu");
        foreach (var row in report.TopCustomers)
        {
            csv.AppendLine(string.Join(';', "Khách hàng", row.CustomerId, row.CustomerName.Replace(';', ','), row.Orders.ToString(CultureInfo.InvariantCulture), row.Quantity.ToString(CultureInfo.InvariantCulture), row.Revenue.ToString(CultureInfo.InvariantCulture)));
        }
        csv.AppendLine();

        csv.AppendLine("6. TỒN KHO THEO THỂ LOẠI;Thể loại;Số đầu sách;Tổng tồn kho;Sách sắp hết;Giá trị tồn");
        foreach (var row in report.InventoryByCategory)
        {
            csv.AppendLine(string.Join(';', "Tồn kho", row.Category.Replace(';', ','), row.BookCount.ToString(CultureInfo.InvariantCulture), row.StockQuantity.ToString(CultureInfo.InvariantCulture), row.LowStockBooks.ToString(CultureInfo.InvariantCulture), row.StockValue.ToString(CultureInfo.InvariantCulture)));
        }

        File.WriteAllText(dialog.FileName, csv.ToString(), new System.Text.UTF8Encoding(true));
        MessageBox.Show(this, "Đã xuất báo cáo CSV toàn diện.", "Xuất báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static bool _pdfConfigured;

    private void ExportExcel(ManagerReportDto report, ManagerReportDto history, string path)
    {
        var sheets = new List<ExcelSheetData>
        {
            new ExcelSheetData("Tong quan", BuildSummary(report)),
            new ExcelSheetData("Theo ngay", [ ["Ngày", "Số đơn hoàn tất", "Doanh thu bán", "Tiền nhập hàng"], .. report.RevenueByDayDto.OrderBy(x => x.Date).Select(row => new object?[] { row.Date.ToString("yyyy-MM-dd"), row.Orders, row.Revenue, row.PurchaseSpend }) ]),
            new ExcelSheetData("Theo thang", [ ["Năm", "Tháng", "Số đơn bán", "Tiền bán", "Tiền nhập", "Lãi gộp ước tính"], .. (history.RevenueByMonth.Count > 0 ? history.RevenueByMonth : report.RevenueByMonth).Select(row => new object?[] { row.Year, row.Month, row.Orders, row.Revenue, row.PurchaseSpend, row.EstimatedGrossProfit }) ]),
            new ExcelSheetData("Theo quy", [ ["Năm", "Quý", "Số đơn bán", "Tiền bán", "Tiền nhập", "Lãi gộp ước tính"], .. (history.RevenueByQuarter.Count > 0 ? history.RevenueByQuarter : report.RevenueByQuarter).Select(row => new object?[] { row.Year, row.Quarter, row.Orders, row.Revenue, row.PurchaseSpend, row.EstimatedGrossProfit }) ]),
            new ExcelSheetData("Theo nam", [ ["Năm", "Số đơn bán", "Tiền bán", "Tiền nhập", "Lãi gộp ước tính"], .. (history.RevenueByYear.Count > 0 ? history.RevenueByYear : report.RevenueByYear).Select(row => new object?[] { row.Year, row.Orders, row.Revenue, row.PurchaseSpend, row.EstimatedGrossProfit }) ]),
            new ExcelSheetData("Theo nhan vien", [ ["Mã nhân viên", "Nhân viên", "Năm", "Số đơn", "Doanh thu"], .. (history.RevenueByEmployeeYear.Count > 0 ? history.RevenueByEmployeeYear : report.RevenueByEmployeeYear).Select(row => new object?[] { row.EmployeeId, row.EmployeeName, row.Year, row.Orders, row.Revenue }) ]),
            new ExcelSheetData("Theo san pham", [ ["Mã sách", "Tên sách", "Năm", "Quý", "Số lượng", "Doanh thu"], .. (history.ProductSalesByQuarter.Count > 0 ? history.ProductSalesByQuarter : report.ProductSalesByQuarter).Select(row => new object?[] { row.BookId, row.Title, row.Year, row.Quarter, row.Quantity, row.Revenue }) ]),
            new ExcelSheetData("Sach ban chay", [ ["Mã sách", "Tên sách", "Số lượng bán", "Doanh thu"], .. report.BestSellers.Select(row => new object?[] { row.BookId, row.Title, row.Quantity, row.Revenue }) ]),
            new ExcelSheetData("The loai", [ ["Thể loại", "Số lượng bán", "Doanh thu"], .. report.RevenueByCategory.Select(row => new object?[] { row.Category, row.Quantity, row.Revenue }) ]),
            new ExcelSheetData("Khach hang", [ ["Mã khách hàng", "Tên khách hàng", "Số đơn", "Số cuốn", "Doanh thu"], .. report.TopCustomers.Select(row => new object?[] { row.CustomerId, row.CustomerName, row.Orders, row.Quantity, row.Revenue }) ]),
            new ExcelSheetData("Ton kho", [ ["Thể loại", "Số đầu sách", "Tổng tồn kho", "Sách sắp hết", "Giá trị tồn kho"], .. report.InventoryByCategory.Select(row => new object?[] { row.Category, row.BookCount, row.StockQuantity, row.LowStockBooks, row.StockValue }) ])
        };
        new ExcelWorkbookService().Write(path, sheets);
    }

    private void ExportPdf(ManagerReportDto report, ManagerReportDto history, string path)
    {
        ConfigurePdf();
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(24);
            page.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(9));
            page.Header().Column(column =>
            {
                column.Item().Text("BÁO CÁO KINH DOANH NHÀ SÁCH").FontSize(18).SemiBold();
                column.Item().Text($"Kỳ báo cáo: {report.FromDate:dd/MM/yyyy} đến {report.ToDate:dd/MM/yyyy}");
            });
            page.Content().Column(column =>
            {
                column.Spacing(8);
                column.Item().Text("Tổng quan").FontSize(13).SemiBold();
                AddTable(column, ["Chỉ tiêu", "Giá trị"],
                [
                    ["Doanh thu đơn hoàn tất", Money(report.Revenue)],
                    ["Tổng đơn", report.TotalOrders.ToString("N0")],
                    ["Đơn hoàn tất", report.CompletedOrders.ToString("N0")],
                    ["Giá trị đơn trung bình", Money(report.AverageOrderValue)],
                    ["Chi nhập hàng", Money(report.PurchaseSpend)],
                    ["Lãi gộp ước tính", Money(report.EstimatedGrossProfit)]
                ]);

                var months = history.RevenueByMonth.Count > 0 ? history.RevenueByMonth : report.RevenueByMonth;
                column.Item().Text("Doanh thu theo tháng").FontSize(12).SemiBold();
                AddTable(column, ["Năm", "Tháng", "Đơn bán", "Tiền bán", "Tiền nhập", "Lãi gộp ước tính"], months.Select(row => new[] { row.Year.ToString(), row.Month.ToString("00"), row.Orders.ToString("N0"), Money(row.Revenue), Money(row.PurchaseSpend), Money(row.EstimatedGrossProfit) }).ToArray());

                var quarters = history.RevenueByQuarter.Count > 0 ? history.RevenueByQuarter : report.RevenueByQuarter;
                column.Item().Text("Doanh thu theo quý").FontSize(12).SemiBold();
                AddTable(column, ["Năm", "Quý", "Đơn bán", "Tiền bán", "Tiền nhập", "Lãi gộp ước tính"], quarters.Select(row => new[] { row.Year.ToString(), $"Q{row.Quarter}", row.Orders.ToString("N0"), Money(row.Revenue), Money(row.PurchaseSpend), Money(row.EstimatedGrossProfit) }).ToArray());

                var years = history.RevenueByYear.Count > 0 ? history.RevenueByYear : report.RevenueByYear;
                column.Item().Text("Doanh thu theo năm").FontSize(12).SemiBold();
                AddTable(column, ["Năm", "Đơn bán", "Tiền bán", "Tiền nhập", "Lãi gộp ước tính"], years.Select(row => new[] { row.Year.ToString(), row.Orders.ToString("N0"), Money(row.Revenue), Money(row.PurchaseSpend), Money(row.EstimatedGrossProfit) }).ToArray());

                var employees = history.RevenueByEmployeeYear.Count > 0 ? history.RevenueByEmployeeYear : report.RevenueByEmployeeYear;
                column.Item().Text("Doanh thu theo nhân viên và năm").FontSize(12).SemiBold();
                AddTable(column, ["Mã nhân viên", "Nhân viên", "Năm", "Số đơn", "Doanh thu"], employees.Select(row => new[] { row.EmployeeId, row.EmployeeName, row.Year.ToString(), row.Orders.ToString("N0"), Money(row.Revenue) }).ToArray());

                var products = history.ProductSalesByQuarter.Count > 0 ? history.ProductSalesByQuarter : report.ProductSalesByQuarter;
                column.Item().Text("Doanh thu sản phẩm theo quý và năm").FontSize(12).SemiBold();
                AddTable(column, ["Mã sách", "Tên sách", "Năm", "Quý", "SL", "Doanh thu"], products.Select(row => new[] { row.BookId, row.Title, row.Year.ToString(), $"Q{row.Quarter}", row.Quantity.ToString("N0"), Money(row.Revenue) }).ToArray());

                if (report.BestSellers.Count > 0)
                {
                    column.Item().Text("Top sách bán chạy").FontSize(12).SemiBold();
                    AddTable(column, ["Mã sách", "Tên sách", "Số lượng bán", "Doanh thu"], report.BestSellers.Select(row => new[] { row.BookId, row.Title, row.Quantity.ToString("N0"), Money(row.Revenue) }).ToArray());
                }

                if (report.RevenueByCategory.Count > 0)
                {
                    column.Item().Text("Doanh thu theo thể loại").FontSize(12).SemiBold();
                    AddTable(column, ["Thể loại", "Số lượng bán", "Doanh thu"], report.RevenueByCategory.Select(row => new[] { row.Category, row.Quantity.ToString("N0"), Money(row.Revenue) }).ToArray());
                }

                if (report.TopCustomers.Count > 0)
                {
                    column.Item().Text("Khách hàng mua nhiều").FontSize(12).SemiBold();
                    AddTable(column, ["Mã khách hàng", "Tên khách hàng", "Số đơn", "Số cuốn", "Doanh thu"], report.TopCustomers.Select(row => new[] { row.CustomerId, row.CustomerName, row.Orders.ToString("N0"), row.Quantity.ToString("N0"), Money(row.Revenue) }).ToArray());
                }

                if (report.InventoryByCategory.Count > 0)
                {
                    column.Item().Text("Tồn kho theo thể loại").FontSize(12).SemiBold();
                    AddTable(column, ["Thể loại", "Số đầu sách", "Tổng tồn kho", "Sắp hết", "Giá trị tồn"], report.InventoryByCategory.Select(row => new[] { row.Category, row.BookCount.ToString("N0"), row.StockQuantity.ToString("N0"), row.LowStockBooks.ToString("N0"), Money(row.StockValue) }).ToArray());
                }
            });
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Trang ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        })).GeneratePdf(path);
    }

    private static void AddTable(ColumnDescriptor column, string[] headings, string[][] rows)
    {
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in headings) columns.RelativeColumn();
            });
            table.Header(header =>
            {
                foreach (var heading in headings)
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(heading).SemiBold();
            });
            foreach (var row in rows)
                foreach (var value in row)
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(value ?? string.Empty);
        });
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

    private static string Money(decimal amount) => amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";

    private static void ConfigurePdf()
    {
        if (_pdfConfigured) return;
        QuestPDF.Settings.License = LicenseType.Community;
        var arial = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (File.Exists(arial)) FontManager.RegisterFontFromFile(arial);
        _pdfConfigured = true;
    }

}



