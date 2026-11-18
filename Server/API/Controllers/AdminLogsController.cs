using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace TrangSach.API.Controllers;

[ApiController]
[Route("api/admin/logs")]
[Authorize(Roles = StaffRoles.Manager)]
public sealed class AdminLogsController(AdminLogsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ManagerAuditLogDto>>> GetLogs(CancellationToken cancellationToken) =>
        Ok(await service.GetLogsAsync(cancellationToken));
}


