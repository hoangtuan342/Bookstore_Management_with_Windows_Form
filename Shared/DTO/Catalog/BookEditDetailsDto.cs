
namespace TrangSach.Shared.DTO
{
    public class BookEditDetailsDto
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

        public BookEditDetailsDto()
        {
        }

        public BookEditDetailsDto(string id, string title, decimal salePrice, string unit, string categoryId, int? publisherId, List<int> authorIds, bool isActive, string description = "")
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
        }
    }
}

