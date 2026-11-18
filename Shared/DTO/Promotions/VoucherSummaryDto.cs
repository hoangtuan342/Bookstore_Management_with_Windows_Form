
namespace TrangSach.Shared.DTO
{
    public class VoucherSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUsageCount { get; set; }
        public int UsageCount { get; set; }
        public List<string> Promotions { get; set; } = new List<string>();
        public int OrderCount { get; set; }

        public VoucherSummaryDto()
        {
        }

        public VoucherSummaryDto(string id, string status, DateTime? expiresAt, int? maxUsageCount, int usageCount, List<string> promotions, int orderCount)
        {
            this.Id = id;
            this.Status = status;
            this.ExpiresAt = expiresAt;
            this.MaxUsageCount = maxUsageCount;
            this.UsageCount = usageCount;
            this.Promotions = promotions;
            this.OrderCount = orderCount;
        }
    }
}

