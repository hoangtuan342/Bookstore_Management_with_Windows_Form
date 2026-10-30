using BookStore.Shared.Models;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.BUS.Services;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/admin/logs")]
[Authorize(Roles = StaffRoles.Manager)]
public sealed class AdminLogsController(AdminLogsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ManagerAuditLog>>> GetLogs(CancellationToken cancellationToken) =>
        Ok(await service.GetLogsAsync(cancellationToken));
}
