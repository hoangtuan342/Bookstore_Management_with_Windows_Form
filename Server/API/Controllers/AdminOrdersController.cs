using System.Security.Claims;
using TrangSach.API.Contracts;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TrangSach.API.Controllers;

[ApiController]
[Authorize(Roles = StaffRoles.ManagerOrSales)]
[Route("api/admin/orders")]
public sealed class AdminOrdersController(AdminOrderService service) : ControllerBase
{
    [HttpPost]
    public IActionResult CreateOrder(AdminCreateOrderRequest request)
    {
        var command = new CreateAdminOrderDto(request.CustomerId, request.ShippingAddress, request.ReceiverPhone, request.PaymentMethod,
            request.Lines.Select(x => new CreateAdminOrderLineDto(x.BookId, x.Quantity)).ToList());
        return ControllerExceptionHelper.Execute(() => service.Create(command, GetAccountId()), StatusCodes.Status201Created);
    }

    [HttpGet]
    public List<ManagerOrderRowDto> GetOrders([FromQuery] string? status) => service.GetOrdersList(status);

    [HttpPut("{id}")]
    public IActionResult UpdateOrder(string id, UpdateAdminOrderRequest request) =>
        ControllerExceptionHelper.Execute(() => service.Update(id, new UpdateAdminOrderDto(request.ShippingAddress, request.ReceiverPhone, request.PaymentMethod)));

    [HttpDelete("{id}")]
    public IActionResult DeleteAsCancelled(string id) =>
        ControllerExceptionHelper.Execute(() => service.UpdateStatus(id, "Cancelled", GetAccountId()));

    [HttpPut("{id}/status")]
    public IActionResult UpdateStatus(string id, UpdateOrderStatusRequest request) =>
        ControllerExceptionHelper.Execute(() => service.UpdateStatus(id, request.Status, GetAccountId()));

    [HttpPut("{id}/payment")]
    public IActionResult MarkBankTransferPaid(string id) =>
        ControllerExceptionHelper.Execute(() => service.MarkBankTransferPaid(id));

    [HttpPut("{id}/refund")]
    public IActionResult MarkRefundCompleted(string id) =>
        ControllerExceptionHelper.Execute(() => service.MarkRefundCompleted(id));

    private int? GetAccountId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
