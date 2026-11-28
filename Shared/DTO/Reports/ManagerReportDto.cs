
namespace TrangSach.Shared.DTO
{
    public class ManagerReportDto
    {
        public int Days { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal Revenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal Discounts { get; set; }
        public decimal PurchaseSpend { get; set; }
        public decimal EstimatedCostOfGoods { get; set; }
        public decimal EstimatedGrossProfit { get; set; }
        public decimal CostCoveragePercent { get; set; }
        public decimal PreviousPeriodRevenue { get; set; }
        public int PreviousPeriodCompletedOrders { get; set; }
        public decimal? RevenueChangePercent { get; set; }
        public decimal? CompletedOrdersChangePercent { get; set; }
        public List<RevenueByDayDto> RevenueByDayDto { get; set; } = new List<RevenueByDayDto>();
        public List<BestSellerDto> BestSellers { get; set; } = new List<BestSellerDto>();
        public List<CategorySalesDto> RevenueByCategory { get; set; } = new List<CategorySalesDto>();
        public List<CustomerSalesDto> TopCustomers { get; set; } = new List<CustomerSalesDto>();
        public List<CategoryInventorySummaryDto> InventoryByCategory { get; set; } = new List<CategoryInventorySummaryDto>();
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<MonthlyRevenueDto> RevenueByMonth { get; set; } = new List<MonthlyRevenueDto>();
        public List<QuarterlyRevenueDto> RevenueByQuarter { get; set; } = new List<QuarterlyRevenueDto>();
        public List<YearlyRevenueDto> RevenueByYear { get; set; } = new List<YearlyRevenueDto>();
        public List<EmployeeYearSalesDto> RevenueByEmployeeYear { get; set; } = new List<EmployeeYearSalesDto>();
        public List<CustomerYearSalesDto> RevenueByCustomerYear { get; set; } = new List<CustomerYearSalesDto>();
        public List<ProductQuarterSalesDto> ProductSalesByQuarter { get; set; } = new List<ProductQuarterSalesDto>();

        public ManagerReportDto()
        {
        }

        public ManagerReportDto(int days, int totalOrders, int completedOrders, int processingOrders, int pendingOrders, int cancelledOrders, decimal revenue, decimal averageOrderValue, decimal discounts, decimal purchaseSpend, decimal estimatedCostOfGoods, decimal estimatedGrossProfit, decimal costCoveragePercent, decimal previousPeriodRevenue, int previousPeriodCompletedOrders, decimal? revenueChangePercent, decimal? completedOrdersChangePercent, List<RevenueByDayDto> revenueByDayDto, List<BestSellerDto> bestSellers, List<CategorySalesDto> revenueByCategory, List<CustomerSalesDto> topCustomers, List<CategoryInventorySummaryDto> inventoryByCategory, DateTime fromDate, DateTime toDate, List<MonthlyRevenueDto> revenueByMonth, List<QuarterlyRevenueDto> revenueByQuarter, List<YearlyRevenueDto> revenueByYear, List<EmployeeYearSalesDto> revenueByEmployeeYear, List<ProductQuarterSalesDto> productSalesByQuarter, List<CustomerYearSalesDto>? revenueByCustomerYear = null)
        {
            this.Days = days;
            this.TotalOrders = totalOrders;
            this.CompletedOrders = completedOrders;
            this.ProcessingOrders = processingOrders;
            this.PendingOrders = pendingOrders;
            this.CancelledOrders = cancelledOrders;
            this.Revenue = revenue;
            this.AverageOrderValue = averageOrderValue;
            this.Discounts = discounts;
            this.PurchaseSpend = purchaseSpend;
            this.EstimatedCostOfGoods = estimatedCostOfGoods;
            this.EstimatedGrossProfit = estimatedGrossProfit;
            this.CostCoveragePercent = costCoveragePercent;
            this.PreviousPeriodRevenue = previousPeriodRevenue;
            this.PreviousPeriodCompletedOrders = previousPeriodCompletedOrders;
            this.RevenueChangePercent = revenueChangePercent;
            this.CompletedOrdersChangePercent = completedOrdersChangePercent;
            this.RevenueByDayDto = revenueByDayDto;
            this.BestSellers = bestSellers;
            this.RevenueByCategory = revenueByCategory;
            this.TopCustomers = topCustomers;
            this.InventoryByCategory = inventoryByCategory;
            this.FromDate = fromDate;
            this.ToDate = toDate;
            this.RevenueByMonth = revenueByMonth;
            this.RevenueByQuarter = revenueByQuarter;
            this.RevenueByYear = revenueByYear;
            this.RevenueByEmployeeYear = revenueByEmployeeYear;
            this.RevenueByCustomerYear = revenueByCustomerYear ?? new List<CustomerYearSalesDto>();
            this.ProductSalesByQuarter = productSalesByQuarter;
        }
    }
}

