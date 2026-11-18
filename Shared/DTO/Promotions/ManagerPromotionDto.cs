
namespace TrangSach.Shared.DTO
{
    public class ManagerPromotionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public IReadOnlyList<string> BookOffers { get; set; } = new List<string>();
        public IReadOnlyList<string> OrderOffers { get; set; } = new List<string>();
        public IReadOnlyList<string> Vouchers { get; set; } = new List<string>();
        public IReadOnlyList<PromotionBookRuleDto> BookRules { get; set; } = new List<PromotionBookRuleDto>();
        public IReadOnlyList<PromotionOrderRuleDto> OrderRules { get; set; } = new List<PromotionOrderRuleDto>();
        public IReadOnlyList<PromotionVoucherRuleDto> VoucherRules { get; set; } = new List<PromotionVoucherRuleDto>();

        public ManagerPromotionDto()
        {
        }

        public ManagerPromotionDto(string id, string name, DateTime startsAt, DateTime endsAt, IReadOnlyList<string> bookOffers, IReadOnlyList<string> orderOffers, IReadOnlyList<string> vouchers, IReadOnlyList<PromotionBookRuleDto> bookRules, IReadOnlyList<PromotionOrderRuleDto> orderRules, IReadOnlyList<PromotionVoucherRuleDto> voucherRules)
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

