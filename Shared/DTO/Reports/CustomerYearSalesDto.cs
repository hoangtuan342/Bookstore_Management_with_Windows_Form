namespace TrangSach.Shared.DTO
{
    public class CustomerYearSalesDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Orders { get; set; }
        public decimal Revenue { get; set; }

        public CustomerYearSalesDto()
        {
        }

        public CustomerYearSalesDto(string customerId, string customerName, int year, int orders, decimal revenue)
        {
            this.CustomerId = customerId;
            this.CustomerName = customerName;
            this.Year = year;
            this.Orders = orders;
            this.Revenue = revenue;
        }
    }
}
