using System.ComponentModel.DataAnnotations;
using TrangSach.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Shared.DTO;
namespace TrangSach.API.Controllers;

[ApiController]
[Authorize(Roles = StaffRoles.ManagerOrMarketing)]
[Route("api/admin/vouchers")]
public sealed class AdminVouchersController(AdminVoucherService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<VoucherSummaryDto>> Get(CancellationToken ct) => service.GetAsync(ct);

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, VoucherUpdateRequest request, CancellationToken ct) =>
        ToHttpResult(await service.UpdateAsync(id, new VoucherUpdateCommandDto(request.Status, request.ExpiresAt, request.MaxUsageCount), ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct) => ToHttpResult(await service.DeleteAsync(id, ct));

    private IActionResult ToHttpResult(OperationResult<Unit> result) => result.Status switch
    {
        OperationStatus.Success => NoContent(),
        OperationStatus.NotFound => NotFound(),
        OperationStatus.Invalid => BadRequest(new { message = result.Message }),
        OperationStatus.Conflict => Conflict(new { message = result.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}

public sealed class VoucherUpdateRequest
{
    [Required, StringLength(30)] public string Status { get; init; } = "Active";
    public DateTime? ExpiresAt { get; init; }
    [Range(1, 1000000)] public int? MaxUsageCount { get; init; }
}


