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
    public async Task<IActionResult> CreateOrder(AdminCreateOrderRequest request, CancellationToken ct)
    {
        var command = new CreateAdminOrderDto(request.CustomerId, request.ShippingAddress, request.ReceiverPhone, request.PaymentMethod,
            request.Lines.Select(x => new CreateAdminOrderLineDto(x.BookId, x.Quantity)).ToArray());
        var result = await service.CreateAsync(command, GetAccountId(), ct);
        return result.Status switch
        {
            OperationStatus.Created => StatusCode(StatusCodes.Status201Created, result.Value),
            OperationStatus.Invalid => BadRequest(new { message = result.Message }),
            OperationStatus.Conflict => Conflict(new { message = result.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet]
    public Task<IReadOnlyList<ManagerOrderRowDto>> GetOrders([FromQuery] string? status, CancellationToken ct) => service.GetAsync(status, ct);

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(string id, UpdateAdminOrderRequest request, CancellationToken ct) =>
        ToHttpResult(await service.UpdateAsync(id, new UpdateAdminOrderDto(request.ShippingAddress, request.ReceiverPhone, request.PaymentMethod), ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsCancelled(string id, CancellationToken ct) => ToHttpResult(await service.UpdateStatusAsync(id, "Cancelled", GetAccountId(), ct));

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, UpdateOrderStatusRequest request, CancellationToken ct) =>
        ToHttpResult(await service.UpdateStatusAsync(id, request.Status, GetAccountId(), ct));

    [HttpPut("{id}/payment")]
    public async Task<IActionResult> MarkBankTransferPaid(string id, CancellationToken ct) => ToHttpResult(await service.MarkBankTransferPaidAsync(id, ct));

    [HttpPut("{id}/refund")]
    public async Task<IActionResult> MarkRefundCompleted(string id, CancellationToken ct) => ToHttpResult(await service.MarkRefundCompletedAsync(id, ct));

    private int? GetAccountId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private IActionResult ToHttpResult(OperationResult<Unit> result) => result.Status switch
    {
        OperationStatus.Success => NoContent(), OperationStatus.NotFound => NotFound(),
        OperationStatus.Invalid => BadRequest(new { message = result.Message }),
        OperationStatus.Conflict => Conflict(new { message = result.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}


