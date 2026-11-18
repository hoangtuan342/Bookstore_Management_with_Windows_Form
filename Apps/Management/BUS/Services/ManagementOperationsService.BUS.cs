using TrangSach.Shared.DTO;

namespace TrangSach.Management.BUS;

/// <summary>Forwards management application operations to the in-process management service.</summary>
public sealed class ManagementOperationsService(ManagementBusinessService dispatch)
{
    // Synchronous operations forwarding
    public ManagerDashboardDto GetDashboard() => dispatch.GetDashboard();
    public IReadOnlyList<ManagerAuditLogDto> GetAuditLogs() => dispatch.GetAuditLogs();
    public IReadOnlyList<BookItemDto> GetBooks() => dispatch.GetManagerBooks();
    public IReadOnlyList<BookItemDto> GetBooksForPromotions() => GetBooks();
    public ManagerLookupsDto GetLookups() => dispatch.GetManagerLookups();

    public IReadOnlyList<ManagerCategoryDto> GetCategories() => dispatch.GetManagerCategories();
    public void SaveCategory(string? id, SaveCategoryDto item) { if (id is null) dispatch.CreateManagerCategory(item); else dispatch.UpdateManagerCategory(id, item); }
    public void DeleteCategory(string id) => dispatch.DeleteManagerCategory(id);

    public IReadOnlyList<ManagerAuthorDto> GetAuthors() => dispatch.GetManagerAuthors();
    public void SaveAuthor(int? id, SaveAuthorDto item) { if (id is null) dispatch.CreateManagerAuthor(item); else dispatch.UpdateManagerAuthor(id.Value, item); }
    public void DeleteAuthor(int id) => dispatch.DeleteManagerAuthor(id);

    public IReadOnlyList<ManagerPublisherDto> GetPublishers() => dispatch.GetManagerPublishers();
    public void SavePublisher(int? id, SavePublisherDto item) { if (id is null) dispatch.CreateManagerPublisher(item); else dispatch.UpdateManagerPublisher(id.Value, item); }
    public void DeletePublisher(int id) => dispatch.DeleteManagerPublisher(id);

    public IReadOnlyList<ManagerCustomerDto> GetCustomers() => dispatch.GetManagerCustomers();
    public void SaveCustomer(string? id, SaveCustomerDto item) { if (id is null) dispatch.CreateManagerCustomer(item); else dispatch.UpdateManagerCustomer(id, item); }
    public void DeleteCustomer(string id) => dispatch.DeleteManagerCustomer(id);

    public IReadOnlyList<ManagerEmployeeDto> GetEmployees() => dispatch.GetManagerEmployees();
    public IReadOnlyList<StaffRoleOptionDto> GetEmployeeRoles() => dispatch.GetManagerEmployeeRoles();
    public void SaveEmployee(string? id, SaveEmployeeDto item) { if (id is null) dispatch.CreateManagerEmployee(item); else dispatch.UpdateManagerEmployee(id, item); }
    public void DeleteEmployee(string id) => dispatch.DeleteManagerEmployee(id);

    public IReadOnlyList<ManagerOrderRowDto> GetOrders(string? status = null) => dispatch.GetManagerOrders(status);
    public void CreateOrder(CreateAdminOrderDto order) => dispatch.CreateManagerOrder(order);
    public void UpdateOrder(string id, UpdateAdminOrderDto order) => dispatch.UpdateManagerOrder(id, order);
    public void DeleteOrder(string id) => dispatch.CancelManagerOrder(id);
    public void UpdateOrderStatus(string id, string status) => dispatch.UpdateManagerOrderStatus(id, status);
    public void MarkBankTransferPaid(string id) => dispatch.MarkManagerTransferPaid(id);
    public void MarkRefundCompleted(string id) => dispatch.MarkManagerRefundCompleted(id);

    public IReadOnlyList<ManagerReceiptDto> GetReceipts() => dispatch.GetManagerReceipts();
    public void CreateReceipt(CreateReceiptDto receipt) => dispatch.CreateManagerReceipt(ToReceiptCommand(receipt));
    public void CancelReceipt(string id) => dispatch.CancelManagerReceipt(id);

    public IReadOnlyList<ManagerPromotionDto> GetPromotions() => dispatch.GetManagerPromotions();
    public void CreatePromotion(CreatePromotionDto promotion) => dispatch.CreateManagerPromotion(promotion);
    public void UpdatePromotion(string id, UpdatePromotionDetailsDto item) => dispatch.UpdateManagerPromotion(id, item);
    public void UpdatePromotionRules(string id, UpdatePromotionRulesDto item) => dispatch.UpdateManagerPromotionRules(id, item);
    public void DeletePromotion(string id) => dispatch.DeleteManagerPromotion(id);

    public IReadOnlyList<ManagerVoucherDto> GetVouchers() =>
        dispatch.GetVouchers().Select(x => new ManagerVoucherDto(x.Id, x.Status, x.ExpiresAt,
            x.MaxUsageCount, x.UsageCount, x.Promotions, x.OrderCount)).ToArray();
    public void UpdateVoucher(string id, UpdateVoucherDto item) =>
        dispatch.UpdateVoucher(id, new VoucherUpdateCommandDto(item.Status, item.ExpiresAt, item.MaxUsageCount));
    public void DeleteVoucher(string id) => dispatch.DeleteVoucher(id);
    public ManagerReportDto GetReport(int days = 30) => dispatch.GetReport(days);
    public ManagerReportDto GetReport(DateOnly fromDate, DateOnly toDate) => dispatch.GetReport(fromDate, toDate);

    public void CreateBook(CreateManagerBookDto book) => dispatch.CreateManagerBook(ToBookCommand(book));
    public ManagerBookDetailsDto GetBookDetails(string id)
    {
        var details = dispatch.GetManagerBookDetails(id);
        return new ManagerBookDetailsDto(details.Id, details.Title, details.SalePrice, details.Unit, details.CategoryId,
            details.PublisherId, details.AuthorIds, details.IsActive, details.Description);
    }
    public void UpdateBook(string id, CreateManagerBookDto book) => dispatch.UpdateManagerBook(id, ToBookCommand(book));
    public void HideBook(string id) => dispatch.HideManagerBook(id);
    public void SetBookVisibility(string id, bool isActive) => dispatch.SetManagerBookVisibility(id, isActive);

    public void CreateSupplier(CreateSupplierDto supplier) => dispatch.CreateManagerSupplier(new SupplierCommandDto(supplier.Name, supplier.PhoneNumber));
    public void UpdateSupplier(string id, CreateSupplierDto supplier) => dispatch.UpdateManagerSupplier(id, new SupplierCommandDto(supplier.Name, supplier.PhoneNumber));
    public void DeleteSupplier(string id) => dispatch.DeleteManagerSupplier(id);


    private static ReceiptCommandDto ToReceiptCommand(CreateReceiptDto receipt) => new(receipt.SupplierId,
        receipt.Lines.Select(x => new ReceiptLineCommandDto(x.BookId, x.Quantity, x.UnitCost)).ToArray());

    private static BookManagementCommandDto ToBookCommand(CreateManagerBookDto item) => new(
        item.Title, item.SalePrice, item.Unit, item.CategoryId, item.PublisherId, item.AuthorIds, item.Description);
}
