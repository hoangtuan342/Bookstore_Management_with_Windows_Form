namespace BookStore.Domain.Entities;

public sealed class SystemAuditLog
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public int? ActorAccountId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string? ActorCode { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? IpAddress { get; set; }
}
