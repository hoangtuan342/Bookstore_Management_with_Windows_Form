using TrangSach.Shared.DTO;

namespace TrangSach.Management.BUS;

/// <summary>Forwards management application operations to the in-process management service.</summary>
public sealed class ManagementOperationsService(ManagementBusinessService dispatch)
{
    public Task<ManagerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default) => dispatch.GetDashboardAsync(cancellationToken);
    public Task<IReadOnlyList<ManagerAuditLogDto>> GetAuditLogsAsync(CancellationToken cancellationToken = default) => dispatch.GetAuditLogsAsync(cancellationToken);
    public Task<IReadOnlyList<BookItemDto>> GetBooksAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerBooksAsync(cancellationToken);
    public Task<IReadOnlyList<BookItemDto>> GetBooksForPromotionsAsync(CancellationToken cancellationToken = default) => GetBooksAsync(cancellationToken);
    public Task<ManagerLookupsDto> GetLookupsAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerLookupsAsync(cancellationToken);

    public Task<IReadOnlyList<ManagerCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerCategoriesAsync(cancellationToken);
    public Task SaveCategoryAsync(string? id, SaveCategoryDto item, CancellationToken cancellationToken = default) => id is null
        ? dispatch.CreateManagerCategoryAsync(item, cancellationToken) : dispatch.UpdateManagerCategoryAsync(id, item, cancellationToken);
    public Task DeleteCategoryAsync(string id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerCategoryAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManagerAuthorDto>> GetAuthorsAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerAuthorsAsync(cancellationToken);
    public Task SaveAuthorAsync(int? id, SaveAuthorDto item, CancellationToken cancellationToken = default) => id is null
        ? dispatch.CreateManagerAuthorAsync(item, cancellationToken) : dispatch.UpdateManagerAuthorAsync(id.Value, item, cancellationToken);
    public Task DeleteAuthorAsync(int id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerAuthorAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManagerPublisherDto>> GetPublishersAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerPublishersAsync(cancellationToken);
    public Task SavePublisherAsync(int? id, SavePublisherDto item, CancellationToken cancellationToken = default) => id is null
        ? dispatch.CreateManagerPublisherAsync(item, cancellationToken) : dispatch.UpdateManagerPublisherAsync(id.Value, item, cancellationToken);
    public Task DeletePublisherAsync(int id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerPublisherAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManagerCustomerDto>> GetCustomersAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerCustomersAsync(cancellationToken);
    public Task SaveCustomerAsync(string? id, SaveCustomerDto item, CancellationToken cancellationToken = default) =>
        id is null ? dispatch.CreateManagerCustomerAsync(item, cancellationToken) : dispatch.UpdateManagerCustomerAsync(id, item, cancellationToken);
    public Task DeleteCustomerAsync(string id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerCustomerAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManagerEmployeeDto>> GetEmployeesAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerEmployeesAsync(cancellationToken);
    public Task<IReadOnlyList<StaffRoleOptionDto>> GetEmployeeRolesAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerEmployeeRolesAsync(cancellationToken);
    public Task SaveEmployeeAsync(string? id, SaveEmployeeDto item, CancellationToken cancellationToken = default) =>
        id is null ? dispatch.CreateManagerEmployeeAsync(item, cancellationToken) : dispatch.UpdateManagerEmployeeAsync(id, item, cancellationToken);
    public Task DeleteEmployeeAsync(string id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerEmployeeAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManagerOrderRowDto>> GetOrdersAsync(string? status = null, CancellationToken cancellationToken = default) => dispatch.GetManagerOrdersAsync(status, cancellationToken);
    public Task CreateOrderAsync(CreateAdminOrderDto order, CancellationToken cancellationToken = default) => dispatch.CreateManagerOrderAsync(order, cancellationToken);
    public Task UpdateOrderAsync(string id, UpdateAdminOrderDto order, CancellationToken cancellationToken = default) => dispatch.UpdateManagerOrderAsync(id, order, cancellationToken);
    public Task DeleteOrderAsync(string id, CancellationToken cancellationToken = default) => dispatch.CancelManagerOrderAsync(id, cancellationToken);
    public Task UpdateOrderStatusAsync(string id, string status, CancellationToken cancellationToken = default) => dispatch.UpdateManagerOrderStatusAsync(id, status, cancellationToken);
    public Task MarkBankTransferPaidAsync(string id, CancellationToken cancellationToken = default) => dispatch.MarkManagerTransferPaidAsync(id, cancellationToken);
    public Task MarkRefundCompletedAsync(string id, CancellationToken cancellationToken = default) => dispatch.MarkManagerRefundCompletedAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManagerReceiptDto>> GetReceiptsAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerReceiptsAsync(cancellationToken);
    public Task CreateReceiptAsync(CreateReceiptDto receipt, CancellationToken cancellationToken = default) => dispatch.CreateManagerReceiptAsync(ToReceiptCommand(receipt), cancellationToken);
    public Task UpdateReceiptAsync(string id, CreateReceiptDto receipt, CancellationToken cancellationToken = default) => dispatch.UpdateManagerReceiptAsync(id, ToReceiptCommand(receipt), cancellationToken);
    public Task CancelReceiptAsync(string id, CancellationToken cancellationToken = default) => dispatch.CancelManagerReceiptAsync(id, cancellationToken);
    private static ReceiptCommandDto ToReceiptCommand(CreateReceiptDto receipt) => new(receipt.SupplierId,
        receipt.Lines.Select(x => new ReceiptLineCommandDto(x.BookId, x.Quantity, x.UnitCost)).ToArray());

    public Task<IReadOnlyList<ManagerPromotionDto>> GetPromotionsAsync(CancellationToken cancellationToken = default) => dispatch.GetManagerPromotionsAsync(cancellationToken);
    public Task CreatePromotionAsync(CreatePromotionDto promotion, CancellationToken cancellationToken = default) => dispatch.CreateManagerPromotionAsync(promotion, cancellationToken);
    public Task UpdatePromotionAsync(string id, UpdatePromotionDetailsDto item, CancellationToken cancellationToken = default) => dispatch.UpdateManagerPromotionAsync(id, item, cancellationToken);
    public Task UpdatePromotionRulesAsync(string id, UpdatePromotionRulesDto item, CancellationToken cancellationToken = default) => dispatch.UpdateManagerPromotionRulesAsync(id, item, cancellationToken);
    public Task DeletePromotionAsync(string id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerPromotionAsync(id, cancellationToken);

    public async Task<IReadOnlyList<ManagerVoucherDto>> GetVouchersAsync(CancellationToken cancellationToken = default) =>
        (await dispatch.GetVouchersAsync(cancellationToken)).Select(x => new ManagerVoucherDto(x.Id, x.Status, x.ExpiresAt,
            x.MaxUsageCount, x.UsageCount, x.Promotions, x.OrderCount)).ToArray();
    public Task UpdateVoucherAsync(string id, UpdateVoucherDto item, CancellationToken cancellationToken = default) =>
        dispatch.UpdateVoucherAsync(id, new VoucherUpdateCommandDto(item.Status, item.ExpiresAt, item.MaxUsageCount), cancellationToken);
    public Task DeleteVoucherAsync(string id, CancellationToken cancellationToken = default) => dispatch.DeleteVoucherAsync(id, cancellationToken);
    public Task<ManagerReportDto> GetReportAsync(int days = 30, CancellationToken cancellationToken = default) => dispatch.GetReportAsync(days, cancellationToken);
    public Task<ManagerReportDto> GetReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) => dispatch.GetReportAsync(fromDate, toDate, cancellationToken);

    public Task CreateBookAsync(CreateManagerBookDto book, CancellationToken cancellationToken = default) => dispatch.CreateManagerBookAsync(ToBookCommand(book), cancellationToken);
    public async Task<ManagerBookDetailsDto> GetBookDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        var details = await dispatch.GetManagerBookDetailsAsync(id, cancellationToken);
        return new ManagerBookDetailsDto(details.Id, details.Title, details.SalePrice, details.Unit, details.CategoryId,
            details.PublisherId, details.AuthorIds, details.IsActive, details.Description);
    }
    public Task UpdateBookAsync(string id, CreateManagerBookDto book, CancellationToken cancellationToken = default) => dispatch.UpdateManagerBookAsync(id, ToBookCommand(book), cancellationToken);
    public Task HideBookAsync(string id, CancellationToken cancellationToken = default) => dispatch.HideManagerBookAsync(id, cancellationToken);
    public Task SetBookVisibilityAsync(string id, bool isActive, CancellationToken cancellationToken = default) => dispatch.SetManagerBookVisibilityAsync(id, isActive, cancellationToken);
    private static BookManagementCommandDto ToBookCommand(CreateManagerBookDto item) => new(
        item.Title, item.SalePrice, item.Unit, item.CategoryId, item.PublisherId, item.AuthorIds, item.Description);

    public Task CreateSupplierAsync(CreateSupplierDto supplier, CancellationToken cancellationToken = default) => dispatch.CreateManagerSupplierAsync(new SupplierCommandDto(supplier.Name, supplier.PhoneNumber), cancellationToken);
    public Task UpdateSupplierAsync(string id, CreateSupplierDto supplier, CancellationToken cancellationToken = default) => dispatch.UpdateManagerSupplierAsync(id, new SupplierCommandDto(supplier.Name, supplier.PhoneNumber), cancellationToken);
    public Task DeleteSupplierAsync(string id, CancellationToken cancellationToken = default) => dispatch.DeleteManagerSupplierAsync(id, cancellationToken);
}


