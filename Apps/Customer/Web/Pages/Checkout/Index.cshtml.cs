using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Customer.BUS;
using TrangSach.Shared.DTO;

namespace TrangSach.Customer.Web.Pages.Checkout;

[Authorize(Roles = "Customer")]
public sealed class IndexModel(CustomerWebApiClient api) : CustomerPageModel
{
    [BindProperty] public CheckoutInput Input { get; set; } = new();
    public List<StoreCartLineDto> Lines { get; private set; } = [];
    public decimal Subtotal => Lines.Sum(line => line.LineTotal);
    public AccountProfileDto? CustomerProfile { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
        ViewData["Title"] = "Thanh toán";
        if (Lines.Count == 0) { TempData["Notice"] = "Giỏ hàng đang trống."; return RedirectToPage("/Cart/Index"); }

        try
        {
            CustomerProfile = await api.GetProfileAsync(RequireApiToken(), ct);
            if (CustomerProfile is not null)
            {
                if (string.IsNullOrWhiteSpace(Input.ReceiverPhone) && !string.IsNullOrWhiteSpace(CustomerProfile.PhoneNumber))
                {
                    Input.ReceiverPhone = CustomerProfile.PhoneNumber;
                }
                if (string.IsNullOrWhiteSpace(Input.ShippingAddress) && !string.IsNullOrWhiteSpace(CustomerProfile.Address))
                {
                    Input.ShippingAddress = CustomerProfile.Address;
                }
            }
        }
        catch { }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Thanh toán";
        if (Input.PaymentMethod is not ("CashOnDelivery" or "BankTransfer"))
            ModelState.AddModelError("Input.PaymentMethod", "Chọn phương thức thanh toán hợp lệ.");
        if (!ModelState.IsValid) { await LoadAsync(ct); return Page(); }
        try
        {
            var order = await api.CheckoutAsync(RequireApiToken(), new CheckoutDetailsDto(
                Input.ShippingAddress.Trim(), Input.ReceiverPhone.Trim(), Input.PaymentMethod,
                string.IsNullOrWhiteSpace(Input.VoucherCode) ? null : Input.VoucherCode.Trim()), ct);
            TempData["Notice"] = "Đặt hàng thành công.";
            return RedirectToPage("/Orders/Details", new { id = order.Id });
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadAsync(ct); return Page(); }
    }

    private async Task LoadAsync(CancellationToken ct) => Lines = await api.GetCartAsync(RequireApiToken(), ct);

    public sealed class CheckoutInput
    {
        [Required, StringLength(500, MinimumLength = 8), Display(Name = "Địa chỉ nhận hàng")]
        public string ShippingAddress { get; set; } = string.Empty;
        [Required, Phone, StringLength(30), Display(Name = "Số điện thoại người nhận")]
        public string ReceiverPhone { get; set; } = string.Empty;
        [Required, Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "CashOnDelivery";
        [StringLength(40), Display(Name = "Mã voucher")]
        public string? VoucherCode { get; set; }
    }
}
