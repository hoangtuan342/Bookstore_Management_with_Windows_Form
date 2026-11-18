using System.Security.Claims;
using TrangSach.API.Contracts;
using TrangSach.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Shared.DTO;
namespace TrangSach.API.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public sealed class AdminInventoryController(AdminInventoryService service) : ControllerBase
{
    [Authorize(Roles = StaffRoles.ManagerCatalogInventory)]
    [HttpGet("lookups")]
    public TrangSach.Shared.DTO.ManagerLookupsDto GetLookups() => service.GetLookups();

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpGet("suppliers")]
    public IReadOnlyList<TrangSach.Shared.DTO.SupplierOptionDto> GetSuppliers() => service.GetSuppliers();

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPost("suppliers")]
    public IActionResult CreateSupplierDto(SupplierRequest request) =>
        ToHttpResult(service.CreateSupplier(new SupplierCommandDto(request.Name, request.PhoneNumber)));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPut("suppliers/{id}")]
    public IActionResult UpdateSupplier(string id, SupplierRequest request) =>
        ToHttpResult(service.UpdateSupplier(id, new SupplierCommandDto(request.Name, request.PhoneNumber)));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpDelete("suppliers/{id}")]
    public IActionResult DeleteSupplier(string id) => ToHttpResult(service.DeleteSupplier(id));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpGet("receipts")]
    public IReadOnlyList<TrangSach.Shared.DTO.ManagerReceiptDto> GetReceipts() => service.GetReceipts();

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPost("receipts")]
    public IActionResult CreateReceiptDto(PurchaseReceiptRequest request) =>
        ToHttpResult(service.CreateReceipt(ToCommand(request), int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPut("receipts/{id}")]
    public IActionResult UpdateReceipt(string id, PurchaseReceiptRequest request) =>
        ToHttpResult(service.UpdateReceipt(id, ToCommand(request)));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpDelete("receipts/{id}")]
    [HttpPut("receipts/{id}/cancel")]
    public IActionResult CancelReceipt(string id) => ToHttpResult(service.CancelReceipt(id));

    private static ReceiptCommandDto ToCommand(PurchaseReceiptRequest request) => new(request.SupplierId,
        request.Lines.Select(x => new ReceiptLineCommandDto(x.BookId, x.Quantity, x.UnitCost)).ToArray());

    private IActionResult ToHttpResult<T>(OperationResult<T> result) => result.Status switch
    {
        OperationStatus.Success => result.Value is Unit ? NoContent() : Ok(result.Value),
        OperationStatus.Created => StatusCode(StatusCodes.Status201Created, result.Value),
        OperationStatus.NotFound => NotFound(),
        OperationStatus.Invalid => BadRequest(new { message = result.Message }),
        OperationStatus.Conflict => Conflict(new { message = result.Message }),
        OperationStatus.Forbidden => Forbid(),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}
