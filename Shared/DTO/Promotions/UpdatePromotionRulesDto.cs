
namespace TrangSach.Shared.DTO
{
    public class UpdatePromotionRulesDto
    {
        public IReadOnlyList<PromotionBookRuleDto> Books { get; set; } = new List<PromotionBookRuleDto>();
        public IReadOnlyList<PromotionOrderRuleDto> Orders { get; set; } = new List<PromotionOrderRuleDto>();
        public IReadOnlyList<PromotionVoucherRuleDto> Vouchers { get; set; } = new List<PromotionVoucherRuleDto>();

        public UpdatePromotionRulesDto()
        {
        }

        public UpdatePromotionRulesDto(IReadOnlyList<PromotionBookRuleDto> books, IReadOnlyList<PromotionOrderRuleDto> orders, IReadOnlyList<PromotionVoucherRuleDto> vouchers)
        {
            this.Books = books;
            this.Orders = orders;
            this.Vouchers = vouchers;
        }
    }
}

