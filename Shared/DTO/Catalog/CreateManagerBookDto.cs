
namespace TrangSach.Shared.DTO
{
    public class CreateManagerBookDto
    {
        public string Title { get; set; } = string.Empty;
        public decimal SalePrice { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public int? PublisherId { get; set; }
        public IReadOnlyList<int> AuthorIds { get; set; } = new List<int>();
        public string Description { get; set; } = "";

        public CreateManagerBookDto()
        {
        }

        public CreateManagerBookDto(string title, decimal salePrice, string unit, string categoryId, int? publisherId, IReadOnlyList<int> authorIds, string description = "")
        {
            this.Title = title;
            this.SalePrice = salePrice;
            this.Unit = unit;
            this.CategoryId = categoryId;
            this.PublisherId = publisherId;
            this.AuthorIds = authorIds;
            this.Description = description;
        }
    }
}

