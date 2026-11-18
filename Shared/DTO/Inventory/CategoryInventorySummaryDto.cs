
namespace TrangSach.Shared.DTO
{
    public class CategoryInventorySummaryDto
    {
        public string Category { get; set; } = string.Empty;
        public int BookCount { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockBooks { get; set; }
        public decimal StockValue { get; set; }

        public CategoryInventorySummaryDto()
        {
        }

        public CategoryInventorySummaryDto(string category, int bookCount, int stockQuantity, int lowStockBooks, decimal stockValue)
        {
            this.Category = category;
            this.BookCount = bookCount;
            this.StockQuantity = stockQuantity;
            this.LowStockBooks = lowStockBooks;
            this.StockValue = stockValue;
        }
    }
}

