using BookStore.Domain.Entities;
using BookStore.Infrastructure.Persistence;
using BookStore.Shared.Models;
using BookStore.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Api.Controllers;

[ApiController]
[Route("api/admin/logs")]
[Authorize(Roles = StaffRoles.Manager)]
public sealed class AdminLogsController(BookStoreDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ManagerAuditLog>>> GetLogs(CancellationToken cancellationToken)
    {
        var logs = await db.SystemAuditLogs.AsNoTracking()
            .OrderByDescending(item => item.OccurredAt)
            .Take(500)
            .Select(item => new ManagerAuditLog(item.Id, item.OccurredAt, item.ActorName, item.ActorCode, item.ActorRole,
                item.Action, item.Area, item.Resource, item.StatusCode, item.IpAddress))
            .ToListAsync(cancellationToken);
        return Ok(logs);
    }
}
