using System.Globalization;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TrangSach.Shared.DTO;

namespace TrangSach.Management.Web.BUS;

public sealed class ManagementSpreadsheetService
{
    static ManagementSpreadsheetService() => QuestPDF.Settings.License = LicenseType.Community;

    public byte[] CreateWorkbook(IReadOnlyList<(string Name, IReadOnlyList<object?[]> Rows)> sheets)
    {
        if (sheets.Count == 0) throw new ArgumentException("Workbook cần ít nhất một trang tính.", nameof(sheets));
        using IWorkbook workbook = new XSSFWorkbook();
        foreach (var (name, rows) in sheets)
        {
            var safeName = string.Concat(name.Select(c => "[]:*?/\\".Contains(c) ? ' ' : c)).Trim();
            if (safeName.Length == 0) safeName = "Sheet";
            var sheet = workbook.CreateSheet(safeName[..Math.Min(safeName.Length, 31)]);
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = sheet.CreateRow(rowIndex);
                for (var col = 0; col < rows[rowIndex].Length; col++)
                {
                    var value = rows[rowIndex][col];
                    if (value is null) continue;
                    var cell = row.CreateCell(col);
                    if (value is DateTime date) cell.SetCellValue(date);
                    else if (value is bool boolean) cell.SetCellValue(boolean);
                    else if (value is string text) cell.SetCellValue(text);
                    else if (decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Number, CultureInfo.InvariantCulture, out var number)) cell.SetCellValue((double)number);
                    else cell.SetCellValue(Convert.ToString(value, CultureInfo.CurrentCulture));
                }
            }
            var columns = rows.Select(row => row.Length).DefaultIfEmpty().Max();
            for (var col = 0; col < columns; col++) sheet.AutoSizeColumn(col);
        }
        using var output = new MemoryStream();
        workbook.Write(output, leaveOpen: true);
        return output.ToArray();
    }

    public IReadOnlyList<string[]> ReadFirstSheet(Stream stream)
    {
        using var workbook = WorkbookFactory.Create(stream);
        if (workbook.NumberOfSheets == 0) throw new InvalidDataException("Tệp Excel không có trang tính.");
        var sheet = workbook.GetSheetAt(0);
        var formatter = new DataFormatter(CultureInfo.InvariantCulture);
        var evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
        var rows = new List<string[]>();
        for (var rowIndex = sheet.FirstRowNum; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row is null) { rows.Add([]); continue; }
            var count = Math.Max((int)row.LastCellNum, 0);
            var values = new string[count];
            for (var col = Math.Max((int)row.FirstCellNum, 0); col < count; col++) values[col] = formatter.FormatCellValue(row.GetCell(col), evaluator).Trim();
            rows.Add(values);
        }
        return rows;
    }

    public byte[] CreateReportWorkbook(ManagerReportDto report)
    {
        var sheets = new List<(string Name, IReadOnlyList<object?[]> Rows)>
        {
            ("Tong quan", [ ["Chỉ tiêu", "Giá trị"], ["Từ ngày", report.FromDate], ["Đến ngày", report.ToDate], ["Doanh thu", report.Revenue], ["Tổng đơn", report.TotalOrders], ["Đơn hoàn tất", report.CompletedOrders], ["Giảm giá", report.Discounts], ["Chi nhập hàng", report.PurchaseSpend], ["Lãi gộp ước tính", report.EstimatedGrossProfit] ]),
            ("Theo ngay", [ ["Ngày", "Đơn", "Doanh thu", "Tiền nhập"], .. report.RevenueByDayDto.Select(x => new object?[] { x.Date, x.Orders, x.Revenue, x.PurchaseSpend }) ]),
            ("Theo thang", [ ["Năm", "Tháng", "Đơn", "Doanh thu", "Tiền nhập", "Lãi gộp"], .. report.RevenueByMonth.Select(x => new object?[] { x.Year, x.Month, x.Orders, x.Revenue, x.PurchaseSpend, x.EstimatedGrossProfit }) ]),
            ("Theo quy", [ ["Năm", "Quý", "Đơn", "Doanh thu", "Tiền nhập", "Lãi gộp"], .. report.RevenueByQuarter.Select(x => new object?[] { x.Year, x.Quarter, x.Orders, x.Revenue, x.PurchaseSpend, x.EstimatedGrossProfit }) ]),
            ("Theo nam", [ ["Năm", "Đơn", "Doanh thu", "Tiền nhập", "Lãi gộp"], .. report.RevenueByYear.Select(x => new object?[] { x.Year, x.Orders, x.Revenue, x.PurchaseSpend, x.EstimatedGrossProfit }) ]),
            ("Nhan vien", [ ["Mã nhân viên", "Nhân viên", "Năm", "Đơn", "Doanh thu"], .. report.RevenueByEmployeeYear.Select(x => new object?[] { x.EmployeeId, x.EmployeeName, x.Year, x.Orders, x.Revenue }) ]),
            ("San pham", [ ["Mã sách", "Tên sách", "Năm", "Quý", "Số lượng", "Doanh thu"], .. report.ProductSalesByQuarter.Select(x => new object?[] { x.BookId, x.Title, x.Year, x.Quarter, x.Quantity, x.Revenue }) ]),
            ("Ban chay", [ ["Mã sách", "Tên sách", "Số lượng", "Doanh thu"], .. report.BestSellers.Select(x => new object?[] { x.BookId, x.Title, x.Quantity, x.Revenue }) ]),
            ("Theo the loai", [ ["Thể loại", "Số lượng", "Doanh thu"], .. report.RevenueByCategory.Select(x => new object?[] { x.Category, x.Quantity, x.Revenue }) ]),
            ("Khach hang", [ ["Khách hàng", "Số đơn", "Số cuốn", "Doanh thu"], .. report.TopCustomers.Select(x => new object?[] { x.CustomerName, x.Orders, x.Quantity, x.Revenue }) ]),
            ("Ton kho", [ ["Thể loại", "Số sách", "Tồn kho", "Sắp hết"], .. report.InventoryByCategory.Select(x => new object?[] { x.Category, x.BookCount, x.StockQuantity, x.LowStockBooks }) ])
        };
        return CreateWorkbook(sheets);
    }

    public byte[] CreateReportPdf(ManagerReportDto report)
    {
        using var output = new MemoryStream();
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(28);
            page.DefaultTextStyle(style => style.FontSize(9));
            page.Header().Column(column =>
            {
                column.Item().Text("BÁO CÁO KINH DOANH NHÀ SÁCH").FontSize(18).SemiBold();
                column.Item().Text($"Kỳ báo cáo: {report.FromDate:dd/MM/yyyy} đến {report.ToDate:dd/MM/yyyy}");
            });
            page.Content().Column(column =>
            {
                column.Spacing(10);
                PdfTable(column, ["Chỉ tiêu", "Giá trị"], [
                    ["Doanh thu đơn hoàn tất", Money(report.Revenue)], ["Tổng đơn", report.TotalOrders.ToString("N0")],
                    ["Đơn hoàn tất", report.CompletedOrders.ToString("N0")], ["Giá trị đơn trung bình", Money(report.AverageOrderValue)],
                    ["Chi nhập hàng", Money(report.PurchaseSpend)], ["Lãi gộp ước tính", Money(report.EstimatedGrossProfit)]]);
                column.Item().Text("Doanh thu theo tháng").FontSize(13).SemiBold();
                PdfTable(column, ["Năm", "Tháng", "Đơn", "Doanh thu", "Tiền nhập", "Lãi gộp"], report.RevenueByMonth.Select(x => new[] { x.Year.ToString(), x.Month.ToString("00"), x.Orders.ToString("N0"), Money(x.Revenue), Money(x.PurchaseSpend), Money(x.EstimatedGrossProfit) }).ToArray());
                column.Item().Text("Doanh thu theo quý / năm").FontSize(13).SemiBold();
                PdfTable(column, ["Năm", "Quý", "Đơn", "Doanh thu", "Tiền nhập", "Lãi gộp"], report.RevenueByQuarter.Select(x => new[] { x.Year.ToString(), $"Q{x.Quarter}", x.Orders.ToString("N0"), Money(x.Revenue), Money(x.PurchaseSpend), Money(x.EstimatedGrossProfit) }).ToArray());
                column.Item().Text("Doanh thu theo năm").FontSize(13).SemiBold();
                PdfTable(column, ["Năm", "Đơn", "Doanh thu", "Tiền nhập", "Lãi gộp"], report.RevenueByYear.Select(x => new[] { x.Year.ToString(), x.Orders.ToString("N0"), Money(x.Revenue), Money(x.PurchaseSpend), Money(x.EstimatedGrossProfit) }).ToArray());
                column.Item().Text("Doanh thu theo nhân viên và năm").FontSize(13).SemiBold();
                PdfTable(column, ["Mã nhân viên", "Nhân viên", "Năm", "Số đơn", "Doanh thu"], report.RevenueByEmployeeYear.Select(x => new[] { x.EmployeeId, x.EmployeeName, x.Year.ToString(), x.Orders.ToString("N0"), Money(x.Revenue) }).ToArray());
                column.Item().Text("Sản phẩm theo quý và năm").FontSize(13).SemiBold();
                PdfTable(column, ["Mã sách", "Tên sách", "Năm", "Quý", "Số lượng", "Doanh thu"], report.ProductSalesByQuarter.Select(x => new[] { x.BookId, x.Title, x.Year.ToString(), $"Q{x.Quarter}", x.Quantity.ToString("N0"), Money(x.Revenue) }).ToArray());
                column.Item().Text("Sản phẩm bán chạy").FontSize(13).SemiBold();
                PdfTable(column, ["Mã sách", "Tên sách", "Số lượng", "Doanh thu"], report.BestSellers.Select(x => new[] { x.BookId, x.Title, x.Quantity.ToString("N0"), Money(x.Revenue) }).ToArray());
            });
            page.Footer().AlignCenter().Text(text => { text.Span("Trang "); text.CurrentPageNumber(); text.Span(" / "); text.TotalPages(); });
        })).GeneratePdf(output);
        return output.ToArray();
    }

    private static void PdfTable(ColumnDescriptor column, string[] headings, string[][] rows) => column.Item().Table(table =>
    {
        table.ColumnsDefinition(columns => { foreach (var _ in headings) columns.RelativeColumn(); });
        table.Header(header => { foreach (var heading in headings) header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(heading).SemiBold(); });
        foreach (var row in rows) foreach (var value in row) table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(value ?? "");
    });

    private static string Money(decimal amount) => amount.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
}
