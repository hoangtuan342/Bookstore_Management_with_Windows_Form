using System.Security.Claims;
using BookStore.Api.Contracts;
using BookStore.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.BUS.Results;
using TrangSach.BUS.Services;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/cart")]
public sealed class CartController(CustomerCommerceService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var id = User.FindFirstValue("customer_id");
        return id is null ? Forbid() : Ok(await service.GetCartAsync(id, ct));
    }
    [HttpPut("items/{bookId}")]
    public async Task<IActionResult> SetQuantity(string bookId, SetCartQuantityRequest request, CancellationToken ct)
    {
        var id = User.FindFirstValue("customer_id"); if (id is null) return Forbid();
        var result = await service.SetCartQuantityAsync(id, bookId, request.Quantity, ct);
        return result.Status switch { OperationStatus.Success => NoContent(), OperationStatus.NotFound => NotFound(new { message = result.Message }),
            OperationStatus.Conflict => Conflict(new { message = result.Message }), _ => BadRequest(new { message = result.Message }) };
    }
    [HttpDelete("items/{bookId}")]
    public async Task<IActionResult> RemoveItem(string bookId, CancellationToken ct)
    {
        var id = User.FindFirstValue("customer_id"); if (id is null) return Forbid();
        await service.RemoveCartItemAsync(id, bookId, ct); return NoContent();
    }
}
