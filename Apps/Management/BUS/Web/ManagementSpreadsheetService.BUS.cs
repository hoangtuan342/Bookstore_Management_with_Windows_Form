using System.Globalization;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using TrangSach.Shared.DTO;

namespace TrangSach.Management.Web.BUS;

public sealed class ManagementSpreadsheetService
{
    public byte[] CreateWorkbook(List<(string Name, List<object?[]> Rows)> sheets)
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

    public List<string[]> ReadFirstSheet(Stream stream)
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

    public List<(string Name, List<object?[]> Rows)> GetReportSheets(ManagerReportDto report, ManagerReportDto? history = null)
    {
        var h = history ?? report;
        return
        [
            ("Tong quan", [
                ["Chỉ tiêu", "Giá trị"],
                ["Từ ngày", report.FromDate.ToString("dd/MM/yyyy")],
                ["Đến ngày", report.ToDate.ToString("dd/MM/yyyy")],
                ["Doanh thu", report.Revenue],
                ["Tổng đơn", report.TotalOrders],
                ["Đơn hoàn tất", report.CompletedOrders],
                ["Giảm giá", report.Discounts],
                ["Chi nhập hàng", report.PurchaseSpend],
                ["Lãi gộp ước tính", report.EstimatedGrossProfit]
            ]),
            ("Theo ngay", [ ["Ngày", "Số đơn", "Doanh thu", "Tiền nhập"], .. report.RevenueByDayDto.OrderBy(x => x.Date).Select(x => new object?[] { x.Date.ToString("yyyy-MM-dd"), x.Orders, x.Revenue, x.PurchaseSpend }) ]),
            ("Theo thang", [ ["Năm", "Tháng", "Số đơn", "Doanh thu", "Tiền nhập", "Lãi gộp"], .. (h.RevenueByMonth.Count > 0 ? h.RevenueByMonth : report.RevenueByMonth).Select(x => new object?[] { x.Year, x.Month, x.Orders, x.Revenue, x.PurchaseSpend, x.EstimatedGrossProfit }) ]),
            ("Theo quy", [ ["Năm", "Quý", "Số đơn", "Doanh thu", "Tiền nhập", "Lãi gộp"], .. (h.RevenueByQuarter.Count > 0 ? h.RevenueByQuarter : report.RevenueByQuarter).Select(x => new object?[] { x.Year, x.Quarter, x.Orders, x.Revenue, x.PurchaseSpend, x.EstimatedGrossProfit }) ]),
            ("Theo nam", [ ["Năm", "Số đơn", "Doanh thu", "Tiền nhập", "Lãi gộp"], .. (h.RevenueByYear.Count > 0 ? h.RevenueByYear : report.RevenueByYear).Select(x => new object?[] { x.Year, x.Orders, x.Revenue, x.PurchaseSpend, x.EstimatedGrossProfit }) ]),
            ("Nhan vien", [ ["Mã nhân viên", "Nhân viên", "Năm", "Số đơn", "Doanh thu"], .. (h.RevenueByEmployeeYear.Count > 0 ? h.RevenueByEmployeeYear : report.RevenueByEmployeeYear).Select(x => new object?[] { x.EmployeeId, x.EmployeeName, x.Year, x.Orders, x.Revenue }) ]),
            ("San pham", [ ["Mã sách", "Tên sách", "Năm", "Quý", "Số lượng", "Doanh thu"], .. (h.ProductSalesByQuarter.Count > 0 ? h.ProductSalesByQuarter : report.ProductSalesByQuarter).Select(x => new object?[] { x.BookId, x.Title, x.Year, x.Quarter, x.Quantity, x.Revenue }) ]),
            ("Ban chay", [ ["Mã sách", "Tên sách", "Số lượng", "Doanh thu"], .. report.BestSellers.Select(x => new object?[] { x.BookId, x.Title, x.Quantity, x.Revenue }) ]),
            ("Theo the loai", [ ["Thể loại", "Số lượng", "Doanh thu"], .. report.RevenueByCategory.Select(x => new object?[] { x.Category, x.Quantity, x.Revenue }) ]),
            ("Khach hang", [ ["Mã KH", "Khách hàng", "Số đơn", "Số cuốn", "Doanh thu"], .. report.TopCustomers.Select(x => new object?[] { x.CustomerId, x.CustomerName, x.Orders, x.Quantity, x.Revenue }) ]),
            ("Ton kho", [ ["Thể loại", "Số sách", "Tồn kho", "Sắp hết", "Giá trị tồn"], .. report.InventoryByCategory.Select(x => new object?[] { x.Category, x.BookCount, x.StockQuantity, x.LowStockBooks, x.StockValue }) ])
        ];
    }

    public byte[] CreateReportWorkbook(ManagerReportDto report, ManagerReportDto? history = null)
    {
        return CreateWorkbook(GetReportSheets(report, history));
    }

    public string CreateReportCsv(ManagerReportDto report, ManagerReportDto? history = null)
    {
        var sheets = GetReportSheets(report, history);
        var sb = new System.Text.StringBuilder();

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

        foreach (var (name, rows) in sheets)
        {
            sb.AppendLine($"# {name}");
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(',', row.Select(CsvCell)));
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
