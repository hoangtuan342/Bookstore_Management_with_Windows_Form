
namespace TrangSach.Shared.DTO
{
    public class ManagerVoucherDto
    {
        public string Id { get; set; } = string.Empty;
        public string PromotionInfo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUsageCount { get; set; }
        public int UsageCount { get; set; }
        public List<string> Promotions { get; set; } = new List<string>();
        public int OrderCount { get; set; }

        public ManagerVoucherDto()
        {
        }

        public ManagerVoucherDto(string id, string status, DateTime? expiresAt, int? maxUsageCount, int usageCount, List<string> promotions, int orderCount, string? promotionInfo = null)
        {
            this.Id = id;
            this.Status = status;
            this.ExpiresAt = expiresAt;
            this.MaxUsageCount = maxUsageCount;
            this.UsageCount = usageCount;
            this.Promotions = promotions;
            this.OrderCount = orderCount;
            this.PromotionInfo = !string.IsNullOrWhiteSpace(promotionInfo) 
                ? promotionInfo 
                : (promotions != null && promotions.Count > 0 ? string.Join(", ", promotions) : "Chưa gắn CTKM");
        }
    }
}

