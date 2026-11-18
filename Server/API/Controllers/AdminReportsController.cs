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
    public async Task<IActionResult> Get([FromQuery] int days = 30, [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null, CancellationToken cancellationToken = default)
    {
        if (fromDate.HasValue != toDate.HasValue)
            return BadRequest(new { message = "Cần truyền cả fromDate và toDate để thống kê theo khoảng ngày." });
        if (fromDate.HasValue && toDate!.Value < fromDate.Value)
            return BadRequest(new { message = "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu." });
        return Ok(fromDate is not null
            ? await service.GetAsync(fromDate.Value, toDate!.Value, cancellationToken)
            : await service.GetAsync(days, cancellationToken));
    }
}


