using TrangSach.Shared.DTO;
using TrangSach.Shared.GUI;
using System.Globalization;

namespace TrangSach.Management.GUI;

public partial class ManagementForm
{
    private void EditBook(string? id)
    {
        ManagerBookDetailsDto? old = id is null ? null : _services.Manager.GetBookDetails(id);
        var lookups = GetLookups();

        var categoryOptions = lookups.Categories.Select(c => (c.Id, $"{c.Name} ({c.Id})")).ToList();
        var publisherOptions = new List<(string Id, string Name)> { ("", "-- Không chọn --") };
        publisherOptions.AddRange(lookups.Publishers.Select(p => (p.Id.ToString(), p.Name)));

        var authorHint = string.Join(", ", lookups.Authors.Take(5).Select(a => $"{a.Id}: {a.Name}"));

        var fields = new List<EditDialogField>
        {
            new("title", "Tên sách", old?.Title ?? ""),
            new("price", "Giá bán (VNĐ)", old?.SalePrice.ToString(CultureInfo.InvariantCulture) ?? "0"),
            new("unit", "Đơn vị tính", old?.Unit ?? "Quyển"),
            new("category", "Thể loại", old?.CategoryId ?? lookups.Categories.FirstOrDefault()?.Id ?? "", categoryOptions),
            new("publisher", "Nhà xuất bản", old?.PublisherId?.ToString() ?? "", publisherOptions),
            new("authors", $"Mã tác giả ({authorHint}...)", string.Join(",", old?.AuthorIds ?? [])),
            new("description", "Mô tả sách", old?.Description ?? "")
        };

        if (old != null)
        {
            fields.Add(new EditDialogField("stock", "Tồn kho hiện tại", old.AvailableStock.ToString(), null, isReadOnly: true));
        }

        var values = EditDialog.Show(this, id is null ? "Thêm sách mới" : "Sửa thông tin sách", fields);
        if (values is null) return;

        if (!decimal.TryParse(values["price"], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0)
            throw new InvalidOperationException("Giá bán phải là số dương lớn hơn 0.");

        var authorIds = values["authors"].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var aId) ? aId : throw new InvalidOperationException($"Mã tác giả '{s}' phải là số nguyên."))
            .ToList();

        int? publisherId = int.TryParse(values["publisher"], out var pub) && pub > 0 ? pub : null;
        var book = new CreateManagerBookDto(values["title"], price, values["unit"], values["category"], publisherId, authorIds, values["description"]);
        if (id is null) _services.Manager.CreateBook(book); else _services.Manager.UpdateBook(id, book);
        LoadBooks(true);
    }

    private void EditSelectedBook()
    {
        var item = Selected<BookItemDto>(_booksGrid) ?? throw new InvalidOperationException("Chọn sách cần sửa.");
        EditBook(item.Id);
    }

    private void HideSelectedBook()
    {
        var item = Selected<BookItemDto>(_booksGrid) ?? throw new InvalidOperationException("Chọn sách cần ẩn.");
        if (MessageBox.Show(this, $"Ẩn sách ‘{item.Title}’ khỏi cửa hàng?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _services.Manager.HideBook(item.Id); LoadBooks(true);
    }

    private void ExportBooks(DataGridView grid)
    {
        var books = grid.DataSource as IEnumerable<BookItemDto>;
        if (books is null) throw new InvalidOperationException("Tải danh sách sách trước khi xuất.");
        using var dialog = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx|Excel 97-2003 (*.xls)|*.xls", FileName = "sach-trang-sach.xlsx" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var rows = new List<(BookItemDto Book, ManagerBookDetailsDto Details)>();
        foreach (var book in books) rows.Add((book, _services.Manager.GetBookDetails(book.Id)));
        _bookExcelFiles.Export(rows, dialog.FileName);
        MessageBox.Show(this, "Đã ghi danh sách sách ra file Excel.", "Xuất dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ImportBooks()
    {
        using var dialog = new OpenFileDialog { Filter = "Excel Workbook (*.xlsx;*.xls)|*.xlsx;*.xls|CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var extension = Path.GetExtension(dialog.FileName);
        var rows = extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) || extension.Equals(".xls", StringComparison.OrdinalIgnoreCase)
            ? _bookExcelFiles.Read(dialog.FileName)
            : _bookFiles.ReadForImport(dialog.FileName);
        if (rows.Count == 0) throw new InvalidOperationException("Tệp không có dữ liệu hợp lệ.");
        var result = MessageBox.Show(this, $"Đọc được {rows.Count} dòng sách từ tệp. Thêm các sách này vào hệ thống?", "Nhập dữ liệu", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;
        foreach (var row in rows)
        {
            _services.Manager.CreateBook(new CreateManagerBookDto(row.Title, row.SalePrice, row.Unit, row.CategoryId, row.PublisherId, row.AuthorIds));
        }
        LoadBooks(true);
        MessageBox.Show(this, $"Đã nhập thành công {rows.Count} cuốn sách.", "Nhập dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
