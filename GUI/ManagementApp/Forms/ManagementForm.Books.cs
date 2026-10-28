using BookStore.Shared.Models;
using System.Globalization;

namespace TrangSach.Management.Forms;

public partial class ManagementForm
{
    private async Task EditBookAsync(string? id)
    {
        ManagerBookDetails? old = id is null ? null : await _services.Manager.GetBookDetailsAsync(id);
        var lookups = await _services.Manager.GetLookupsAsync();
        var values = EditDialog.Show(this, id is null ? "Thêm sách" : "Sửa sách", [
            ("title", "Tên sách", old?.Title ?? ""), ("price", "Giá bán", old?.SalePrice.ToString(CultureInfo.InvariantCulture) ?? "0"),
            ("unit", "Đơn vị", old?.Unit ?? "Quyển"), ("category", "Mã danh mục (" + string.Join(", ", lookups.Categories.Select(x => x.Id)) + ")", old?.CategoryId ?? lookups.Categories.FirstOrDefault()?.Id ?? ""),
            ("publisher", "Mã NXB (để trống nếu không có)", old?.PublisherId?.ToString() ?? ""),
            ("authors", "Mã tác giả, cách nhau dấu phẩy", string.Join(",", old?.AuthorIds ?? []))]);
        if (values is null) return;
        if (!decimal.TryParse(values["price"], NumberStyles.Number, CultureInfo.InvariantCulture, out var price)) throw new InvalidOperationException("Giá bán phải là số, nhập theo dạng 85000.");
        var authorIds = values["authors"].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToArray();
        int? publisherId = int.TryParse(values["publisher"], out var pub) ? pub : null;
        var book = new CreateManagerBook(values["title"], price, values["unit"], values["category"], publisherId, authorIds);
        if (id is null) await _services.Manager.CreateBookAsync(book); else await _services.Manager.UpdateBookAsync(id, book);
        await LoadBooksAsync();
    }

    private Task EditSelectedBookAsync()
    {
        var item = Selected<BookItem>(_booksGrid) ?? throw new InvalidOperationException("Chọn sách cần sửa.");
        return EditBookAsync(item.Id);
    }

    private async Task HideSelectedBookAsync()
    {
        var item = Selected<BookItem>(_booksGrid) ?? throw new InvalidOperationException("Chọn sách cần ẩn.");
        if (MessageBox.Show(this, $"Ẩn sách ‘{item.Title}’ khỏi cửa hàng?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        await _services.Manager.HideBookAsync(item.Id); await LoadBooksAsync();
    }

    private async Task ExportBooksAsync(DataGridView grid)
    {
        var books = grid.DataSource as IEnumerable<BookItem>;
        if (books is null) throw new InvalidOperationException("Tải danh sách sách trước khi xuất.");
        using var dialog = new SaveFileDialog { Filter = "CSV UTF-8 (*.csv)|*.csv", FileName = "sach-trang-sach.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var rows = new List<(BookItem Book, ManagerBookDetails Details)>();
        foreach (var book in books) rows.Add((book, await _services.Manager.GetBookDetailsAsync(book.Id)));
        _bookFiles.Export(rows, dialog.FileName);
        MessageBox.Show(this, "Đã ghi danh sách sách ra file CSV dạng văn bản.", "Xuất dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task ImportBooksAsync()
    {
        using var dialog = new OpenFileDialog { Filter = "CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var rows = _bookFiles.ReadForImport(dialog.FileName);
        if (rows.Count == 0) throw new InvalidDataException("File không có dữ liệu sách.");
        var result = MessageBox.Show(this, $"Đọc được {rows.Count} dòng. Thêm các sách này vào database?\nMã danh mục, nhà xuất bản và tác giả phải hợp lệ.", "Nhập CSV", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;
        foreach (var row in rows)
            await _services.Manager.CreateBookAsync(new CreateManagerBook(row.Title, row.SalePrice, row.Unit, row.CategoryId, row.PublisherId, row.AuthorIds));
        await LoadBooksAsync();
    }
}

