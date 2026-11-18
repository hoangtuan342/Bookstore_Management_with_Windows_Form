
namespace TrangSach.Shared.DTO
{
    public class ManagerRecentOrderDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public ManagerRecentOrderDto()
        {
        }

        public ManagerRecentOrderDto(string id, DateTime createdAt, string orderStatus, decimal totalAmount, string customerName)
        {
            this.Id = id;
            this.CreatedAt = createdAt;
            this.OrderStatus = orderStatus;
            this.TotalAmount = totalAmount;
            this.CustomerName = customerName;
        }
    }
}

