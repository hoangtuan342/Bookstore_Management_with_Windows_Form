using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.Customers;

[Authorize(Roles = StaffRoles.ManagerOrSales)]
public sealed class IndexModel(ManagementWebApiClient api) : AdminPageModel
{
    public IReadOnlyList<ManagerCustomerDto> Customers { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty] public CustomerInput Input { get; set; } = new();
    public async Task OnGetAsync(string? id, CancellationToken ct)
    {
        await LoadAsync(ct);
        if (!string.IsNullOrWhiteSpace(id) && Customers.FirstOrDefault(x => x.Id == id) is { } old)
            Input = new() { Id = old.Id, FullName = old.FullName, PhoneNumber = old.PhoneNumber, Address = old.Address, UserName = old.UserName, Email = old.Email, AccountStatus = old.AccountStatus ?? "Active" };
        ViewData["Title"] = "Khách hàng";
    }
    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadAsync(ct); ViewData["Title"] = "Khách hàng"; return Page(); }
        try
        {
            await api.SaveCustomerAsync(Token, new SaveCustomerDto(Empty(Input.Id), Input.FullName.Trim(), Empty(Input.PhoneNumber), Empty(Input.Address), Empty(Input.UserName), Empty(Input.Email), Empty(Input.Password), Input.AccountStatus), ct);
            TempData["Notice"] = "Đã lưu thông tin khách hàng.";
            return RedirectToPage();
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadAsync(ct); ViewData["Title"] = "Khách hàng"; return Page(); }
    }
    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken ct)
    {
        try { await api.DeleteCustomerAsync(Token, id, ct); TempData["Notice"] = "Đã xóa khách hàng."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }
    private async Task LoadAsync(CancellationToken ct)
    {
        var rows = await api.GetCustomersAsync(Token, ct);
        Customers = rows.Where(x => string.IsNullOrWhiteSpace(Search) || string.Join(' ', x.Id, x.FullName, x.PhoneNumber, x.Email, x.Address).Contains(Search, StringComparison.CurrentCultureIgnoreCase)).ToArray();
    }
    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public sealed class CustomerInput
    {
        public string? Id { get; set; }
        [Required, StringLength(180, MinimumLength = 2), Display(Name = "Họ tên")] public string FullName { get; set; } = "";
        [Phone, StringLength(30), Display(Name = "Điện thoại")] public string? PhoneNumber { get; set; }
        [StringLength(500), Display(Name = "Địa chỉ")] public string? Address { get; set; }
        [StringLength(100), Display(Name = "Tên đăng nhập (tùy chọn)")] public string? UserName { get; set; }
        [EmailAddress, StringLength(254), Display(Name = "Email")] public string? Email { get; set; }
        [StringLength(100), DataType(DataType.Password), Display(Name = "Mật khẩu mới")] public string? Password { get; set; }
        [Required, StringLength(30), Display(Name = "Trạng thái tài khoản")] public string AccountStatus { get; set; } = "Active";
    }
}
