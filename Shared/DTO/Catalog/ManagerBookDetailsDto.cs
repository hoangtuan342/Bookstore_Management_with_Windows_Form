
namespace TrangSach.Shared.DTO
{
    public class ManagerBookDetailsDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal SalePrice { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public int? PublisherId { get; set; }
        public List<int> AuthorIds { get; set; } = new List<int>();
        public bool IsActive { get; set; }
        public string Description { get; set; } = "";
        public string? CoverImagePath { get; set; } = "";
        public string? Isbn { get; set; } = "";
        public int AvailableStock { get; set; } = 0;

        public ManagerBookDetailsDto()
        {
        }

        public ManagerBookDetailsDto(
            string id,
            string title,
            decimal salePrice,
            string unit,
            string categoryId,
            int? publisherId,
            List<int> authorIds,
            bool isActive,
            string description = "",
            string? coverImagePath = "",
            string? isbn = "",
            int availableStock = 0)
        {
            this.Id = id;
            this.Title = title;
            this.SalePrice = salePrice;
            this.Unit = unit;
            this.CategoryId = categoryId;
            this.PublisherId = publisherId;
            this.AuthorIds = authorIds;
            this.IsActive = isActive;
            this.Description = description;
            this.CoverImagePath = coverImagePath;
            this.Isbn = isbn;
            this.AvailableStock = availableStock;
        }
    }
}

