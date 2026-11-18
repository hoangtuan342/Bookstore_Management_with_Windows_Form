using System.ComponentModel.DataAnnotations;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TrangSach.API.Controllers;

[ApiController]
[Authorize(Roles = StaffRoles.ManagerOrMarketing)]
[Route("api/admin/vouchers")]
public sealed class AdminVouchersController(AdminVoucherService service) : ControllerBase
{
    [HttpGet]
    public List<VoucherSummaryDto> Get() => service.GetAllVouchersList();

    [HttpPut("{id}")]
    public IActionResult Update(string id, VoucherUpdateRequest request) =>
        ControllerExceptionHelper.Execute(() => service.Update(id, new VoucherUpdateCommandDto(request.Status, request.ExpiresAt, request.MaxUsageCount)));

    [HttpDelete("{id}")]
    public IActionResult Delete(string id) =>
        ControllerExceptionHelper.Execute(() => service.Delete(id));
}

public sealed class VoucherUpdateRequest
{
    [Required, StringLength(30)] public string Status { get; init; } = "Active";
    public DateTime? ExpiresAt { get; init; }
    [Range(1, 1000000)] public int? MaxUsageCount { get; init; }
}
