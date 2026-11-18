using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Customer.BUS;
using TrangSach.Shared.DTO;

namespace TrangSach.Customer.Web.Pages.Orders;

[Authorize(Roles = "Customer")]
public sealed class IndexModel(CustomerWebApiClient api) : CustomerPageModel
{
    public IReadOnlyList<StoreOrderDto> Orders { get; private set; } = [];

    public async Task<IActionResult> OnPostCancelAsync(string id, CancellationToken ct)
    {
        try
        {
            await api.CancelOrderAsync(RequireApiToken(), id, ct);
            TempData["Notice"] = $"Đã hủy đơn {id}. Tồn kho và ưu đãi sẽ được xử lý theo trạng thái thanh toán.";
        }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Orders = await api.GetOrdersAsync(RequireApiToken(), ct);
        ViewData["Title"] = "Đơn hàng của tôi";
    }
}
