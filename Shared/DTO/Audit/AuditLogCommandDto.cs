
namespace TrangSach.Shared.DTO
{
    public class AuditLogCommandDto
    {
        public int? AccountId { get; set; }
        public string? LoginIdentifier { get; set; }
        public string? TokenRole { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? IpAddress { get; set; }

        public AuditLogCommandDto()
        {
        }

        public AuditLogCommandDto(int? accountId, string? loginIdentifier, string? tokenRole, string action, string area, string resource, int statusCode, string? ipAddress)
        {
            this.AccountId = accountId;
            this.LoginIdentifier = loginIdentifier;
            this.TokenRole = tokenRole;
            this.Action = action;
            this.Area = area;
            this.Resource = resource;
            this.StatusCode = statusCode;
            this.IpAddress = ipAddress;
        }
    }
}

