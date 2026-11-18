
namespace TrangSach.Shared.DTO
{
    public class YearlyRevenueDto
    {
        public int Year { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public decimal PurchaseSpend { get; set; }
        public decimal EstimatedGrossProfit { get; set; }

        public YearlyRevenueDto()
        {
        }

        public YearlyRevenueDto(int year, decimal revenue, int orders, decimal purchaseSpend, decimal estimatedGrossProfit)
        {
            this.Year = year;
            this.Revenue = revenue;
            this.Orders = orders;
            this.PurchaseSpend = purchaseSpend;
            this.EstimatedGrossProfit = estimatedGrossProfit;
        }
    }
}

