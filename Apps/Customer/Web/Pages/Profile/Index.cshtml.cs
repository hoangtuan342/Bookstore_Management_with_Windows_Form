using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Customer.BUS;
using TrangSach.Shared.DTO;

namespace TrangSach.Customer.Web.Pages.Profile;

[Authorize(Roles = "Customer")]
public sealed class IndexModel(CustomerWebApiClient api) : CustomerPageModel
{
    public AccountProfileDto? Profile { get; private set; }

    [BindProperty] public ProfileInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        Profile = await api.GetProfileAsync(RequireApiToken(), ct);
        if (Profile is null) return Unauthorized();
        ViewData["Title"] = "Hồ sơ cá nhân";

        Input.FullName = Profile.FullName;
        Input.PhoneNumber = Profile.PhoneNumber ?? string.Empty;
        Input.Address = Profile.Address ?? string.Empty;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Hồ sơ cá nhân";
        if (!ModelState.IsValid)
        {
            Profile = await api.GetProfileAsync(RequireApiToken(), ct);
            return Page();
        }

        try
        {
            Profile = await api.UpdateProfileAsync(
                RequireApiToken(),
                Input.FullName.Trim(),
                string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim(),
                string.IsNullOrWhiteSpace(Input.Address) ? null : Input.Address.Trim(),
                ct);

            TempData["Notice"] = "Cập nhật thông tin hồ sơ thành công!";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            Profile = await api.GetProfileAsync(RequireApiToken(), ct);
            return Page();
        }
    }

    public sealed class ProfileInput
    {
        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(180, MinimumLength = 2, ErrorMessage = "Họ và tên từ 2 đến 180 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [StringLength(30, ErrorMessage = "Số điện thoại tối đa 30 ký tự")]
        [Display(Name = "Số điện thoại nhận hàng")]
        public string? PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự")]
        [Display(Name = "Địa chỉ nhận hàng mặc định")]
        public string? Address { get; set; }
    }
}
