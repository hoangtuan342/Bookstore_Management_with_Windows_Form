namespace TrangSach.BUS.Models;

public sealed record AuditLogCommand(
    int? AccountId,
    string? LoginIdentifier,
    string? TokenRole,
    string Action,
    string Area,
    string Resource,
    int StatusCode,
    string? IpAddress);
