
namespace TrangSach.Shared.DTO
{
    public class RevenueByDayDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public decimal PurchaseSpend { get; set; }
        public int Orders { get; set; }

        public RevenueByDayDto()
        {
        }

        public RevenueByDayDto(DateTime date, decimal revenue, decimal purchaseSpend, int orders)
        {
            this.Date = date;
            this.Revenue = revenue;
            this.PurchaseSpend = purchaseSpend;
            this.Orders = orders;
        }
    }
}

