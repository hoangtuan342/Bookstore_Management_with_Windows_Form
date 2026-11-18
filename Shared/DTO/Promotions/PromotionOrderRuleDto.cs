
namespace TrangSach.Shared.DTO
{
    public class PromotionOrderRuleDto
    {
        public decimal MinimumOrderAmount { get; set; }
        public decimal DiscountPercent { get; set; }

        public PromotionOrderRuleDto()
        {
        }

        public PromotionOrderRuleDto(decimal minimumOrderAmount, decimal discountPercent)
        {
            this.MinimumOrderAmount = minimumOrderAmount;
            this.DiscountPercent = discountPercent;
        }
    }
}

