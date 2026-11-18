using TrangSach.Shared.Security;
using TrangSach.Shared.DTO;
namespace TrangSach.Management.BUS;

/// <summary>Coordinates manager use cases and authorization for the in-process WinForms application.</summary>
public sealed class ManagementBusinessService(ManagementSessionService session, IPasswordHashingService passwordHasher)
{
    // -----------------------------------------------------------------------
    // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
    // -----------------------------------------------------------------------
    public ManagerDashboardDto GetDashboard()
    {
        EnsureAccess("dashboard");
        var dao = BookStoreDao.Create();
        return new AdminDashboardService(dao).GetSummary();
    }

    public IReadOnlyList<ManagerAuditLogDto> GetAuditLogs()
    {
        EnsureAccess("logs");
        var dao = BookStoreDao.Create();
        return new AdminLogsService(dao).GetLogs();
    }

    public ManagerReportDto GetReport(int days)
    {
        EnsureAccess("reports");
        var dao = BookStoreDao.Create();
        return new AdminReportsService(dao).Get(days);
    }

    public ManagerReportDto GetReport(DateOnly fromDate, DateOnly toDate)
    {
        EnsureAccess("reports");
        var dao = BookStoreDao.Create();
        return new AdminReportsService(dao).Get(fromDate, toDate);
    }

    public IReadOnlyList<BookItemDto> GetManagerBooks()
    {
        EnsureAccess("productReaders");
        var dao = BookStoreDao.Create();
        return new AdminBookService(dao).GetBooks();
    }

    public BookEditDetailsDto GetManagerBookDetails(string id)
    {
        EnsureAccess("books");
        var dao = BookStoreDao.Create();
        var result = new AdminBookService(dao).GetBookForEdit(id);
        return RequireValue(result);
    }

    public void CreateManagerBook(BookManagementCommandDto command)
    {
        EnsureAccess("books");
        var dao = BookStoreDao.Create();
        var result = new AdminBookService(dao).CreateBook(command);
        RequireSuccess(result);
    }

    public void UpdateManagerBook(string id, BookManagementCommandDto command)
    {
        EnsureAccess("books");
        var dao = BookStoreDao.Create();
        var result = new AdminBookService(dao).UpdateBook(id, command);
        RequireSuccess(result);
    }

    public void HideManagerBook(string id)
    {
        EnsureAccess("books");
        var dao = BookStoreDao.Create();
        var result = new AdminBookService(dao).HideBook(id);
        RequireSuccess(result);
    }

    public void SetManagerBookVisibility(string id, bool isActive)
    {
        EnsureAccess("books");
        var dao = BookStoreDao.Create();
        var result = new AdminBookService(dao).SetVisibility(id, new BookVisibilityCommandDto(isActive));
        RequireSuccess(result);
    }

