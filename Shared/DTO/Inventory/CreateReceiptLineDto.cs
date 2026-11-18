
namespace TrangSach.Shared.DTO
{
    public class CreateReceiptLineDto
    {
        public string BookId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }

        public CreateReceiptLineDto()
        {
        }

        public CreateReceiptLineDto(string bookId, int quantity, decimal unitCost)
        {
            this.BookId = bookId;
            this.Quantity = quantity;
            this.UnitCost = unitCost;
        }
    }
}

