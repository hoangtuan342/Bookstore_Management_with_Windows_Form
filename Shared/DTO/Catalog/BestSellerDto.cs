
namespace TrangSach.Shared.DTO
{
    public class BestSellerDto
    {
        public string BookId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }

        public BestSellerDto()
        {
        }

        public BestSellerDto(string bookId, string title, int quantity, decimal revenue)
        {
            this.BookId = bookId;
            this.Title = title;
            this.Quantity = quantity;
            this.Revenue = revenue;
        }
    }
}

