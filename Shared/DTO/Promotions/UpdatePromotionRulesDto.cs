
namespace TrangSach.Shared.DTO
{
    public class UpdatePromotionRulesDto
    {
        public List<PromotionBookRuleDto> Books { get; set; } = new List<PromotionBookRuleDto>();
        public List<PromotionOrderRuleDto> Orders { get; set; } = new List<PromotionOrderRuleDto>();
        public List<PromotionVoucherRuleDto> Vouchers { get; set; } = new List<PromotionVoucherRuleDto>();

        public UpdatePromotionRulesDto()
        {
        }

        public UpdatePromotionRulesDto(List<PromotionBookRuleDto> books, List<PromotionOrderRuleDto> orders, List<PromotionVoucherRuleDto> vouchers)
        {
            this.Books = books;
            this.Orders = orders;
            this.Vouchers = vouchers;
        }
    }
}

