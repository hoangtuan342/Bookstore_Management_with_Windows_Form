
namespace TrangSach.Shared.DTO
{
    public class CategorySalesDto
    {
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }

        public CategorySalesDto()
        {
        }

        public CategorySalesDto(string category, int quantity, decimal revenue)
        {
            this.Category = category;
            this.Quantity = quantity;
            this.Revenue = revenue;
        }
    }
}

