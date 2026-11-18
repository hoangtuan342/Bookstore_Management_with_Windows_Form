
namespace TrangSach.Shared.DTO
{
    public class ManagerPromotionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public List<string> BookOffers { get; set; } = new List<string>();
        public List<string> OrderOffers { get; set; } = new List<string>();
        public List<string> Vouchers { get; set; } = new List<string>();
        public List<PromotionBookRuleDto> BookRules { get; set; } = new List<PromotionBookRuleDto>();
        public List<PromotionOrderRuleDto> OrderRules { get; set; } = new List<PromotionOrderRuleDto>();
        public List<PromotionVoucherRuleDto> VoucherRules { get; set; } = new List<PromotionVoucherRuleDto>();

        public ManagerPromotionDto()
        {
        }

        public ManagerPromotionDto(string id, string name, DateTime startsAt, DateTime endsAt, List<string> bookOffers, List<string> orderOffers, List<string> vouchers, List<PromotionBookRuleDto> bookRules, List<PromotionOrderRuleDto> orderRules, List<PromotionVoucherRuleDto> voucherRules)
        {
            this.Id = id;
            this.Name = name;
            this.StartsAt = startsAt;
            this.EndsAt = endsAt;
            this.BookOffers = bookOffers;
            this.OrderOffers = orderOffers;
            this.Vouchers = vouchers;
            this.BookRules = bookRules;
            this.OrderRules = orderRules;
            this.VoucherRules = voucherRules;
        }
    }
}

