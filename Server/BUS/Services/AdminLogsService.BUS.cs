using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrangSach.Shared.DTO;

namespace TrangSach.BUS
{
    public class AdminLogsService
    {
        private readonly BookStoreDao dao;

        public AdminLogsService(BookStoreDao dao)
        {
            this.dao = dao;
        }

        public async Task<IReadOnlyList<ManagerAuditLogDto>> GetLogsAsync(CancellationToken cancellationToken = default)
        {
            return await dao.Users.GetAuditLogsAsync(500, cancellationToken);
        }

        public async Task WriteRequestAuditAsync(AuditLogCommandDto command, CancellationToken cancellationToken = default)
        {
            var auditDetails = await dao.Users.GetAuditDetailsAsync(command.AccountId, command.LoginIdentifier, cancellationToken);

            string actorName = "Chưa xác thực";
            if (auditDetails.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(auditDetails.Value.EmployeeName))
                    actorName = auditDetails.Value.EmployeeName;
                else if (!string.IsNullOrWhiteSpace(auditDetails.Value.CustomerName))
                    actorName = auditDetails.Value.CustomerName;
                else
                    actorName = auditDetails.Value.UserName;
            }
            else if (!string.IsNullOrWhiteSpace(command.LoginIdentifier))
            {
                actorName = command.LoginIdentifier;
            }

            string safeActorName = actorName.Length > 100 ? actorName.Substring(0, 100) : actorName;
            var safeCmd = new AuditLogCommandDto(
                command.AccountId,
                safeActorName,
                auditDetails?.RoleName ?? command.TokenRole ?? "Unknown",
                command.Action,
                command.Area,
                command.Resource,
                command.StatusCode,
                command.IpAddress);

            await dao.Users.InsertAuditLogAsync(safeCmd, cancellationToken);
        }
    }
}
