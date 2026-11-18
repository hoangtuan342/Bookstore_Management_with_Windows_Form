using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.Employees;

[Authorize(Roles = StaffRoles.Manager)]
public sealed class IndexModel(ManagementWebApiClient api) : AdminPageModel
{
    public List<ManagerEmployeeDto> Employees { get; private set; } = [];
    public List<StaffRoleOptionDto> Roles { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty] public EmployeeInput Input { get; set; } = new();
    public async Task OnGetAsync(string? id, CancellationToken ct)
    {
        await LoadAsync(ct);
        if (!string.IsNullOrWhiteSpace(id) && Employees.FirstOrDefault(x => x.Id == id) is { } old)
            Input = new() { Id = old.Id, LastName = old.LastName, FirstName = old.FirstName, BirthDate = old.BirthDate?.ToString("yyyy-MM-dd"), AvatarPath = old.AvatarPath, Address = old.Address, BaseSalary = old.BaseSalary, AccountStatus = old.AccountStatus, UserName = old.UserName, Email = old.Email, RoleName = old.RoleName ?? StaffRoles.Sales, LoginStatus = old.LoginStatus ?? "Active" };
        ViewData["Title"] = "Nhân viên";
    }
    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadAsync(ct); ViewData["Title"] = "Nhân viên"; return Page(); }
        try
        {
            DateOnly? birth = DateOnly.TryParse(Input.BirthDate, out var parsed) ? parsed : null;
            await api.SaveEmployeeAsync(Token, new SaveEmployeeDto(Empty(Input.Id), Input.LastName, Input.FirstName, birth, Empty(Input.AvatarPath), Empty(Input.Address), Input.AccountStatus, Input.BaseSalary, Empty(Input.UserName), Empty(Input.Email), Empty(Input.Password), Input.LoginStatus, Input.RoleName), ct);
            TempData["Notice"] = "Đã lưu hồ sơ nhân viên.";
            return RedirectToPage();
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadAsync(ct); ViewData["Title"] = "Nhân viên"; return Page(); }
    }
    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken ct)
    {
        try { await api.DeleteEmployeeAsync(Token, id, ct); TempData["Notice"] = "Đã xóa nhân viên."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }
    private async Task LoadAsync(CancellationToken ct)
    {
        var rows = await api.GetEmployeesAsync(Token, ct);
        Employees = rows.Where(x => string.IsNullOrWhiteSpace(Search) || string.Join(' ', x.Id, x.LastName, x.FirstName, x.Email, x.RoleName).Contains(Search, StringComparison.CurrentCultureIgnoreCase)).ToList();
        Roles = await api.GetEmployeeRolesAsync(Token, ct);
    }
    private static string? Empty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    public sealed class EmployeeInput
    {
        public string? Id { get; set; }
        [Required, StringLength(100), Display(Name = "Họ")]
        public string LastName { get; set; } = "";
        [Required, StringLength(60, MinimumLength = 1), Display(Name = "Tên")]
        public string FirstName { get; set; } = "";
        [Display(Name = "Ngày sinh (yyyy-MM-dd)")] public string? BirthDate { get; set; }
        [StringLength(500), Display(Name = "Ảnh chân dung (đường dẫn)")] public string? AvatarPath { get; set; }
        [StringLength(300), Display(Name = "Địa chỉ")] public string? Address { get; set; }
        [Range(typeof(decimal), "0", "999999999"), Display(Name = "Lương cơ bản")] public decimal BaseSalary { get; set; }
        [Required, StringLength(30), Display(Name = "Trạng thái nhân viên")] public string AccountStatus { get; set; } = "Active";
        [StringLength(100), Display(Name = "Tên đăng nhập")] public string? UserName { get; set; }
        [EmailAddress, StringLength(254), Display(Name = "Email")] public string? Email { get; set; }
        [StringLength(100), DataType(DataType.Password), Display(Name = "Mật khẩu (khi tạo tài khoản)")] public string? Password { get; set; }
        [StringLength(30), Display(Name = "Trạng thái đăng nhập")] public string? LoginStatus { get; set; } = "Active";
        [Required, Display(Name = "Vai trò")] public string RoleName { get; set; } = StaffRoles.Sales;
    }
}
