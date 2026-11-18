using TrangSach.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace TrangSach.API.Controllers;

[ApiController]
[Authorize(Roles = StaffRoles.ManagerOrAccountant)]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController(AdminDashboardService service) : ControllerBase
{
    [HttpGet]
    public IActionResult GetSummary() =>
        Ok(service.GetSummary());
}
