
namespace TrangSach.Shared.DTO
{
    public class ManagerOrderLineDto
    {
        public string BookId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public ManagerOrderLineDto()
        {
        }

        public ManagerOrderLineDto(string bookId, string title, int quantity, decimal unitPrice, decimal lineTotal)
        {
            this.BookId = bookId;
            this.Title = title;
            this.Quantity = quantity;
            this.UnitPrice = unitPrice;
            this.LineTotal = lineTotal;
        }
    }
}

