
namespace TrangSach.Shared.DTO
{
    public class ManagerAuditLogDto
    {
        public long Id { get; set; }
        public DateTime OccurredAt { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string? ActorCode { get; set; }
        public string ActorRole { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? IpAddress { get; set; }

        public ManagerAuditLogDto()
        {
        }

        public ManagerAuditLogDto(long id, DateTime occurredAt, string actorName, string? actorCode, string actorRole, string action, string area, string resource, int statusCode, string? ipAddress)
        {
            this.Id = id;
            this.OccurredAt = occurredAt;
            this.ActorName = actorName;
            this.ActorCode = actorCode;
            this.ActorRole = actorRole;
            this.Action = action;
            this.Area = area;
            this.Resource = resource;
            this.StatusCode = statusCode;
            this.IpAddress = ipAddress;
        }
    }
}

