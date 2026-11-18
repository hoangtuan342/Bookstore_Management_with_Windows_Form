
namespace TrangSach.Shared.DTO
{
    public class BookItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string CoverStyle { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int AvailableStock { get; set; } = 0;
        public string Publisher { get; set; } = "";
        public string CoverImagePath { get; set; } = "";
        public string Isbn { get; set; } = "";

        public BookItemDto()
        {
        }

        public BookItemDto(string id, string title, string author, string category, decimal price, string coverStyle, string label, string description, int availableStock = 0, string publisher = "", string coverImagePath = "", string isbn = "")
        {
            this.Id = id;
            this.Title = title;
            this.Author = author;
            this.Category = category;
            this.Price = price;
            this.CoverStyle = coverStyle;
            this.Label = label;
            this.Description = description;
            this.AvailableStock = availableStock;
            this.Publisher = publisher;
            this.CoverImagePath = coverImagePath;
            this.Isbn = isbn;
        }
    }
}

