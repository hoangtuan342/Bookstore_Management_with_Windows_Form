using System.Security.Claims;
using BookStore.Api.Contracts;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.BUS.Models;
using TrangSach.BUS.Results;
using TrangSach.BUS.Services;

namespace BookStore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public sealed class AdminInventoryController(AdminInventoryService service) : ControllerBase
{
    [Authorize(Roles = StaffRoles.ManagerCatalogInventory)]
    [HttpGet("lookups")]
    public Task<BookStore.Shared.Models.ManagerLookups> GetLookups(CancellationToken ct) => service.GetLookupsAsync(ct);

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpGet("suppliers")]
    public Task<IReadOnlyList<BookStore.Shared.Models.SupplierOption>> GetSuppliers(CancellationToken ct) => service.GetSuppliersAsync(ct);

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier(SupplierRequest request, CancellationToken ct) =>
        ToHttpResult(await service.CreateSupplierAsync(new SupplierCommand(request.Name, request.PhoneNumber), ct));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPut("suppliers/{id}")]
    public async Task<IActionResult> UpdateSupplier(string id, SupplierRequest request, CancellationToken ct) =>
        ToHttpResult(await service.UpdateSupplierAsync(id, new SupplierCommand(request.Name, request.PhoneNumber), ct));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpDelete("suppliers/{id}")]
    public async Task<IActionResult> DeleteSupplier(string id, CancellationToken ct) => ToHttpResult(await service.DeleteSupplierAsync(id, ct));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpGet("receipts")]
    public Task<IReadOnlyList<BookStore.Shared.Models.ManagerReceipt>> GetReceipts(CancellationToken ct) => service.GetReceiptsAsync(ct);

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPost("receipts")]
    public async Task<IActionResult> CreateReceipt(PurchaseReceiptRequest request, CancellationToken ct) =>
        ToHttpResult(await service.CreateReceiptAsync(ToCommand(request), int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null, ct));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpPut("receipts/{id}")]
    public async Task<IActionResult> UpdateReceipt(string id, PurchaseReceiptRequest request, CancellationToken ct) =>
        ToHttpResult(await service.UpdateReceiptAsync(id, ToCommand(request), ct));

    [Authorize(Roles = StaffRoles.ManagerOrInventory)]
    [HttpDelete("receipts/{id}")]
    [HttpPut("receipts/{id}/cancel")]
    public async Task<IActionResult> CancelReceipt(string id, CancellationToken ct) => ToHttpResult(await service.CancelReceiptAsync(id, ct));

    private static ReceiptCommand ToCommand(PurchaseReceiptRequest request) => new(request.SupplierId,
        request.Lines.Select(x => new ReceiptLineCommand(x.BookId, x.Quantity, x.UnitCost)).ToArray());

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
