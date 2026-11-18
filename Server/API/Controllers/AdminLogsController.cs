using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace TrangSach.API.Controllers;

[ApiController]
[Authorize(Roles = StaffRoles.Manager)]
[Route("api/admin/logs")]
public sealed class AdminLogsController(AdminLogsService service) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<ManagerAuditLogDto>> GetLogs() =>
        Ok(service.GetLogs());
}
