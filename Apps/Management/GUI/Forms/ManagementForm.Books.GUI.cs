using TrangSach.Shared.DTO;
using System.Globalization;

namespace TrangSach.Management.GUI;

public partial class ManagementForm
{
    private void EditBook(string? id)
    {
        ManagerBookDetailsDto? old = id is null ? null : _services.Manager.GetBookDetails(id);
        var lookups = GetLookups();
        var values = EditDialog.Show(this, id is null ? "Thêm sách" : "Sửa sách", [
            ("title", "Tên sách", old?.Title ?? ""), ("price", "Giá bán", old?.SalePrice.ToString(CultureInfo.InvariantCulture) ?? "0"),
            ("unit", "Đơn vị", old?.Unit ?? "Quyển"), ("category", "Mã danh mục (" + string.Join(", ", lookups.Categories.Select(x => x.Id)) + ")", old?.CategoryId ?? lookups.Categories.FirstOrDefault()?.Id ?? ""),
            ("publisher", "Mã NXB (để trống nếu không có)", old?.PublisherId?.ToString() ?? ""),
            ("authors", "Mã tác giả, cách nhau dấu phẩy", string.Join(",", old?.AuthorIds ?? [])),
            ("description", "Mô tả sách (tối đa 4.000 ký tự)", old?.Description ?? ""),
            ("AvailableStock", "Số lượng tồn kho", old?.AvailableStock.ToString() ?? "0")]);
        if (values is null) return;
        if (!decimal.TryParse(values["price"], NumberStyles.Number, CultureInfo.InvariantCulture, out var price)) throw new InvalidOperationException("Giá bán phải là số, nhập theo dạng 85000.");
        var authorIds = values["authors"].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToList();
        int? publisherId = int.TryParse(values["publisher"], out var pub) ? pub : null;
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
        if (rows.Count == 0) throw new InvalidDataException("File không có dữ liệu sách.");
        var result = MessageBox.Show(this, $"Đọc được {rows.Count} dòng. Thêm các sách này vào database?\nMã danh mục, nhà xuất bản và tác giả phải hợp lệ.", "Nhập CSV", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;
        foreach (var row in rows)
            _services.Manager.CreateBook(new CreateManagerBookDto(row.Title, row.SalePrice, row.Unit, row.CategoryId, row.PublisherId, row.AuthorIds));
        LoadBooks(true);
    }
}





