using System.Security.Claims;
using TrangSach.API.Contracts;
using TrangSach.Shared.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace TrangSach.API.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/cart")]
public sealed class CartController(CustomerCommerceService service) : ControllerBase
{
    [HttpGet]
    public IActionResult GetCart()
    {
        var id = User.FindFirstValue("customer_id");
        return id is null ? Forbid() : Ok(service.GetCart(id));
    }
    [HttpPut("items/{bookId}")]
    public IActionResult SetQuantity(string bookId, SetCartQuantityRequest request)
    {
        var id = User.FindFirstValue("customer_id"); if (id is null) return Forbid();
        var result = service.SetCartQuantity(id, bookId, request.Quantity);
        return result.Status switch { OperationStatus.Success => NoContent(), OperationStatus.NotFound => NotFound(new { message = result.Message }),
            OperationStatus.Conflict => Conflict(new { message = result.Message }), _ => BadRequest(new { message = result.Message }) };
    }
    [HttpDelete("items/{bookId}")]
    public IActionResult RemoveItem(string bookId)
    {
        var id = User.FindFirstValue("customer_id"); if (id is null) return Forbid();
        service.RemoveCartItem(id, bookId); return NoContent();
    }
}
