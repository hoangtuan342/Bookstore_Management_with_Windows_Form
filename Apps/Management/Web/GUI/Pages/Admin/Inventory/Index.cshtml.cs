using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.Inventory;

[Authorize(Roles = StaffRoles.ManagerOrInventory)]
public sealed class IndexModel(ManagementWebApiClient api) : AdminPageModel
{
    public List<ManagerReceiptDto> Receipts { get; private set; } = [];
    public List<SupplierOptionDto> Suppliers { get; private set; } = [];
    public List<BookItemDto> Books { get; private set; } = [];
    [BindProperty] public ReceiptInput Input { get; set; } = new();
    public async Task OnGetAsync(string? id, CancellationToken ct)
    {
        await LoadAsync(ct);
        if (!string.IsNullOrWhiteSpace(id))
        {
            var receipt = Receipts.FirstOrDefault(item => item.Id == id && item.Status == "Posted");
            if (receipt is not null) Input = new ReceiptInput { Id = receipt.Id, SupplierId = receipt.SupplierId, Lines = receipt.Lines.Select(line => new ReceiptLineInput { BookId = line.BookId, Quantity = line.Quantity, UnitCost = line.UnitCost }).ToList() };
            else TempData["Notice"] = "Chỉ có thể sửa phiếu nhập đã ghi nhận.";
        }
        ViewData["Title"] = "Nhập kho";
    }
    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadAsync(ct); ViewData["Title"] = "Nhập kho"; return Page(); }
        try
        {
            var lines = Input.Lines.Where(line => !string.IsNullOrWhiteSpace(line.BookId)).ToArray();
            if (lines.Length == 0) throw new InvalidOperationException("Phiếu nhập cần ít nhất một mặt hàng.");
            if (lines.Any(line => line.Quantity <= 0 || line.UnitCost <= 0)) throw new InvalidOperationException("Số lượng và giá nhập mỗi dòng phải lớn hơn 0.");
            var command = new CreateReceiptDto(Input.SupplierId, lines.Select(line => new CreateReceiptLineDto(line.BookId, line.Quantity, line.UnitCost)).ToList());
            if (string.IsNullOrWhiteSpace(Input.Id)) await api.CreateReceiptAsync(Token, command, ct);
            else await api.UpdateReceiptAsync(Token, Input.Id, command, ct);
            TempData["Notice"] = string.IsNullOrWhiteSpace(Input.Id) ? "Đã tạo phiếu nhập và cập nhật tồn kho." : "Đã cập nhật phiếu nhập và điều chỉnh tồn kho.";
            return RedirectToPage();
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadAsync(ct); ViewData["Title"] = "Nhập kho"; return Page(); }
    }
    public async Task<IActionResult> OnPostAddLineAsync(CancellationToken ct) { Input.Lines.Add(new ReceiptLineInput()); await LoadAsync(ct); ViewData["Title"] = "Nhập kho"; return Page(); }
    public async Task<IActionResult> OnPostRemoveLineAsync(int index, CancellationToken ct) { if (Input.Lines.Count > 1 && index >= 0 && index < Input.Lines.Count) Input.Lines.RemoveAt(index); await LoadAsync(ct); ViewData["Title"] = "Nhập kho"; return Page(); }
    public async Task<IActionResult> OnPostCancelAsync(string id, CancellationToken ct)
    {
        try { await api.CancelReceiptAsync(Token, id, ct); TempData["Notice"] = "Đã đảo phiếu nhập."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }
    private async Task LoadAsync(CancellationToken ct)
    {
        Receipts = await api.GetReceiptsAsync(Token, ct);
        Suppliers = await api.GetSuppliersAsync(Token, ct);
        Books = await api.GetBooksAsync(Token, ct);
    }
    public sealed class ReceiptInput
    {
        public string? Id { get; set; }
        [Required, Display(Name = "Nhà cung cấp")] public string SupplierId { get; set; } = "";
        [MinLength(1)] public List<ReceiptLineInput> Lines { get; set; } = [new()];
    }

    public sealed class ReceiptLineInput
    {
        [Required, Display(Name = "Sách")] public string BookId { get; set; } = "";
        [Range(1, 1000000), Display(Name = "Số lượng nhập")] public int Quantity { get; set; } = 1;
        [Range(typeof(decimal), "0.01", "999999999"), Display(Name = "Giá nhập mỗi cuốn")] public decimal UnitCost { get; set; }
    }
}
