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

    public List<ManagerAuditLogDto> GetAuditLogs()
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

    public List<BookItemDto> GetManagerBooks()
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
        if (result == null) throw new InvalidOperationException("Không tìm thấy dữ liệu.");
        return result;
    }

    public void CreateManagerBook(BookManagementCommandDto command)
    {
        EnsureAccess("books");
        var dao = BookStoreDao.Create();
        new AdminBookService(dao).CreateBook(command);
    }

    public void UpdateManagerBook(string id, BookManagementCommandDto command)
    {
        EnsureAccess("books");
        var dao = BookStoreDao.Create();
        new AdminBookService(dao).UpdateBook(id, command);
    }

    public void HideManagerBook(string id)
    {
        EnsureAccess("books");
        var dao = BookStoreDao.Create();
        new AdminBookService(dao).HideBook(id);
    }

    public void SetManagerBookVisibility(string id, bool isActive)
    {
        EnsureAccess("books");
        var dao = BookStoreDao.Create();
        new AdminBookService(dao).SetVisibility(id, new BookVisibilityCommandDto(isActive));
    }

    public List<VoucherSummaryDto> GetVouchers()
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        return new AdminVoucherService(dao).GetAllVouchersList();
    }

    public string CreateVoucher(CreateVoucherDto command)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        return new AdminVoucherService(dao).CreateVoucher(command);
    }

    public void UpdateVoucher(string id, VoucherUpdateCommandDto command)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        new AdminVoucherService(dao).Update(id, command);
    }

    public void DeleteVoucher(string id)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        new AdminVoucherService(dao).Delete(id);
    }

    public ManagerLookupsDto GetManagerLookups()
    {
        EnsureAccess("lookups");
        var dao = BookStoreDao.Create();
        return new AdminInventoryService(dao).GetLookups();
    }

    public List<SupplierOptionDto> GetManagerSuppliers()
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        return new AdminInventoryService(dao).GetAllSuppliersList();
    }

    public List<ManagerReceiptDto> GetManagerReceipts()
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        return new AdminInventoryService(dao).GetAllReceiptsList();
    }

    public void CreateManagerSupplier(SupplierCommandDto command)
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        new AdminInventoryService(dao).CreateSupplier(command);
    }

    public void UpdateManagerSupplier(string id, SupplierCommandDto command)
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        new AdminInventoryService(dao).UpdateSupplier(id, command);
    }

    public void DeleteManagerSupplier(string id)
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        new AdminInventoryService(dao).DeleteSupplier(id);
    }

    public void CreateManagerReceipt(ReceiptCommandDto command)
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        new AdminInventoryService(dao).CreateReceipt(command, session.AccountId);
    }

    public void CancelManagerReceipt(string id)
    {
        EnsureAccess("inventory");
        var dao = BookStoreDao.Create();
        new AdminInventoryService(dao).CancelReceipt(id);
    }

    public List<ManagerPromotionDto> GetManagerPromotions()
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        return new AdminPromotionService(dao).GetAllPromotionsList();
    }

    public void CreateManagerPromotion(CreatePromotionDto command)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        new AdminPromotionService(dao).Create(command);
    }

    public void UpdateManagerPromotion(string id, UpdatePromotionDetailsDto command)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        new AdminPromotionService(dao).UpdateDetails(id, command);
    }

    public void UpdateManagerPromotionRules(string id, UpdatePromotionRulesDto command)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        new AdminPromotionService(dao).UpdateRules(id, command);
    }

    public void DeleteManagerPromotion(string id)
    {
        EnsureAccess("promotions");
        var dao = BookStoreDao.Create();
        new AdminPromotionService(dao).Delete(id);
    }

    public List<ManagerOrderRowDto> GetManagerOrders(string? status = null)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        return new AdminOrderService(dao, new BookPricingService(dao)).GetOrdersList(status);
    }

    public void CreateManagerOrder(CreateAdminOrderDto command)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        new AdminOrderService(dao, new BookPricingService(dao)).Create(command, session.AccountId);
    }

    public void UpdateManagerOrder(string id, UpdateAdminOrderDto command)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        new AdminOrderService(dao, new BookPricingService(dao)).Update(id, command);
    }

    public void CancelManagerOrder(string id)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        new AdminOrderService(dao, new BookPricingService(dao)).UpdateStatus(id, "Cancelled", session.AccountId);
    }

    public void UpdateManagerOrderStatus(string id, string status)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        new AdminOrderService(dao, new BookPricingService(dao)).UpdateStatus(id, status, session.AccountId);
    }

    public void MarkManagerTransferPaid(string id)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        new AdminOrderService(dao, new BookPricingService(dao)).MarkBankTransferPaid(id);
    }

    public void MarkManagerRefundCompleted(string id)
    {
        EnsureAccess("orders");
        var dao = BookStoreDao.Create();
        new AdminOrderService(dao, new BookPricingService(dao)).MarkRefundCompleted(id);
    }

    public List<ManagerCategoryDto> GetManagerCategories()
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        return CreateAdminRecordsService(dao).GetAllCategoriesList();
    }

    public List<ManagerAuthorDto> GetManagerAuthors()
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        return CreateAdminRecordsService(dao).GetAllAuthorsList();
    }

    public List<ManagerPublisherDto> GetManagerPublishers()
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        return CreateAdminRecordsService(dao).GetAllPublishersList();
    }

    public List<ManagerCustomerDto> GetManagerCustomers()
    {
        EnsureAccess("customers");
        var dao = BookStoreDao.Create();
        return CreateAdminRecordsService(dao).GetAllCustomersList();
    }

    public List<ManagerEmployeeDto> GetManagerEmployees()
    {
        EnsureAccess("employees");
        var dao = BookStoreDao.Create();
        return CreateAdminRecordsService(dao).GetAllEmployeesList();
    }

    public List<StaffRoleOptionDto> GetManagerEmployeeRoles()
    {
        EnsureAccess("employees");
        var dao = BookStoreDao.Create();
        return CreateAdminRecordsService(dao).GetAllEmployeeRolesList();
    }

    public void CreateManagerCategory(SaveCategoryDto command)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).CreateCategory(command);
    }

    public void UpdateManagerCategory(string id, SaveCategoryDto command)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).UpdateCategory(id, command);
    }

    public void DeleteManagerCategory(string id)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).DeleteCategory(id);
    }

    public void CreateManagerAuthor(SaveAuthorDto command)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).CreateAuthor(command);
    }

    public void UpdateManagerAuthor(int id, SaveAuthorDto command)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).UpdateAuthor(id, command);
    }

    public void DeleteManagerAuthor(int id)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).DeleteAuthor(id);
    }

    public void CreateManagerPublisher(SavePublisherDto command)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).CreatePublisher(command);
    }

    public void UpdateManagerPublisher(int id, SavePublisherDto command)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).UpdatePublisher(id, command);
    }

    public void DeleteManagerPublisher(int id)
    {
        EnsureAccess("records");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).DeletePublisher(id);
    }

    public void CreateManagerCustomer(SaveCustomerDto command)
    {
        EnsureAccess("customers");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).CreateCustomer(command);
    }

    public void UpdateManagerCustomer(string id, SaveCustomerDto command)
    {
        EnsureAccess("customers");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).UpdateCustomer(id, command);
    }

    public void DeleteManagerCustomer(string id)
    {
        EnsureAccess("customers");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).DeleteCustomer(id);
    }

    public void CreateManagerEmployee(SaveEmployeeDto command)
    {
        EnsureAccess("employees");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).CreateEmployee(command);
    }

    public void UpdateManagerEmployee(string id, SaveEmployeeDto command)
    {
        EnsureAccess("employees");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).UpdateEmployee(id, command, session.AccountId);
    }

    public void DeleteManagerEmployee(string id)
    {
        EnsureAccess("employees");
        var dao = BookStoreDao.Create();
        CreateAdminRecordsService(dao).DeleteEmployee(id, session.AccountId);
    }

    private AdminRecordsService CreateAdminRecordsService(BookStoreDao dao) => new(dao, passwordHasher);

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
