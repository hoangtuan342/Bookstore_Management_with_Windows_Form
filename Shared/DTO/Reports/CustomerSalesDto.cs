
namespace TrangSach.Shared.DTO
{
    public class CustomerSalesDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int Orders { get; set; }
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }

        public CustomerSalesDto()
        {
        }

        public CustomerSalesDto(string customerId, string customerName, int orders, int quantity, decimal revenue)
        {
            this.CustomerId = customerId;
            this.CustomerName = customerName;
            this.Orders = orders;
            this.Quantity = quantity;
            this.Revenue = revenue;
        }
    }
}

