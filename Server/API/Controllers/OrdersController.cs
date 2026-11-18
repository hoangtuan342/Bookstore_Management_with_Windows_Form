using System.Security.Claims;
using TrangSach.API.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace TrangSach.API.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/orders")]
public sealed class OrdersController(CustomerCommerceService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyOrders(CancellationToken ct)
    {
        var id = User.FindFirstValue("customer_id"); return id is null ? Forbid() : Ok(await service.GetOrdersAsync(id, ct));
    }

    [HttpGet("{id}")]
    [ProducesResponseType<TrangSach.Shared.DTO.StoreOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetMyOrder(string id, CancellationToken ct)
    {
        var customerId = User.FindFirstValue("customer_id");
        if (customerId is null) return Forbid();
        var order = service.GetOrder(id, customerId, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    [ProducesResponseType<TrangSach.Shared.DTO.StoreOrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Checkout(CheckoutRequest request, CancellationToken ct)
    {
        var id = User.FindFirstValue("customer_id"); if (id is null) return Forbid();
        var result = await service.CheckoutAsync(id, new(request.ShippingAddress, request.ReceiverPhone, request.PaymentMethod, request.VoucherCode), ct);
        return result.Status switch { OperationStatus.Created => StatusCode(StatusCodes.Status201Created, result.Value),
            OperationStatus.Invalid => BadRequest(new { message = result.Message }), OperationStatus.Conflict => Conflict(new { message = result.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError) };
    }
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelMyOrder(string id, CancellationToken ct)
    {
        var customerId = User.FindFirstValue("customer_id"); if (customerId is null) return Forbid();
        var result = await service.CancelOrderAsync(id, customerId, ct);
        return result.Status switch { OperationStatus.Success => NoContent(), OperationStatus.NotFound => NotFound(),
            OperationStatus.Conflict => Conflict(new { message = result.Message }), _ => StatusCode(StatusCodes.Status500InternalServerError) };
    }
}


