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
    public ManagerLookupsDto GetLookups() => service.GetLookups();

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpGet("suppliers")]
    public List<SupplierOptionDto> GetSuppliers() => service.GetSuppliers();

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPost("suppliers")]
    public IActionResult CreateSupplierDto(SupplierRequest request) =>
        ControllerExceptionHelper.Execute(() => service.CreateSupplier(new SupplierCommandDto(request.Name, request.PhoneNumber)), StatusCodes.Status201Created);

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPut("suppliers/{id}")]
    public IActionResult UpdateSupplier(string id, SupplierRequest request) =>
        ControllerExceptionHelper.Execute(() => service.UpdateSupplier(id, new SupplierCommandDto(request.Name, request.PhoneNumber)));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpDelete("suppliers/{id}")]
    public IActionResult DeleteSupplier(string id) =>
        ControllerExceptionHelper.Execute(() => service.DeleteSupplier(id));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpGet("receipts")]
    public List<ManagerReceiptDto> GetReceipts() => service.GetReceipts();

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPost("receipts")]
    public IActionResult CreateReceiptDto(PurchaseReceiptRequest request) =>
        ControllerExceptionHelper.Execute(() => service.CreateReceipt(ToCommand(request), int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null), StatusCodes.Status201Created);

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPut("receipts/{id}")]
    public IActionResult UpdateReceipt(string id, PurchaseReceiptRequest request) =>
        ControllerExceptionHelper.Execute(() => service.UpdateReceipt(id, ToCommand(request)));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpDelete("receipts/{id}")]
    [HttpPut("receipts/{id}/cancel")]
    public IActionResult CancelReceipt(string id) =>
        ControllerExceptionHelper.Execute(() => service.CancelReceipt(id));

    private static ReceiptCommandDto ToCommand(PurchaseReceiptRequest request) => new(request.SupplierId,
        request.Lines.Select(x => new ReceiptLineCommandDto(x.BookId, x.Quantity, x.UnitCost)).ToList(),
        request.EmployeeId);
}
