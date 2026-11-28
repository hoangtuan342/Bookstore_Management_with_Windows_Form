
namespace TrangSach.Shared.DTO
{
    public class ManagerOrderLineDto
    {
        public string BookId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public decimal OriginalPrice { get; set; } = 0;
        public decimal DiscountPercent { get; set; } = 0;
        public string PromotionName { get; set; } = string.Empty;

        public ManagerOrderLineDto()
        {
        }

        public ManagerOrderLineDto(string bookId, string title, int quantity, decimal unitPrice, decimal lineTotal, decimal originalPrice = 0, decimal discountPercent = 0, string promotionName = "")
        {
            this.BookId = bookId;
            this.Title = title;
            this.Quantity = quantity;
            this.UnitPrice = unitPrice;
            this.LineTotal = lineTotal;
            this.OriginalPrice = originalPrice > 0 ? originalPrice : unitPrice;
            this.DiscountPercent = discountPercent;
            this.PromotionName = promotionName;
        }
    }
}

