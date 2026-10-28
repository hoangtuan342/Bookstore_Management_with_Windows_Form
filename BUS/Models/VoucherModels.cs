namespace TrangSach.BUS.Models;

public sealed record VoucherSummary(string Id, string Status, DateTime? ExpiresAt, int? MaxUsageCount,
    int UsageCount, IReadOnlyList<string> Promotions, int OrderCount);
public sealed record VoucherUpdateCommand(string Status, DateTime? ExpiresAt, int? MaxUsageCount);
