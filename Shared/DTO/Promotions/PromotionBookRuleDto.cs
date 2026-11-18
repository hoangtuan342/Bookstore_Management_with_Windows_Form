
namespace TrangSach.Shared.DTO
{
    public class PromotionBookRuleDto
    {
        public string BookId { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }

        public PromotionBookRuleDto()
        {
        }

        public PromotionBookRuleDto(string bookId, decimal discountPercent)
        {
            this.BookId = bookId;
            this.DiscountPercent = discountPercent;
        }
    }
}

