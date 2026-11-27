using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Customer.BUS;
using TrangSach.Shared.DTO;

namespace TrangSach.Customer.Web.Pages.Orders;

public sealed class DetailsModel(CustomerWebApiClient api) : CustomerPageModel
{
    public StoreOrderDto? Order { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken ct)
    {
        var token = await EnsureCustomerTokenAsync(api, ct);
        Order = await api.GetOrderAsync(token, id, ct);
        if (Order is null) return NotFound();
        ViewData["Title"] = "Chi tiết hóa đơn";
        return Page();
    }
}
