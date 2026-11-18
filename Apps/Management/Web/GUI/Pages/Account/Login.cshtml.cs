using System.ComponentModel.DataAnnotations;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Account;

public sealed class LoginModel(ManagementWebApiClient api) : AdminAccountPageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty(SupportsGet = true)] public bool SessionExpired { get; set; }

    public void OnGet() => ViewData["Title"] = "Đăng nhập quản lý";

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Đăng nhập quản lý";
        if (!ModelState.IsValid) return Page();
        try
        {
            var account = await api.LoginAsync(Input.UserNameOrEmail.Trim(), Input.Password, ct)
                ?? throw new InvalidOperationException("Máy chủ không trả về thông tin đăng nhập.");
            if (!StaffPermissions.IsEmployeeRole(account.Role)) throw new InvalidOperationException("Tài khoản khách hàng không thể đăng nhập khu vực quản lý.");
            await SignInStaffAsync(account, Input.RememberMe);
            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)) return LocalRedirect(ReturnUrl);
            return LocalRedirect(StaffPermissions.HomePath(account.Role));
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); return Page(); }
    }

    public sealed class InputModel
    {
        [Required, Display(Name = "Tên đăng nhập hoặc email")] public string UserNameOrEmail { get; set; } = string.Empty;
        [Required, DataType(DataType.Password), Display(Name = "Mật khẩu")] public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
