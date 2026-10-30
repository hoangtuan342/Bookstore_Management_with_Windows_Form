using System.Globalization;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace TrangSach.Management.Services;

internal sealed record ExcelSheetData(string Name, IReadOnlyList<object?[]> Rows);

internal sealed class ExcelWorkbookService
{
    public void Write(string path, IReadOnlyList<ExcelSheetData> sheets)
    {
        if (sheets.Count == 0) throw new ArgumentException("Workbook cần ít nhất một trang tính.", nameof(sheets));
        IWorkbook workbook = Path.GetExtension(path).Equals(".xls", StringComparison.OrdinalIgnoreCase)
            ? new HSSFWorkbook()
            : new XSSFWorkbook();
        using (workbook)
        {
            foreach (var source in sheets)
            {
                var sheet = workbook.CreateSheet(SafeSheetName(source.Name));
                for (var rowIndex = 0; rowIndex < source.Rows.Count; rowIndex++)
                {
                    var row = sheet.CreateRow(rowIndex);
                    for (var column = 0; column < source.Rows[rowIndex].Length; column++)
                    {
                        var value = source.Rows[rowIndex][column];
                        if (value is null) continue;
                        var cell = row.CreateCell(column);
                        switch (value)
                        {
                            case string text: cell.SetCellValue(text); break;
                            case DateTime date: cell.SetCellValue(date); break;
                            case bool boolean: cell.SetCellValue(boolean); break;
                            case byte or short or int or long or float or double or decimal:
                                cell.SetCellValue(Convert.ToDouble(value, CultureInfo.InvariantCulture)); break;
                            default: cell.SetCellValue(Convert.ToString(value, CultureInfo.CurrentCulture)); break;
                        }
                    }
                }
                for (var column = 0; column < source.Rows.Select(row => row.Length).DefaultIfEmpty().Max(); column++)
                    sheet.AutoSizeColumn(column);
            }
            using var stream = File.Create(path);
            workbook.Write(stream, leaveOpen: true);
        }
    }

    public IReadOnlyList<string[]> ReadFirstSheet(string path)
    {
        using var stream = File.OpenRead(path);
        using var workbook = WorkbookFactory.Create(stream);
        if (workbook.NumberOfSheets == 0) throw new InvalidDataException("Workbook không có trang tính.");
        var sheet = workbook.GetSheetAt(0);
        var formatter = new DataFormatter(CultureInfo.InvariantCulture);
        var evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
        var result = new List<string[]>();
        for (var rowIndex = sheet.FirstRowNum; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row is null) { result.Add([]); continue; }
            var values = new string[Math.Max((int)row.LastCellNum, 0)];
            for (var column = row.FirstCellNum; column >= 0 && column < row.LastCellNum; column++)
                values[column] = formatter.FormatCellValue(row.GetCell(column), evaluator).Trim();
            result.Add(values);
        }
        return result;
    }

    private static string SafeSheetName(string value)
    {
        var name = string.Concat(value.Select(character => "[]:*?/\\".Contains(character) ? ' ' : character)).Trim();
        if (name.Length == 0) name = "Sheet";
        return name[..Math.Min(name.Length, 31)];
    }
}
