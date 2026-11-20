
namespace TrangSach.Shared.DTO
{
    public class BookManagementCommandDto
    {
        public string Title { get; set; } = string.Empty;
        public decimal SalePrice { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public int? PublisherId { get; set; }
        public List<int> AuthorIds { get; set; } = new List<int>();
        public string Description { get; set; } = "";
        public string? CoverImagePath { get; set; } = "";
        public string? Isbn { get; set; } = "";
        public bool IsActive { get; set; } = true;

        public BookManagementCommandDto()
        {
        }

        public BookManagementCommandDto(
            string title,
            decimal salePrice,
            string unit,
            string categoryId,
            int? publisherId,
            List<int> authorIds,
            string description = "",
            string? coverImagePath = "",
            string? isbn = "",
            bool isActive = true)
        {
            this.Title = title;
            this.SalePrice = salePrice;
            this.Unit = unit;
            this.CategoryId = categoryId;
            this.PublisherId = publisherId;
            this.AuthorIds = authorIds;
            this.Description = description;
            this.CoverImagePath = coverImagePath;
            this.Isbn = isbn;
            this.IsActive = isActive;
        }
    }
}

