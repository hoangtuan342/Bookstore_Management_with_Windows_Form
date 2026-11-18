using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.Orders;

[Authorize(Roles = StaffRoles.ManagerOrSales)]
public sealed class IndexModel(ManagementWebApiClient api) : AdminPageModel
{
    private static readonly List<string> Statuses = ["Pending", "Confirmed", "Shipping", "Completed", "Cancelled"];
    public List<ManagerOrderRowDto> Orders { get; private set; } = [];
    public List<ManagerCustomerDto> Customers { get; private set; } = [];
    public List<BookItemDto> Books { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty] public OrderInput Input { get; set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
        ViewData["Title"] = "Đơn hàng";
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) { await LoadAsync(ct); ViewData["Title"] = "Đơn hàng"; return Page(); }
        try
        {
            var lines = Input.Lines.Where(line => !string.IsNullOrWhiteSpace(line.BookId)).ToArray();
            if (lines.Length == 0) throw new InvalidOperationException("Đơn hàng cần ít nhất một sản phẩm.");
            if (lines.Any(line => line.Quantity < 1 || line.Quantity > 99)) throw new InvalidOperationException("Số lượng mỗi sản phẩm phải từ 1 đến 99.");
            await api.CreateOrderAsync(Token, new CreateAdminOrderDto(Input.CustomerId, Input.ShippingAddress, Input.ReceiverPhone, Input.PaymentMethod,
                lines.Select(line => new CreateAdminOrderLineDto(line.BookId, line.Quantity)).ToList()), ct);
            TempData["Notice"] = "Đã tạo đơn hàng.";
            return RedirectToPage();
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); await LoadAsync(ct); ViewData["Title"] = "Đơn hàng"; return Page(); }
    }

    public async Task<IActionResult> OnPostAddLineAsync(CancellationToken ct)
    {
        Input.Lines.Add(new OrderLineInput());
        await LoadAsync(ct);
        ViewData["Title"] = "Đơn hàng";
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveLineAsync(int index, CancellationToken ct)
    {
        if (Input.Lines.Count > 1 && index >= 0 && index < Input.Lines.Count) Input.Lines.RemoveAt(index);
        await LoadAsync(ct);
        ViewData["Title"] = "Đơn hàng";
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(string id, string shippingAddress, string receiverPhone, string paymentMethod, CancellationToken ct)
    {
        try { await api.UpdateOrderAsync(Token, id, new UpdateAdminOrderDto(shippingAddress, receiverPhone, paymentMethod), ct); TempData["Notice"] = "Đã cập nhật thông tin giao hàng."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStatusAsync(string id, string orderStatus, CancellationToken ct)
    {
        try { await api.UpdateOrderStatusAsync(Token, id, orderStatus, ct); TempData["Notice"] = "Đã cập nhật trạng thái đơn."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(string id, CancellationToken ct)
    {
        try { await api.CancelOrderAsync(Token, id, ct); TempData["Notice"] = "Đã hủy đơn hàng."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkPaidAsync(string id, CancellationToken ct)
    {
        try { await api.MarkTransferPaidAsync(Token, id, ct); TempData["Notice"] = "Đã xác nhận thanh toán."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRefundAsync(string id, CancellationToken ct)
    {
        try { await api.MarkRefundCompletedAsync(Token, id, ct); TempData["Notice"] = "Đã cập nhật hoàn tiền."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var rows = await api.GetOrdersAsync(Token, Status, ct);
        Orders = rows.Where(x => string.IsNullOrWhiteSpace(Search) || string.Join(' ', x.Id, x.CustomerName, x.EmployeeName, x.ReceiverPhone, x.ShippingAddress).Contains(Search, StringComparison.CurrentCultureIgnoreCase)).ToList();
        Customers = await api.GetCustomersAsync(Token, ct);
        Books = await api.GetBooksAsync(Token, ct);
    }

    public static List<string> AllowedStatuses => Statuses;
    public sealed class OrderInput
    {
        [Required, StringLength(20), Display(Name = "Mã khách hàng")] public string CustomerId { get; set; } = "";
        [MinLength(1)] public List<OrderLineInput> Lines { get; set; } = [new()];
        [Required, StringLength(500, MinimumLength = 8), Display(Name = "Địa chỉ giao hàng")] public string ShippingAddress { get; set; } = "";
        [Required, Phone, StringLength(30), Display(Name = "Số điện thoại nhận hàng")] public string ReceiverPhone { get; set; } = "";
        [Required, Display(Name = "Thanh toán")] public string PaymentMethod { get; set; } = "CashOnDelivery";
    }

    public sealed class OrderLineInput
    {
        [Required] public string BookId { get; set; } = "";
        [Range(1, 99)] public int Quantity { get; set; } = 1;
    }
}
