using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.Books;

[Authorize(Roles = StaffRoles.ManagerOrCatalog)]
public sealed class IndexModel(ManagementWebApiClient api, ManagementSpreadsheetService spreadsheets) : AdminPageModel
{
    public List<BookItemDto> Books { get; private set; } = [];
    public List<CategoryOptionDto> Categories { get; private set; } = [];
    public List<IdNameOptionDto> Authors { get; private set; } = [];
    public List<IdNameOptionDto> Publishers { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? AuthorFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? CategoryFilter { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MinPrice { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MaxPrice { get; set; }
    [BindProperty(SupportsGet = true)] public int? MinStock { get; set; }
    [BindProperty(SupportsGet = true)] public int? MaxStock { get; set; }
    [BindProperty(SupportsGet = true)] public string? Visibility { get; set; }
    [BindProperty] public BookInput Input { get; set; } = new();

    public async Task OnGetAsync(string? id, CancellationToken ct)
    {
        await LoadAsync(ct);
        if (!string.IsNullOrWhiteSpace(id))
        {
            var old = await api.GetBookForEditAsync(Token, id, ct);
            if (old is not null) Input = new BookInput { Id = old.Id, Title = old.Title, Description = old.Description, SalePrice = old.SalePrice, Unit = old.Unit, CategoryId = old.CategoryId, PublisherId = old.PublisherId?.ToString(CultureInfo.InvariantCulture) ?? "", AuthorIds = string.Join(',', old.AuthorIds), IsActive = old.IsActive };
        }
        ViewData["Title"] = "Quản lý sách";
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadAsync(ct); ViewData["Title"] = "Quản lý sách"; return Page(); }
        try
        {
            var authors = Input.AuthorIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(value => int.TryParse(value, out var id) ? id : throw new InvalidOperationException("Mã tác giả phải là số, cách nhau bằng dấu phẩy.")).ToList();
            int? publisher = int.TryParse(Input.PublisherId, out var publisherId) ? publisherId : null;
            var command = new BookManagementCommandDto(Input.Title.Trim(), Input.SalePrice, Input.Unit.Trim(), Input.CategoryId.Trim(), publisher, authors, Input.Description.Trim());
            if (string.IsNullOrWhiteSpace(Input.Id))
            {
                var created = await api.CreateBookAsync(Token, command, ct);
                if (created is not null && !Input.IsActive) await api.SetBookVisibilityAsync(Token, created.Id, false, ct);
            }
            else
            {
                await api.UpdateBookAsync(Token, Input.Id, command, ct);
                await api.SetBookVisibilityAsync(Token, Input.Id, Input.IsActive, ct);
            }
            TempData["Notice"] = "Đã lưu thông tin sách.";
            return RedirectToPage();
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadAsync(ct); ViewData["Title"] = "Quản lý sách"; return Page(); }
    }

    public async Task<IActionResult> OnPostHideAsync(string id, CancellationToken ct)
    {
        try { await api.HideBookAsync(Token, id, ct); TempData["Notice"] = "Đã ẩn sách khỏi cửa hàng."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) { TempData["Notice"] = "Chọn tệp CSV hoặc Excel cần nhập."; return RedirectToPage(); }
        try
        {
            var imported = new List<(string Title, decimal Price, string Unit, string Category, int? Publisher, int[] Authors, string Description)>();
            if (Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                var rows = spreadsheets.ReadFirstSheet(file.OpenReadStream());
                if (rows.Count < 2) throw new InvalidDataException("Tệp Excel chưa có dòng sách nào.");
                var columns = rows[0].Select((name, index) => (name, index)).Where(x => !string.IsNullOrWhiteSpace(x.name)).ToDictionary(x => x.name.Trim(), x => x.index, StringComparer.OrdinalIgnoreCase);
                int Col(string name) => columns.TryGetValue(name, out var index) ? index : throw new InvalidDataException($"Tệp Excel thiếu cột {name}.");
                var title = Col("Title"); var price = Col("SalePrice"); var unit = Col("Unit"); var category = Col("CategoryId"); var publisher = Col("PublisherId"); var authors = Col("AuthorIds");
                var description = columns.TryGetValue("Description", out var desc) ? desc : -1;
                string At(string[] row, int index) => index >= 0 && index < row.Length ? row[index].Trim() : "";
                foreach (var row in rows.Skip(1).Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value))))
                {
                    if (!decimal.TryParse(At(row, price), NumberStyles.Number, CultureInfo.InvariantCulture, out var salePrice)) throw new InvalidDataException("SalePrice trong Excel phải là số hợp lệ.");
                    var authorIds = At(row, authors).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(value => int.TryParse(value, out var authorId) ? authorId : throw new InvalidDataException("AuthorIds phải chứa các mã số, phân cách bằng dấu phẩy.")).ToArray();
                    int? publisherId = int.TryParse(At(row, publisher), out var parsedPublisher) ? parsedPublisher : null;
                    imported.Add((At(row, title), salePrice, At(row, unit), At(row, category), publisherId, authorIds, At(row, description)));
                }
            }
            else
            {
                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var rows = (await reader.ReadToEndAsync(ct)).Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(line => line.Split(';')).ToArray();
                foreach (var row in rows)
                {
                    if (row.Length < 6 || !decimal.TryParse(row[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var price)) throw new InvalidDataException("CSV cần Title;SalePrice;Unit;CategoryId;PublisherId;AuthorIds và giá hợp lệ.");
                    var authorIds = row[5].Split([',', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(value => int.TryParse(value, out var authorId) ? authorId : throw new InvalidDataException("AuthorIds trong CSV không hợp lệ.")).ToArray();
                    int? publisherId = int.TryParse(row[4], out var parsedPublisher) ? parsedPublisher : null;
                    imported.Add((row[0].Trim(), price, row[2].Trim(), row[3].Trim(), publisherId, authorIds, row.Length > 6 ? row[6].Trim() : ""));
                }
            }
            if (imported.Count == 0) throw new InvalidDataException("Tệp nhập không có dòng sách hợp lệ.");
            if (imported.Any(row => string.IsNullOrWhiteSpace(row.Title) || row.Price < 1 || string.IsNullOrWhiteSpace(row.Unit) || string.IsNullOrWhiteSpace(row.Category)))
                throw new InvalidDataException("Mỗi dòng cần tên sách, giá bán dương, đơn vị và mã thể loại.");
            var lookup = await api.GetLookupsAsync(Token, ct) ?? throw new InvalidOperationException("Không tải được danh mục để kiểm tra tệp nhập.");
            var categoryIds = lookup.Categories.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var authorIdsAvailable = lookup.Authors.Select(x => x.Id).ToHashSet();
            var publisherIds = lookup.Publishers.Select(x => x.Id).ToHashSet();
            foreach (var row in imported)
            {
                if (!categoryIds.Contains(row.Category)) throw new InvalidDataException($"Mã thể loại '{row.Category}' chưa có trong danh mục.");
                if (row.Publisher is int publisherId && !publisherIds.Contains(publisherId)) throw new InvalidDataException($"Mã nhà xuất bản {publisherId} chưa có trong danh mục.");
                if (row.Authors.Any(id => !authorIdsAvailable.Contains(id))) throw new InvalidDataException("Một mã tác giả trong tệp chưa có trong danh mục.");
            }
            foreach (var row in imported)
                await api.CreateBookAsync(Token, new BookManagementCommandDto(row.Title, row.Price, row.Unit, row.Category, row.Publisher, row.Authors.ToList(), row.Description), ct);
            TempData["Notice"] = $"Đã nhập {imported.Count} sách.";
        }
        catch (Exception ex) { TempData["Notice"] = "Nhập sách chưa hoàn tất: " + ex.Message; }
        return RedirectToPage();
    }

    public IActionResult OnGetTemplate() => File(spreadsheets.CreateWorkbook([("Books", (List<object?[]>)[
        ["Title", "SalePrice", "Unit", "CategoryId", "PublisherId", "AuthorIds", "Description"]])]), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "mau-nhap-sach.xlsx");

    public async Task<IActionResult> OnGetExportAsync(CancellationToken ct)
    {
        var rows = await api.GetBooksAsync(Token, ct);
        var builder = new StringBuilder("Title;SalePrice;Unit;CategoryId;PublisherId;AuthorIds;Description\r\n");
        foreach (var book in rows)
        {
            var details = await api.GetBookForEditAsync(Token, book.Id, ct);
            if (details is null) continue;
            builder.AppendLine(string.Join(';', details.Title.Replace(';', ','), details.SalePrice.ToString(CultureInfo.InvariantCulture), details.Unit.Replace(';', ','), details.CategoryId, details.PublisherId?.ToString(CultureInfo.InvariantCulture) ?? "", string.Join(',', details.AuthorIds), details.Description.Replace(';', ',')));
        }
        return File(new UTF8Encoding(true).GetBytes(builder.ToString()), "text/csv; charset=utf-8", "trang-sach.csv");
    }

    public async Task<IActionResult> OnGetExportExcelAsync(CancellationToken ct)
    {
        var books = await api.GetBooksAsync(Token, ct);
        var rows = new List<object?[]> { new object?[] { "Title", "SalePrice", "Unit", "CategoryId", "PublisherId", "AuthorIds", "Description", "IsActive" } };
        foreach (var book in books)
        {
            var details = await api.GetBookForEditAsync(Token, book.Id, ct);
            if (details is not null) rows.Add([details.Title, details.SalePrice, details.Unit, details.CategoryId, details.PublisherId, string.Join(',', details.AuthorIds), details.Description, details.IsActive]);
        }
        var data = spreadsheets.CreateWorkbook([("Books", (List<object?[]>)rows)]);
        return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "trang-sach.xlsx");
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var all = await api.GetBooksAsync(Token, ct);
        Books = all.Where(book => (string.IsNullOrWhiteSpace(Search) || book.Title.Contains(Search, StringComparison.CurrentCultureIgnoreCase)
            || book.Author.Contains(Search, StringComparison.CurrentCultureIgnoreCase) || book.Id.Contains(Search, StringComparison.CurrentCultureIgnoreCase)
            || book.Category.Contains(Search, StringComparison.CurrentCultureIgnoreCase))
            && (string.IsNullOrWhiteSpace(AuthorFilter) || book.Author.Contains(AuthorFilter, StringComparison.CurrentCultureIgnoreCase))
            && (string.IsNullOrWhiteSpace(CategoryFilter) || book.Category.Equals(CategoryFilter, StringComparison.CurrentCultureIgnoreCase))
            && (!MinPrice.HasValue || book.Price >= MinPrice.Value) && (!MaxPrice.HasValue || book.Price <= MaxPrice.Value)
            && (!MinStock.HasValue || book.AvailableStock >= MinStock.Value) && (!MaxStock.HasValue || book.AvailableStock <= MaxStock.Value)
            && (string.IsNullOrWhiteSpace(Visibility) || (Visibility == "active"
                ? !book.Label.Contains("ẨN", StringComparison.CurrentCultureIgnoreCase)
                : book.Label.Contains("ẨN", StringComparison.CurrentCultureIgnoreCase)))).ToList();
        var lookups = await api.GetLookupsAsync(Token, ct);
        Categories = lookups?.Categories ?? [];
        Authors = lookups?.Authors ?? [];
        Publishers = lookups?.Publishers ?? [];
    }

    public sealed class BookInput
    {
        public string? Id { get; set; }
        [Required, StringLength(250, MinimumLength = 1), Display(Name = "Tên sách")] public string Title { get; set; } = "";
        [StringLength(4000), Display(Name = "Mô tả")] public string Description { get; set; } = "";
        [Range(typeof(decimal), "1", "999999999"), Display(Name = "Giá bán")] public decimal SalePrice { get; set; }
        [Required, StringLength(30), Display(Name = "Đơn vị")] public string Unit { get; set; } = "Quyển";
        [Required, StringLength(20), Display(Name = "Mã thể loại")] public string CategoryId { get; set; } = "";
        public string? PublisherId { get; set; }
        public string AuthorIds { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }
}
