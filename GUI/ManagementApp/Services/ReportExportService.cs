using System.Globalization;
using BookStore.Shared.Models;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TrangSach.Management.Services;

internal sealed class ReportExportService
{
    private static bool _configured;

    public void ExportExcel(ManagerReport report, string path)
    {
        var sheets = new[]
        {
            new ExcelSheetData("Tong quan", BuildSummary(report)),
            new ExcelSheetData("Theo thang", [ ["Năm", "Tháng", "Số đơn", "Doanh thu"], .. report.RevenueByMonth.Select(row => new object?[] { row.Year, row.Month, row.Orders, row.Revenue }) ]),
            new ExcelSheetData("Theo quy", [ ["Năm", "Quý", "Số đơn", "Doanh thu"], .. report.RevenueByQuarter.Select(row => new object?[] { row.Year, row.Quarter, row.Orders, row.Revenue }) ]),
            new ExcelSheetData("Theo nam", [ ["Năm", "Số đơn", "Doanh thu"], .. report.RevenueByYear.Select(row => new object?[] { row.Year, row.Orders, row.Revenue }) ]),
            new ExcelSheetData("Theo nhan vien", [ ["Mã nhân viên", "Nhân viên", "Năm", "Số đơn", "Doanh thu"], .. report.RevenueByEmployeeYear.Select(row => new object?[] { row.EmployeeId, row.EmployeeName, row.Year, row.Orders, row.Revenue }) ]),
            new ExcelSheetData("Theo san pham", [ ["Mã sách", "Tên sách", "Năm", "Quý", "Số lượng", "Doanh thu"], .. report.ProductSalesByQuarter.Select(row => new object?[] { row.BookId, row.Title, row.Year, row.Quarter, row.Quantity, row.Revenue }) ])
        };
        new ExcelWorkbookService().Write(path, sheets);
    }

    public void ExportPdf(ManagerReport report, string path)
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
                column.Item().Text("Doanh thu theo tháng").FontSize(12).SemiBold();
                AddTable(column, ["Năm", "Tháng", "Số đơn", "Doanh thu"], report.RevenueByMonth.Select(row => new[] { row.Year.ToString(), row.Month.ToString("00"), row.Orders.ToString("N0"), Money(row.Revenue) }).ToArray());
                column.Item().Text("Doanh thu theo quý").FontSize(12).SemiBold();
                AddTable(column, ["Năm", "Quý", "Số đơn", "Doanh thu"], report.RevenueByQuarter.Select(row => new[] { row.Year.ToString(), $"Q{row.Quarter}", row.Orders.ToString("N0"), Money(row.Revenue) }).ToArray());
                column.Item().Text("Doanh thu theo năm").FontSize(12).SemiBold();
                AddTable(column, ["Năm", "Số đơn", "Doanh thu"], report.RevenueByYear.Select(row => new[] { row.Year.ToString(), row.Orders.ToString("N0"), Money(row.Revenue) }).ToArray());
                column.Item().Text("Doanh thu theo nhân viên và năm").FontSize(12).SemiBold();
                AddTable(column, ["Mã nhân viên", "Nhân viên", "Năm", "Số đơn", "Doanh thu"], report.RevenueByEmployeeYear.Select(row => new[] { row.EmployeeId, row.EmployeeName, row.Year.ToString(), row.Orders.ToString("N0"), Money(row.Revenue) }).ToArray());
                column.Item().Text("Doanh thu sản phẩm theo quý và năm").FontSize(12).SemiBold();
                AddTable(column, ["Mã sách", "Tên sách", "Năm", "Quý", "SL", "Doanh thu"], report.ProductSalesByQuarter.Select(row => new[] { row.BookId, row.Title, row.Year.ToString(), $"Q{row.Quarter}", row.Quantity.ToString("N0"), Money(row.Revenue) }).ToArray());
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

    private static List<object?[]> BuildSummary(ManagerReport report) =>
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
        if (_configured) return;
        QuestPDF.Settings.License = LicenseType.Community;
        var arial = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (File.Exists(arial)) FontManager.RegisterFontFromFile(arial);
        _configured = true;
    }

}
