
namespace TrangSach.Shared.DTO
{
    public class CreatePromotionDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public string? BookId { get; set; }
        public decimal? BookDiscountPercent { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
        public decimal? OrderDiscountPercent { get; set; }
        public string? VoucherCode { get; set; }
        public decimal? VoucherDiscountPercent { get; set; }
        public int? VoucherMaxUses { get; set; }

        public CreatePromotionDto()
        {
        }

        public CreatePromotionDto(string name, DateTime startsAt, DateTime endsAt, string? bookId, decimal? bookDiscountPercent, decimal? minimumOrderAmount, decimal? orderDiscountPercent, string? voucherCode, decimal? voucherDiscountPercent, int? voucherMaxUses)
        {
            this.Name = name;
            this.StartsAt = startsAt;
            this.EndsAt = endsAt;
            this.BookId = bookId;
            this.BookDiscountPercent = bookDiscountPercent;
            this.MinimumOrderAmount = minimumOrderAmount;
            this.OrderDiscountPercent = orderDiscountPercent;
            this.VoucherCode = voucherCode;
            this.VoucherDiscountPercent = voucherDiscountPercent;
            this.VoucherMaxUses = voucherMaxUses;
        }
    }
}

