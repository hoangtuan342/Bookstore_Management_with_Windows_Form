namespace BookStore.Shared.Models;

public sealed record StoreAuthResult(string AccessToken, DateTime ExpiresAtUtc, string UserName, string Role, string? CustomerId);
public sealed record StoreProfile(string UserName, string Email, string Role, string ProfileId,
    string FullName, string? PhoneNumber, string? Address);

public sealed record StoreCartLine(
    string BookId,
    string Title,
    string Author,
    decimal UnitPrice,
    int Quantity,
    int AvailableStock,
    decimal LineTotal);

public sealed record StoreOrderLine(string BookId, string Title, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record StoreOrder(
    string Id,
    DateTime CreatedAt,
    string Status,
    string PaymentStatus,
    string PaymentMethod,
    string ShippingAddress,
    string ReceiverPhone,
    decimal TotalAmount,
    decimal DiscountAmount,
    IReadOnlyList<StoreOrderLine> Lines);

public sealed record CheckoutDetails(string ShippingAddress, string ReceiverPhone, string PaymentMethod, string? VoucherCode = null);

public sealed record StoreApiResult(bool Succeeded, string Message, string? Role = null);

public sealed record ManagerDashboard(
    decimal RevenueThisMonth,
    int OrdersToday,
    int PendingOrders,
    int ActiveBooks,
    int LowStockBooks,
    int CustomerCount,
    IReadOnlyList<ManagerRecentOrder> RecentOrders);

public sealed record ManagerRecentOrder(string Id, DateTime CreatedAt, string OrderStatus, decimal TotalAmount, string CustomerName);

public sealed record ManagerOrderRow(
    string Id,
    DateTime CreatedAt,
    string OrderStatus,
    string PaymentStatus,
    string PaymentMethod,
    decimal TotalAmount,
    string CustomerName,
    string EmployeeName,
    string ReceiverPhone,
    string ShippingAddress,
    int ItemCount,
    IReadOnlyList<ManagerOrderLine> Lines);
public sealed record ManagerOrderLine(string BookId, string Title, int Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record ManagerLookups(
    IReadOnlyList<CategoryOption> Categories,
    IReadOnlyList<IdNameOption> Authors,
    IReadOnlyList<IdNameOption> Publishers,
    IReadOnlyList<SupplierOption> Suppliers);

public sealed record CategoryOption(string Id, string Name);
public sealed record IdNameOption(int Id, string Name);
public sealed record SupplierOption(string Id, string Name, string? PhoneNumber);

public sealed record CreateManagerBook(
    string Title,
    decimal SalePrice,
    string Unit,
    string CategoryId,
    int? PublisherId,
    IReadOnlyList<int> AuthorIds);
public sealed record ManagerBookDetails(string Id, string Title, decimal SalePrice, string Unit, string CategoryId, int? PublisherId, IReadOnlyList<int> AuthorIds, bool IsActive);

public sealed record ManagerReceiptLine(string BookId, string BookTitle, int Quantity, decimal UnitCost, decimal LineTotal);
public sealed record ManagerReceipt(string Id, DateTime CreatedAt, string Status, string SupplierId, string SupplierName, decimal TotalAmount, IReadOnlyList<ManagerReceiptLine> Lines);
public sealed record CreateSupplier(string Name, string? PhoneNumber);
public sealed record ManagerCategory(string Id, string Name);
public sealed record ManagerAuthor(int Id, string Name, string? Biography);
public sealed record ManagerPublisher(int Id, string Name, string? Address);
public sealed record ManagerCustomer(string Id, string FullName, string? PhoneNumber, string? Address, int? AccountId, string? UserName, string? Email, string? AccountStatus);
public sealed record ManagerEmployee(string Id, string LastName, string FirstName, DateOnly? BirthDate, string? AvatarPath, string? Address, string AccountStatus, decimal BaseSalary, int? AccountId, string? UserName, string? Email, string? LoginStatus, string? RoleName);
public sealed record StaffRoleOption(string Name, string Label);
public sealed record SaveCategory(string Id, string Name);
public sealed record SaveAuthor(string Name, string? Biography);
public sealed record SavePublisher(string Name, string? Address);
public sealed record SaveCustomer(string? Id, string FullName, string? PhoneNumber, string? Address, string? UserName, string? Email, string? Password, string? AccountStatus);
public sealed record SaveEmployee(string? Id, string LastName, string FirstName, DateOnly? BirthDate, string? AvatarPath, string? Address, string AccountStatus, decimal BaseSalary, string? UserName, string? Email, string? Password, string? AccountLoginStatus, string RoleName);
public sealed record CreateReceiptLine(string BookId, int Quantity, decimal UnitCost);
public sealed record CreateReceipt(string SupplierId, IReadOnlyList<CreateReceiptLine> Lines);
public sealed record UpdateAdminOrder(string ShippingAddress, string ReceiverPhone, string PaymentMethod);
public sealed record CreateAdminOrderLine(string BookId, int Quantity);
public sealed record CreateAdminOrder(string CustomerId, string ShippingAddress, string ReceiverPhone, string PaymentMethod, IReadOnlyList<CreateAdminOrderLine> Lines);
public sealed record ManagerPromotion(string Id, string Name, DateTime StartsAt, DateTime EndsAt,
    IReadOnlyList<string> BookOffers, IReadOnlyList<string> OrderOffers, IReadOnlyList<string> Vouchers,
    IReadOnlyList<PromotionBookRule> BookRules, IReadOnlyList<PromotionOrderRule> OrderRules, IReadOnlyList<PromotionVoucherRule> VoucherRules);
public sealed record PromotionBookRule(string BookId, decimal DiscountPercent);
public sealed record PromotionOrderRule(decimal MinimumOrderAmount, decimal DiscountPercent);
public sealed record PromotionVoucherRule(string VoucherId, decimal DiscountPercent);
public sealed record UpdatePromotionRules(IReadOnlyList<PromotionBookRule> Books, IReadOnlyList<PromotionOrderRule> Orders, IReadOnlyList<PromotionVoucherRule> Vouchers);
public sealed record ManagerVoucher(string Id, string Status, DateTime? ExpiresAt, int? MaxUsageCount, int UsageCount, IReadOnlyList<string> Promotions, int OrderCount);
public sealed record UpdatePromotionDetails(string Name, DateTime StartsAt, DateTime EndsAt);
public sealed record UpdateVoucher(string Status, DateTime? ExpiresAt, int? MaxUsageCount);
public sealed record CreatePromotion(string Name, DateTime StartsAt, DateTime EndsAt, string? BookId,
    decimal? BookDiscountPercent, decimal? MinimumOrderAmount, decimal? OrderDiscountPercent,
    string? VoucherCode, decimal? VoucherDiscountPercent, int? VoucherMaxUses);
public sealed record ManagerReport(int Days, int TotalOrders, int CompletedOrders, int ProcessingOrders, int PendingOrders, int CancelledOrders,
    decimal Revenue, decimal AverageOrderValue, decimal Discounts, decimal PurchaseSpend,
    decimal EstimatedCostOfGoods, decimal EstimatedGrossProfit, decimal CostCoveragePercent,
    decimal PreviousPeriodRevenue, int PreviousPeriodCompletedOrders,
    decimal? RevenueChangePercent, decimal? CompletedOrdersChangePercent,
    IReadOnlyList<RevenueByDay> RevenueByDay, IReadOnlyList<BestSeller> BestSellers,
    IReadOnlyList<CategorySales> RevenueByCategory, IReadOnlyList<CustomerSales> TopCustomers,
    IReadOnlyList<CategoryInventorySummary> InventoryByCategory,
    DateTime FromDate, DateTime ToDate,
    IReadOnlyList<MonthlyRevenue> RevenueByMonth,
    IReadOnlyList<QuarterlyRevenue> RevenueByQuarter,
    IReadOnlyList<YearlyRevenue> RevenueByYear,
    IReadOnlyList<EmployeeYearSales> RevenueByEmployeeYear,
    IReadOnlyList<ProductQuarterSales> ProductSalesByQuarter);
public sealed record RevenueByDay(DateTime Date, decimal Revenue, decimal PurchaseSpend, int Orders);
public sealed record MonthlyRevenue(int Year, int Month, decimal Revenue, int Orders);
public sealed record QuarterlyRevenue(int Year, int Quarter, decimal Revenue, int Orders);
public sealed record YearlyRevenue(int Year, decimal Revenue, int Orders);
public sealed record EmployeeYearSales(string EmployeeId, string EmployeeName, int Year, int Orders, decimal Revenue);
public sealed record ProductQuarterSales(string BookId, string Title, int Year, int Quarter, int Quantity, decimal Revenue);
public sealed record BestSeller(string BookId, string Title, int Quantity, decimal Revenue);
public sealed record CategorySales(string Category, int Quantity, decimal Revenue);
public sealed record CustomerSales(string CustomerId, string CustomerName, int Orders, int Quantity, decimal Revenue);
public sealed record CategoryInventorySummary(string Category, int BookCount, int StockQuantity, int LowStockBooks, decimal StockValue);
public sealed record ChartSegment(string Label, int Value, string Color);
public sealed record ManagerAuditLog(long Id, DateTime OccurredAt, string ActorName, string? ActorCode, string ActorRole, string Action, string Area, string Resource, int StatusCode, string? IpAddress);