    public IReadOnlyList<VoucherSummaryDto> GetVouchers()
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        return new AdminVoucherService(dao).GetAllVouchersList();
    }

    public void UpdateVoucher(string id, VoucherUpdateCommandDto command)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        var result = new AdminVoucherService(dao).Update(id, command);
        RequireSuccess(result);
    }

    public void DeleteVoucher(string id)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        var result = new AdminVoucherService(dao).Delete(id);
        RequireSuccess(result);
    }

    public ManagerLookupsDto GetManagerLookups()
    {
        EnsureAccess("lookups");
        var dao = BookStoreDao.Create();
        return new AdminInventoryService(dao).GetLookups();
    }

    public IReadOnlyList<SupplierOptionDto> GetManagerSuppliers()
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        return new AdminInventoryService(dao).GetAllSuppliersList();
    }

    public IReadOnlyList<ManagerReceiptDto> GetManagerReceipts()
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        return new AdminInventoryService(dao).GetAllReceiptsList();
    }

    public void CreateManagerSupplier(SupplierCommandDto command)
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        var result = new AdminInventoryService(dao).CreateSupplier(command);
        RequireSuccess(result);
    }

    public void UpdateManagerSupplier(string id, SupplierCommandDto command)
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        var result = new AdminInventoryService(dao).UpdateSupplier(id, command);
        RequireSuccess(result);
    }

    public void DeleteManagerSupplier(string id)
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        var result = new AdminInventoryService(dao).DeleteSupplier(id);
        RequireSuccess(result);
    }

    public void CreateManagerReceipt(ReceiptCommandDto command)
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        var result = new AdminInventoryService(dao).CreateReceipt(command, session.AccountId);
        RequireSuccess(result);
    }

    public void CancelManagerReceipt(string id)
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        var result = new AdminInventoryService(dao).CancelReceipt(id);
        RequireSuccess(result);
    }

    public IReadOnlyList<ManagerPromotionDto> GetManagerPromotions()
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        return new AdminPromotionService(dao).GetAllPromotionsList();
    }

    public void CreateManagerPromotion(CreatePromotionDto command)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        var result = new AdminPromotionService(dao).Create(command);
        RequireSuccess(result);
    }

    public void UpdateManagerPromotion(string id, UpdatePromotionDetailsDto command)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        var result = new AdminPromotionService(dao).UpdateDetails(id, command);
        RequireSuccess(result);
    }

    public void UpdateManagerPromotionRules(string id, UpdatePromotionRulesDto command)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        var result = new AdminPromotionService(dao).UpdateRules(id, command);
        RequireSuccess(result);
    }

    public void DeleteManagerPromotion(string id)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        var result = new AdminPromotionService(dao).Delete(id);
        RequireSuccess(result);
    }

    public IReadOnlyList<ManagerOrderRowDto> GetManagerOrders(string? status = null)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        return new AdminOrderService(dao, new BookPricingService(dao)).GetOrdersList(status);
    }

    public void CreateManagerOrder(CreateAdminOrderDto command)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        var result = new AdminOrderService(dao, new BookPricingService(dao)).Create(command, session.AccountId);
        RequireSuccess(result);
    }

    public void UpdateManagerOrder(string id, UpdateAdminOrderDto command)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        var result = new AdminOrderService(dao, new BookPricingService(dao)).Update(id, command);
        RequireSuccess(result);
    }

    public void CancelManagerOrder(string id)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        var result = new AdminOrderService(dao, new BookPricingService(dao)).UpdateStatus(id, "Cancelled", session.AccountId);
        RequireSuccess(result);
    }

    public void UpdateManagerOrderStatus(string id, string status)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        var result = new AdminOrderService(dao, new BookPricingService(dao)).UpdateStatus(id, status, session.AccountId);
        RequireSuccess(result);
    }

    public void MarkManagerTransferPaid(string id)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        var result = new AdminOrderService(dao, new BookPricingService(dao)).MarkBankTransferPaid(id);
        RequireSuccess(result);
    }

    public void MarkManagerRefundCompleted(string id)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        var result = new AdminOrderService(dao, new BookPricingService(dao)).MarkRefundCompleted(id);
        RequireSuccess(result);
    }

    public IReadOnlyList<ManagerCategoryDto> GetManagerCategories()
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        return CreateAdminRecordsService(dao).GetAllCategoriesList();
    }

    public IReadOnlyList<ManagerAuthorDto> GetManagerAuthors()
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        return CreateAdminRecordsService(dao).GetAllAuthorsList();
    }

    public IReadOnlyList<ManagerPublisherDto> GetManagerPublishers()
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        return CreateAdminRecordsService(dao).GetAllPublishersList();
    }

    public IReadOnlyList<ManagerCustomerDto> GetManagerCustomers()
    {
        EnsureAccess("customers");
        var dao = BookStoreDao.Create();
        return CreateAdminRecordsService(dao).GetAllCustomersList();
    }

    public IReadOnlyList<ManagerEmployeeDto> GetManagerEmployees()
    {
        EnsureAccess("employees");
        var dao = BookStoreDao.Create();
        return CreateAdminRecordsService(dao).GetAllEmployeesList();
    }

    public IReadOnlyList<StaffRoleOptionDto> GetManagerEmployeeRoles()
    {
        EnsureAccess("employees");
        var dao = BookStoreDao.Create();
        return CreateAdminRecordsService(dao).GetAllEmployeeRolesList();
    }

    public void CreateManagerCategory(SaveCategoryDto command)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).CreateCategory(command);
        RequireSuccess(result);
    }

    public void UpdateManagerCategory(string id, SaveCategoryDto command)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).UpdateCategory(id, command);
        RequireSuccess(result);
    }

    public void DeleteManagerCategory(string id)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).DeleteCategory(id);
        RequireSuccess(result);
    }

    public void CreateManagerAuthor(SaveAuthorDto command)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).CreateAuthor(command);
        RequireSuccess(result);
    }

    public void UpdateManagerAuthor(int id, SaveAuthorDto command)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).UpdateAuthor(id, command);
        RequireSuccess(result);
    }

    public void DeleteManagerAuthor(int id)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).DeleteAuthor(id);
        RequireSuccess(result);
    }

    public void CreateManagerPublisher(SavePublisherDto command)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).CreatePublisher(command);
        RequireSuccess(result);
    }

    public void UpdateManagerPublisher(int id, SavePublisherDto command)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).UpdatePublisher(id, command);
        RequireSuccess(result);
    }

    public void DeleteManagerPublisher(int id)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).DeletePublisher(id);
        RequireSuccess(result);
    }

    public void CreateManagerCustomer(SaveCustomerDto command)
    {
        EnsureAccess("customers");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).CreateCustomer(command);
        RequireSuccess(result);
    }

    public void UpdateManagerCustomer(string id, SaveCustomerDto command)
    {
        EnsureAccess("customers");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).UpdateCustomer(id, command);
        RequireSuccess(result);
    }

    public void DeleteManagerCustomer(string id)
    {
        EnsureAccess("customers");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).DeleteCustomer(id);
        RequireSuccess(result);
    }

    public void CreateManagerEmployee(SaveEmployeeDto command)
    {
        EnsureAccess("employees");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).CreateEmployee(command);
        RequireSuccess(result);
    }

    public void UpdateManagerEmployee(string id, SaveEmployeeDto command)
    {
        EnsureAccess("employees");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).UpdateEmployee(id, command, session.AccountId);
        RequireSuccess(result);
    }

    public void DeleteManagerEmployee(string id)
    {
        EnsureAccess("employees");
        var dao = BookStoreDao.Create();
        var result = CreateAdminRecordsService(dao).DeleteEmployee(id, session.AccountId);
        RequireSuccess(result);
    }

    private AdminRecordsService CreateAdminRecordsService(BookStoreDao dao) => new(dao, passwordHasher);

    private static T RequireValue<T>(OperationResult<T> result)
    {
        if (!result.Succeeded || result.Value is null) throw new InvalidOperationException(result.Message ?? "Không tìm thấy dữ liệu.");
        return result.Value;
    }

    private static void RequireSuccess<T>(TrangSach.BUS.OperationResult<T> result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(result.Message ?? "Thao tác không thành công.");
    }

    private void EnsureAccess(string area)
    {
        var role = session.Role;
        var permitted = area switch
        {
            "logs" or "employees" => role == StaffRoles.Manager,
            "lookups" => StaffPermissions.CanAccess(role, "inventory") || StaffPermissions.CanAccess(role, "books"),
            "productReaders" => role is StaffRoles.Manager or StaffRoles.Catalog or StaffRoles.Sales or StaffRoles.Inventory or StaffRoles.Marketing,
            _ => StaffPermissions.CanAccess(role, area)
        };
        if (!permitted) throw new UnauthorizedAccessException("Chức danh hiện tại không có quyền sử dụng chức năng này.");
    }
}
