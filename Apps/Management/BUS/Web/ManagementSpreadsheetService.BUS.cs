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
        var currentYear = report.ToDate.Year;

        // 1. Sheet 5.a
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

        // 2. Sheet 5.b
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

        // 3. Sheet 5.c.1
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

        // 4. Sheet 5.c.2
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

        // 5. Sheet 5.d
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
