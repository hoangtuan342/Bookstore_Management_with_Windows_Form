using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.Suppliers;

[Authorize(Roles = StaffRoles.ManagerOrInventory)]
public sealed class IndexModel(ManagementWebApiClient api) : AdminPageModel
{
    public List<SupplierOptionDto> Suppliers { get; private set; } = [];
    [BindProperty] public SupplierInput Input { get; set; } = new();
    public async Task OnGetAsync(string? id, CancellationToken ct)
    {
        Suppliers = await api.GetSuppliersAsync(Token, ct);
        if (!string.IsNullOrWhiteSpace(id) && Suppliers.FirstOrDefault(s => s.Id == id) is { } old) Input = new() { Id = old.Id, Name = old.Name, PhoneNumber = old.PhoneNumber };
        ViewData["Title"] = "Nhà cung cấp";
    }
    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { Suppliers = await api.GetSuppliersAsync(Token, ct); ViewData["Title"] = "Nhà cung cấp"; return Page(); }
        try { await api.SaveSupplierAsync(Token, Input.Id, new CreateSupplierDto(Input.Name.Trim(), Input.PhoneNumber), ct); TempData["Notice"] = "Đã lưu nhà cung cấp."; return RedirectToPage(); }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); Suppliers = await api.GetSuppliersAsync(Token, ct); ViewData["Title"] = "Nhà cung cấp"; return Page(); }
    }
    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken ct)
    {
        try { await api.DeleteSupplierAsync(Token, id, ct); TempData["Notice"] = "Đã xóa nhà cung cấp."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }
    public sealed class SupplierInput
    {
        public string? Id { get; set; }
        [Required, StringLength(180, MinimumLength = 2), Display(Name = "Tên nhà cung cấp")] public string Name { get; set; } = "";
        [Phone, StringLength(30), Display(Name = "Điện thoại")] public string? PhoneNumber { get; set; }
    }
}
