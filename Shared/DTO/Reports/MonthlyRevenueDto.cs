
namespace TrangSach.Shared.DTO
{
    public class MonthlyRevenueDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public decimal PurchaseSpend { get; set; }
        public decimal EstimatedGrossProfit { get; set; }

        public MonthlyRevenueDto()
        {
        }

        public MonthlyRevenueDto(int year, int month, decimal revenue, int orders, decimal purchaseSpend, decimal estimatedGrossProfit)
        {
            this.Year = year;
            this.Month = month;
            this.Revenue = revenue;
            this.Orders = orders;
            this.PurchaseSpend = purchaseSpend;
            this.EstimatedGrossProfit = estimatedGrossProfit;
        }
    }
}

