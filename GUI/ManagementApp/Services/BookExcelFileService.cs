using System.Globalization;
using BookStore.Shared.Models;

namespace TrangSach.Management.Services;

internal sealed class BookExcelFileService(ExcelWorkbookService excel)
{
    public BookExcelFileService() : this(new ExcelWorkbookService()) { }

    public void Export(IReadOnlyList<(BookItem Book, ManagerBookDetails Details)> books, string path)
    {
        var rows = new List<object?[]> { new object?[] { "Title", "SalePrice", "Unit", "CategoryId", "PublisherId", "AuthorIds" } };
        rows.AddRange(books.Select(row => new object?[]
        {
            row.Details.Title, row.Details.SalePrice, row.Details.Unit, row.Details.CategoryId,
            row.Details.PublisherId, string.Join(',', row.Details.AuthorIds)
        }));
        excel.Write(path, [new ExcelSheetData("Books", rows)]);
    }

    public IReadOnlyList<BookCsvRow> Read(string path)
    {
        var rows = excel.ReadFirstSheet(path);
        if (rows.Count < 2) return [];
        var columns = rows[0].Select((header, index) => (header, index))
            .Where(item => !string.IsNullOrWhiteSpace(item.header))
            .ToDictionary(item => item.header, item => item.index, StringComparer.OrdinalIgnoreCase);
        var title = Column(columns, "Title");
        var price = Column(columns, "SalePrice");
        var unit = Column(columns, "Unit");
        var category = Column(columns, "CategoryId");
        var publisher = Column(columns, "PublisherId");
        var authors = Column(columns, "AuthorIds");
        var result = new List<BookCsvRow>();
        foreach (var row in rows.Skip(1))
        {
            string At(int index) => index < row.Length ? row[index].Trim() : string.Empty;
            var bookTitle = At(title);
            var bookUnit = At(unit);
            var bookCategory = At(category);
            if (bookTitle.Length == 0 && bookUnit.Length == 0 && bookCategory.Length == 0) continue;
            if (bookTitle.Length == 0 || bookUnit.Length == 0 || bookCategory.Length == 0
                || !decimal.TryParse(At(price), NumberStyles.Number, CultureInfo.InvariantCulture, out var salePrice))
                throw new InvalidDataException("Mỗi dòng cần Title, SalePrice, Unit, CategoryId, PublisherId, AuthorIds; SalePrice phải là số.");
            int? publisherId = int.TryParse(At(publisher), out var parsedPublisher) ? parsedPublisher : null;
            var authorIds = At(authors).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.TryParse(value, out var id) ? id : throw new InvalidDataException("AuthorIds phải là danh sách mã số, cách nhau bằng dấu phẩy."))
                .ToArray();
            result.Add(new BookCsvRow(bookTitle, salePrice, bookUnit, bookCategory, publisherId, authorIds));
        }
        return result;
    }

    private static int Column(IReadOnlyDictionary<string, int> columns, string name) =>
        columns.TryGetValue(name, out var index) ? index : throw new InvalidDataException($"Thiếu cột '{name}' trong Excel.");
}
