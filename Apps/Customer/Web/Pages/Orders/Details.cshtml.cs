using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Customer.BUS;
using TrangSach.Shared.DTO;

namespace TrangSach.Customer.Web.Pages.Orders;

[Authorize(Roles = "Customer")]
public sealed class DetailsModel(CustomerWebApiClient api) : CustomerPageModel
{
    public StoreOrderDto? Order { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken ct)
    {
        Order = await api.GetOrderAsync(RequireApiToken(), id, ct);
        if (Order is null) return NotFound();
        ViewData["Title"] = "Chi tiết đơn hàng";
        return Page();
    }
}
