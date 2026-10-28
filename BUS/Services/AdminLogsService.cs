using BookStore.Shared.Models;
using BookStore.Domain.Entities;
using TrangSach.BUS.Models;
using TrangSach.DAO.Entities;

namespace TrangSach.BUS.Services;

public sealed class AdminLogsService(BookStoreDao dao)
{
    public async Task<IReadOnlyList<ManagerAuditLog>> GetLogsAsync(CancellationToken cancellationToken = default)
    {
        var entries = await dao.AuditLogs.GetLatestAsync(500, cancellationToken);
        return entries.Select(item => new ManagerAuditLog(item.Id, item.OccurredAt, item.ActorName, item.ActorCode,
            item.ActorRole, item.Action, item.Area, item.Resource, item.StatusCode, item.IpAddress)).ToArray();
    }

    public async Task WriteRequestAuditAsync(AuditLogCommand command, CancellationToken cancellationToken = default)
    {
        var account = command.AccountId is not null
            ? await dao.Accounts.FindAuditDetailsByIdAsync(command.AccountId.Value, cancellationToken)
            : !string.IsNullOrWhiteSpace(command.LoginIdentifier)
                ? await dao.Accounts.FindAuditDetailsByIdentifierAsync(command.LoginIdentifier, cancellationToken)
                : null;

        var accountId = command.AccountId ?? account?.Id;
        var actorName = account is null
            ? string.IsNullOrWhiteSpace(command.LoginIdentifier) ? "Chưa xác thực" : command.LoginIdentifier
            : !string.IsNullOrWhiteSpace(account.EmployeeName) ? account.EmployeeName
            : !string.IsNullOrWhiteSpace(account.CustomerName) ? account.CustomerName
            : account.UserName;
        var actorCode = account?.EmployeeCode ?? account?.CustomerCode;
        var actorRole = account?.RoleName ?? command.TokenRole ?? "Unknown";
        var entry = new SystemAuditLog
        {
            OccurredAt = DateTime.UtcNow,
            ActorAccountId = accountId,
            ActorName = actorName[..Math.Min(actorName.Length, 100)],
            ActorCode = actorCode,
            ActorRole = actorRole[..Math.Min(actorRole.Length, 60)],
            Action = command.Action,
            Area = command.Area,
            Resource = command.Resource[..Math.Min(command.Resource.Length, 300)],
            StatusCode = command.StatusCode,
            IpAddress = command.IpAddress
        };
        dao.AuditLogs.Add(entry);
        await dao.SaveChangesAsync(cancellationToken);
    }
}
