using System.ComponentModel.DataAnnotations;
using TrangSach.Management.Web.BUS;

namespace TrangSach.Management.Web.GUI.Pages.Account;

public sealed class SetupManagerModel(ManagementWebApiClient api) : AdminAccountPageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public void OnGet() => ViewData["Title"] = "Thiết lập quản lý đầu tiên";

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Thiết lập quản lý đầu tiên";
        if (!ModelState.IsValid) return Page();
        try
        {
            var account = await api.SetupFirstManagerAsync(Input.UserName.Trim(), Input.Email.Trim(), Input.Password, Input.FullName.Trim(), Input.PhoneNumber, ct)
                ?? throw new InvalidOperationException("API không trả về thông tin tài khoản.");
            await SignInStaffAsync(account, true);
            TempData["Notice"] = "Đã thiết lập tài khoản quản lý đầu tiên.";
            return RedirectToPage("/Admin/Orders/Index");
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); return Page(); }
    }

    public sealed class InputModel
    {
        [Required, StringLength(80, MinimumLength = 3)] public string UserName { get; set; } = "";
        [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
        [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)] public string Password { get; set; } = "";
        [Required, StringLength(180, MinimumLength = 2)] public string FullName { get; set; } = "";
        [Phone, StringLength(30)] public string? PhoneNumber { get; set; }
    }
}
