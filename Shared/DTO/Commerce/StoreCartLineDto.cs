
namespace TrangSach.Shared.DTO
{
    public class StoreCartLineDto
    {
        public string BookId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int AvailableStock { get; set; }
        public decimal LineTotal { get; set; }
        public string? CoverImagePath { get; set; }

        public StoreCartLineDto()
        {
        }

        public StoreCartLineDto(string bookId, string title, string author, decimal unitPrice, int quantity, int availableStock, decimal lineTotal, string? coverImagePath = null)
        {
            this.BookId = bookId;
            this.Title = title;
            this.Author = author;
            this.UnitPrice = unitPrice;
            this.Quantity = quantity;
            this.AvailableStock = availableStock;
            this.LineTotal = lineTotal;
            this.CoverImagePath = coverImagePath;
        }
    }
}

