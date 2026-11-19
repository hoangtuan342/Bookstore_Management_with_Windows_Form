namespace TrangSach.Shared.DTO
{
    public class CreateVoucherDto
    {
        public string? Code { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime? ExpiresAt { get; set; }
        public int? MaxUsageCount { get; set; }
        public string? PromotionId { get; set; }
        public decimal? DiscountPercent { get; set; }

        public CreateVoucherDto()
        {
        }

        public CreateVoucherDto(string? code, string status, DateTime? expiresAt, int? maxUsageCount, string? promotionId = null, decimal? discountPercent = null)
        {
            this.Code = code;
            this.Status = status;
            this.ExpiresAt = expiresAt;
            this.MaxUsageCount = maxUsageCount;
            this.PromotionId = promotionId;
            this.DiscountPercent = discountPercent;
        }
    }
}
