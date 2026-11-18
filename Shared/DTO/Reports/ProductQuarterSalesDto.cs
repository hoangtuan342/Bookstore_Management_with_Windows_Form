
namespace TrangSach.Shared.DTO
{
    public class ProductQuarterSalesDto
    {
        public string BookId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Quarter { get; set; }
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }

        public ProductQuarterSalesDto()
        {
        }

        public ProductQuarterSalesDto(string bookId, string title, int year, int quarter, int quantity, decimal revenue)
        {
            this.BookId = bookId;
            this.Title = title;
            this.Year = year;
            this.Quarter = quarter;
            this.Quantity = quantity;
            this.Revenue = revenue;
        }
    }
}

