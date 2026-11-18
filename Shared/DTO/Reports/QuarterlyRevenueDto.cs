
namespace TrangSach.Shared.DTO
{
    public class QuarterlyRevenueDto
    {
        public int Year { get; set; }
        public int Quarter { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public decimal PurchaseSpend { get; set; }
        public decimal EstimatedGrossProfit { get; set; }

        public QuarterlyRevenueDto()
        {
        }

        public QuarterlyRevenueDto(int year, int quarter, decimal revenue, int orders, decimal purchaseSpend, decimal estimatedGrossProfit)
        {
            this.Year = year;
            this.Quarter = quarter;
            this.Revenue = revenue;
            this.Orders = orders;
            this.PurchaseSpend = purchaseSpend;
            this.EstimatedGrossProfit = estimatedGrossProfit;
        }
    }
}

