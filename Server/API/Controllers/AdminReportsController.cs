using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace TrangSach.API.Controllers;

[ApiController]
[Authorize(Roles = StaffRoles.ManagerOrAccountant)]
[Route("api/admin/reports")]
public sealed class AdminReportsController(AdminReportsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ManagerReportDto>(StatusCodes.Status200OK)]
    public ActionResult<ManagerReportDto> Get(
        [FromQuery] int days = 30,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null) =>
        Ok(fromDate.HasValue && toDate.HasValue
            ? service.Get(fromDate.Value, toDate.Value)
            : service.Get(days));
}
