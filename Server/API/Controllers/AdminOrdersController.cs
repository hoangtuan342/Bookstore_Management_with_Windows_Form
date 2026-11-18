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
            request.Lines.Select(x => new CreateAdminOrderLineDto(x.BookId, x.Quantity)).ToArray());
        var result = service.Create(command, GetAccountId());
        return result.Status switch
        {
            OperationStatus.Created => StatusCode(StatusCodes.Status201Created, result.Value),
            OperationStatus.Invalid => BadRequest(new { message = result.Message }),
            OperationStatus.Conflict => Conflict(new { message = result.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet]
    public IReadOnlyList<ManagerOrderRowDto> GetOrders([FromQuery] string? status) => service.GetOrdersList(status);

    [HttpPut("{id}")]
    public IActionResult UpdateOrder(string id, UpdateAdminOrderRequest request) =>
        ToHttpResult(service.Update(id, new UpdateAdminOrderDto(request.ShippingAddress, request.ReceiverPhone, request.PaymentMethod)));

    [HttpDelete("{id}")]
    public IActionResult DeleteAsCancelled(string id) => ToHttpResult(service.UpdateStatus(id, "Cancelled", GetAccountId()));

    [HttpPut("{id}/status")]
    public IActionResult UpdateStatus(string id, UpdateOrderStatusRequest request) =>
        ToHttpResult(service.UpdateStatus(id, request.Status, GetAccountId()));

    [HttpPut("{id}/payment")]
    public IActionResult MarkBankTransferPaid(string id) => ToHttpResult(service.MarkBankTransferPaid(id));

    [HttpPut("{id}/refund")]
    public IActionResult MarkRefundCompleted(string id) => ToHttpResult(service.MarkRefundCompleted(id));

    private int? GetAccountId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private IActionResult ToHttpResult(OperationResult<Unit> result) => result.Status switch
    {
        OperationStatus.Success => NoContent(), OperationStatus.NotFound => NotFound(),
        OperationStatus.Invalid => BadRequest(new { message = result.Message }),
        OperationStatus.Conflict => Conflict(new { message = result.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}
