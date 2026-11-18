using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.Promotions;

[Authorize(Roles = StaffRoles.ManagerOrMarketing)]
public sealed class IndexModel(ManagementWebApiClient api) : AdminPageModel
{
    public IReadOnlyList<ManagerPromotionDto> Promotions { get; private set; } = [];
    [BindProperty] public PromotionInput Input { get; set; } = new();
    public async Task OnGetAsync(CancellationToken ct) { Promotions = await api.GetPromotionsAsync(Token, ct); ViewData["Title"] = "Khuyến mãi"; }
    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { Promotions = await api.GetPromotionsAsync(Token, ct); ViewData["Title"] = "Khuyến mãi"; return Page(); }
        try
        {
            await api.CreatePromotionAsync(Token, new CreatePromotionDto(Input.Name, Input.StartsAt, Input.EndsAt, Empty(Input.BookId), Input.BookDiscountPercent, Input.MinimumOrderAmount, Input.OrderDiscountPercent, Empty(Input.VoucherCode), Input.VoucherDiscountPercent, Input.VoucherMaxUses), ct);
            TempData["Notice"] = "Đã tạo chương trình khuyến mãi.";
            return RedirectToPage();
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); Promotions = await api.GetPromotionsAsync(Token, ct); ViewData["Title"] = "Khuyến mãi"; return Page(); }
    }
    public async Task<IActionResult> OnPostUpdateAsync(string id, string name, DateTime startsAt, DateTime endsAt, CancellationToken ct)
    {
        try { await api.UpdatePromotionAsync(Token, id, new UpdatePromotionDetailsDto(name, startsAt, endsAt), ct); TempData["Notice"] = "Đã cập nhật chương trình."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostRulesAsync(string id, string? books, string? orders, string? vouchers, CancellationToken ct)
    {
        try
        {
            var bookRules = ParseRules(books, 2, (f) => new PromotionBookRuleDto(f[0], decimal.Parse(f[1], CultureInfo.InvariantCulture)));
            var orderRules = ParseRules(orders, 2, (f) => new PromotionOrderRuleDto(decimal.Parse(f[0], CultureInfo.InvariantCulture), decimal.Parse(f[1], CultureInfo.InvariantCulture)));
            var voucherRules = ParseRules(vouchers, 2, (f) => new PromotionVoucherRuleDto(f[0], decimal.Parse(f[1], CultureInfo.InvariantCulture)));
            await api.UpdatePromotionRulesAsync(Token, id, new UpdatePromotionRulesDto(bookRules, orderRules, voucherRules), ct);
            TempData["Notice"] = "Đã cập nhật quy tắc sách/đơn/voucher.";
        }
        catch (Exception ex) { TempData["Notice"] = "Không thể cập nhật quy tắc: " + ex.Message; }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken ct)
    {
        try { await api.DeletePromotionAsync(Token, id, ct); TempData["Notice"] = "Đã xóa chương trình khuyến mãi."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }
    private static T[] ParseRules<T>(string? text, int fieldCount, Func<string[], T> factory) => (text ?? "").Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries).Select(line =>
    {
        var fields = line.Split(';', StringSplitOptions.TrimEntries);
        if (fields.Length != fieldCount) throw new InvalidOperationException("Mỗi quy tắc cần đúng 2 trường, phân cách bằng dấu chấm phẩy.");
        return factory(fields);
    }).ToArray();
    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public sealed class PromotionInput
    {
        [Required, StringLength(180, MinimumLength = 2), Display(Name = "Tên chương trình")] public string Name { get; set; } = "";
        [Required, Display(Name = "Bắt đầu")] public DateTime StartsAt { get; set; } = DateTime.Today;
        [Required, Display(Name = "Kết thúc")] public DateTime EndsAt { get; set; } = DateTime.Today.AddDays(7);
        [Display(Name = "Mã sách (không bắt buộc)")] public string? BookId { get; set; }
        [Range(typeof(decimal), "0", "90"), Display(Name = "% giảm sách")] public decimal? BookDiscountPercent { get; set; }
        [Range(typeof(decimal), "0", "999999999"), Display(Name = "Đơn tối thiểu")] public decimal? MinimumOrderAmount { get; set; }
        [Range(typeof(decimal), "0", "90"), Display(Name = "% giảm đơn")] public decimal? OrderDiscountPercent { get; set; }
        [StringLength(40), Display(Name = "Mã voucher (không bắt buộc)")] public string? VoucherCode { get; set; }
        [Range(typeof(decimal), "0", "90"), Display(Name = "% giảm voucher")] public decimal? VoucherDiscountPercent { get; set; }
        [Range(1, 1000000), Display(Name = "Số lượt voucher tối đa")] public int? VoucherMaxUses { get; set; }
    }
}
