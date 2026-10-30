using TrangSach.BUS.Models;
using BookStore.Shared.Models;

namespace TrangSach.BUS;

/// <summary>Admin operations call the existing EF-backed actions locally in-process, not over HTTP.</summary>
public sealed class DirectManagerClient(ManagerApplicationService dispatch)
{
    public Task<ManagerDashboard> GetDashboardAsync(CancellationToken cancellationToken = default) => dispatch.GetDashboardAsync(cancellationToken);
    public Task<IReadOnlyList<ManagerAuditLog>> GetAuditLogsAsync(CancellationToken cancellationToken = default) => dispatch.GetAuditLogsAsync(cancellationToken);
    public Task<IReadOnlyList<BookItem>> GetBooksAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerBooksAsync(cancellationToken);
    public Task<IReadOnlyList<BookItem>> GetBooksForPromotionsAsync(CancellationToken cancellationToken = default) => GetBooksAsync(cancellationToken);
    public Task<ManagerLookups> GetLookupsAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerLookupsAsync(cancellationToken);

    public Task<IReadOnlyList<ManagerCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerCategoriesAsync(cancellationToken);
    public Task SaveCategoryAsync(string? id, SaveCategory item, CancellationToken cancellationToken = default) => id is null
        ? dispatch.CreateManagerCategoryAsync(item, cancellationToken) : dispatch.UpdateManagerCategoryAsync(id, item, cancellationToken);
    public Task DeleteCategoryAsync(string id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerCategoryAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManagerAuthor>> GetAuthorsAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerAuthorsAsync(cancellationToken);
    public Task SaveAuthorAsync(int? id, SaveAuthor item, CancellationToken cancellationToken = default) => id is null
        ? dispatch.CreateManagerAuthorAsync(item, cancellationToken) : dispatch.UpdateManagerAuthorAsync(id.Value, item, cancellationToken);
    public Task DeleteAuthorAsync(int id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerAuthorAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManagerPublisher>> GetPublishersAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerPublishersAsync(cancellationToken);
    public Task SavePublisherAsync(int? id, SavePublisher item, CancellationToken cancellationToken = default) => id is null
        ? dispatch.CreateManagerPublisherAsync(item, cancellationToken) : dispatch.UpdateManagerPublisherAsync(id.Value, item, cancellationToken);
    public Task DeletePublisherAsync(int id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerPublisherAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManagerCustomer>> GetCustomersAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerCustomersAsync(cancellationToken);
    public Task SaveCustomerAsync(string? id, SaveCustomer item, CancellationToken cancellationToken = default) =>
        id is null ? dispatch.CreateManagerCustomerAsync(item, cancellationToken) : dispatch.UpdateManagerCustomerAsync(id, item, cancellationToken);
    public Task DeleteCustomerAsync(string id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerCustomerAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManagerEmployee>> GetEmployeesAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerEmployeesAsync(cancellationToken);
    public Task<IReadOnlyList<StaffRoleOption>> GetEmployeeRolesAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerEmployeeRolesAsync(cancellationToken);
    public Task SaveEmployeeAsync(string? id, SaveEmployee item, CancellationToken cancellationToken = default) =>
        id is null ? dispatch.CreateManagerEmployeeAsync(item, cancellationToken) : dispatch.UpdateManagerEmployeeAsync(id, item, cancellationToken);
    public Task DeleteEmployeeAsync(string id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerEmployeeAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManagerOrderRow>> GetOrdersAsync(string? status = null, CancellationToken cancellationToken = default) => dispatch.GetManagerOrdersAsync(status, cancellationToken);
    public Task CreateOrderAsync(CreateAdminOrder order, CancellationToken cancellationToken = default) => dispatch.CreateManagerOrderAsync(order, cancellationToken);
    public Task UpdateOrderAsync(string id, UpdateAdminOrder order, CancellationToken cancellationToken = default) => dispatch.UpdateManagerOrderAsync(id, order, cancellationToken);
    public Task DeleteOrderAsync(string id, CancellationToken cancellationToken = default) => dispatch.CancelManagerOrderAsync(id, cancellationToken);
    public Task UpdateOrderStatusAsync(string id, string status, CancellationToken cancellationToken = default) => dispatch.UpdateManagerOrderStatusAsync(id, status, cancellationToken);
    public Task MarkBankTransferPaidAsync(string id, CancellationToken cancellationToken = default) => dispatch.MarkManagerTransferPaidAsync(id, cancellationToken);
    public Task MarkRefundCompletedAsync(string id, CancellationToken cancellationToken = default) => dispatch.MarkManagerRefundCompletedAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManagerReceipt>> GetReceiptsAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerReceiptsAsync(cancellationToken);
    public Task CreateReceiptAsync(CreateReceipt receipt, CancellationToken cancellationToken = default) => dispatch.CreateManagerReceiptAsync(ToReceiptCommand(receipt), cancellationToken);
    public Task UpdateReceiptAsync(string id, CreateReceipt receipt, CancellationToken cancellationToken = default) => dispatch.UpdateManagerReceiptAsync(id, ToReceiptCommand(receipt), cancellationToken);
    public Task CancelReceiptAsync(string id, CancellationToken cancellationToken = default) => dispatch.CancelManagerReceiptAsync(id, cancellationToken);
    private static ReceiptCommand ToReceiptCommand(CreateReceipt receipt) => new(receipt.SupplierId,
        receipt.Lines.Select(x => new ReceiptLineCommand(x.BookId, x.Quantity, x.UnitCost)).ToArray());

    public Task<IReadOnlyList<ManagerPromotion>> GetPromotionsAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerPromotionsAsync(cancellationToken);
    public Task CreatePromotionAsync(CreatePromotion promotion, CancellationToken cancellationToken = default) => dispatch.CreateManagerPromotionAsync(promotion, cancellationToken);
    public Task UpdatePromotionAsync(string id, UpdatePromotionDetails item, CancellationToken cancellationToken = default) => dispatch.UpdateManagerPromotionAsync(id, item, cancellationToken);
    public Task UpdatePromotionRulesAsync(string id, UpdatePromotionRules item, CancellationToken cancellationToken = default) => dispatch.UpdateManagerPromotionRulesAsync(id, item, cancellationToken);
    public Task DeletePromotionAsync(string id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerPromotionAsync(id, cancellationToken);

    public async Task<IReadOnlyList<ManagerVoucher>> GetVouchersAsync(CancellationToken cancellationToken = default) =>
        (await dispatch.GetVouchersAsync(cancellationToken)).Select(x => new ManagerVoucher(x.Id, x.Status, x.ExpiresAt,
            x.MaxUsageCount, x.UsageCount, x.Promotions, x.OrderCount)).ToArray();
    public Task UpdateVoucherAsync(string id, UpdateVoucher item, CancellationToken cancellationToken = default) =>
        dispatch.UpdateVoucherAsync(id, new VoucherUpdateCommand(item.Status, item.ExpiresAt, item.MaxUsageCount), cancellationToken);
    public Task DeleteVoucherAsync(string id, CancellationToken cancellationToken = default) => dispatch.DeleteVoucherAsync(id, cancellationToken);
    public Task<ManagerReport> GetReportAsync(int days = 30, CancellationToken cancellationToken = default) => dispatch.GetReportAsync(days, cancellationToken);
    public Task<ManagerReport> GetReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) => dispatch.GetReportAsync(fromDate, toDate, cancellationToken);

    public Task CreateBookAsync(CreateManagerBook book, CancellationToken cancellationToken = default) => dispatch.CreateManagerBookAsync(ToBookCommand(book), cancellationToken);
    public async Task<ManagerBookDetails> GetBookDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        var details = await dispatch.GetManagerBookDetailsAsync(id, cancellationToken);
        return new ManagerBookDetails(details.Id, details.Title, details.SalePrice, details.Unit, details.CategoryId,
            details.PublisherId, details.AuthorIds, details.IsActive);
    }
    public Task UpdateBookAsync(string id, CreateManagerBook book, CancellationToken cancellationToken = default) => dispatch.UpdateManagerBookAsync(id, ToBookCommand(book), cancellationToken);
    public Task HideBookAsync(string id, CancellationToken cancellationToken = default) => dispatch.HideManagerBookAsync(id, cancellationToken);
    public Task SetBookVisibilityAsync(string id, bool isActive, CancellationToken cancellationToken = default) => dispatch.SetManagerBookVisibilityAsync(id, isActive, cancellationToken);
    private static BookManagementCommand ToBookCommand(CreateManagerBook item) => new(
        item.Title, item.SalePrice, item.Unit, item.CategoryId, item.PublisherId, item.AuthorIds);

    public Task CreateSupplierAsync(CreateSupplier supplier, CancellationToken cancellationToken = default) => dispatch.CreateManagerSupplierAsync(new SupplierCommand(supplier.Name, supplier.PhoneNumber), cancellationToken);
    public Task UpdateSupplierAsync(string id, CreateSupplier supplier, CancellationToken cancellationToken = default) => dispatch.UpdateManagerSupplierAsync(id, new SupplierCommand(supplier.Name, supplier.PhoneNumber), cancellationToken);
    public Task DeleteSupplierAsync(string id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerSupplierAsync(id, cancellationToken);
}
