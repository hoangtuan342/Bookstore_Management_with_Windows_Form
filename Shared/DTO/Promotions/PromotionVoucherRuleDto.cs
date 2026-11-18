
namespace TrangSach.Shared.DTO
{
    public class PromotionVoucherRuleDto
    {
        public string VoucherId { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }

        public PromotionVoucherRuleDto()
        {
        }

        public PromotionVoucherRuleDto(string voucherId, decimal discountPercent)
        {
            this.VoucherId = voucherId;
            this.DiscountPercent = discountPercent;
        }
    }
}

