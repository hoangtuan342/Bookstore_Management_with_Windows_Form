
namespace TrangSach.Shared.DTO
{
    public class ManagerReceiptLineDto
    {
        public string BookId { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal { get; set; }

        public ManagerReceiptLineDto()
        {
        }

        public ManagerReceiptLineDto(string bookId, string bookTitle, int quantity, decimal unitCost, decimal lineTotal)
        {
            this.BookId = bookId;
            this.BookTitle = bookTitle;
            this.Quantity = quantity;
            this.UnitCost = unitCost;
            this.LineTotal = lineTotal;
        }
    }
}

