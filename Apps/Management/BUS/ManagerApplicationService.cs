using TrangSach.DAO.Entities;
using BookStore.Domain.Entities;
using TrangSach.BUS.Services;
using BookStore.Shared.Security;
using BookStore.Shared.Models;
using TrangSach.BUS.Models;
using TrangSach.BUS.Results;

namespace TrangSach.Management.BUS;

/// <summary>Coordinates manager use cases and authorization for the in-process WinForms application.</summary>
public sealed class ManagerApplicationService(DirectCustomerClient session, IPasswordHashingService passwordHasher)
{
    public async Task<ManagerDashboard> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        EnsureAccess("AdminDashboardController", "GetSummary");
        await using var dao = BookStoreDao.Create();
        return await new AdminDashboardService(dao).GetSummaryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManagerAuditLog>> GetAuditLogsAsync(CancellationToken cancellationToken = default)
    {
        EnsureAccess("AdminLogsController", "GetLogs");
        await using var dao = BookStoreDao.Create();
        return await new AdminLogsService(dao).GetLogsAsync(cancellationToken);
    }

    public async Task<ManagerReport> GetReportAsync(int days, CancellationToken cancellationToken = default)
    {
        EnsureAccess("AdminReportsController", "Get");
        await using var dao = BookStoreDao.Create();
        return await new AdminReportsService(dao).GetAsync(days, cancellationToken);
    }

    public async Task<ManagerReport> GetReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        EnsureAccess("AdminReportsController", "Get");
        await using var dao = BookStoreDao.Create();
        return await new AdminReportsService(dao).GetAsync(fromDate, toDate, cancellationToken);
    }
    public async Task<IReadOnlyList<BookItem>> GetManagerBooksAsync(CancellationToken cancellationToken = default)
    {
        EnsureAccess("AdminBooksController", "GetBooks");
        await using var dao = BookStoreDao.Create();
        return await new AdminBookService(dao).GetBooksAsync(cancellationToken);
    }

    public async Task<BookEditDetails> GetManagerBookDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await RunAdminBookOperationAsync("GetBookForEdit", [id],
            service => service.GetBookForEditAsync(id, cancellationToken));
        return RequireValue(result);
    }

    public async Task CreateManagerBookAsync(BookManagementCommand command, CancellationToken cancellationToken = default)
    {
        var result = await RunAdminBookOperationAsync("CreateBook", [command],
            service => service.CreateBookAsync(command, cancellationToken));
        EnsureSucceeded(result);
    }

    public async Task UpdateManagerBookAsync(string id, BookManagementCommand command, CancellationToken cancellationToken = default)
    {
        var result = await RunAdminBookOperationAsync("UpdateBook", [id, command],
            service => service.UpdateBookAsync(id, command, cancellationToken));
        EnsureSucceeded(result);
    }

    public async Task HideManagerBookAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await RunAdminBookOperationAsync("HideBook", [id], service => service.HideBookAsync(id, cancellationToken));
        EnsureSucceeded(result);
    }

    public async Task SetManagerBookVisibilityAsync(string id, bool isActive, CancellationToken cancellationToken = default)
    {
        var result = await RunAdminBookOperationAsync("SetVisibility", [id, isActive],
            service => service.SetVisibilityAsync(id, new BookVisibilityCommand(isActive), cancellationToken));
        EnsureSucceeded(result);
    }

    public async Task<IReadOnlyList<VoucherSummary>> GetVouchersAsync(CancellationToken cancellationToken = default)
    {
        EnsureAccess("AdminVouchersController", "Get");
        await using var dao = BookStoreDao.Create();
        return await new AdminVoucherService(dao).GetAsync(cancellationToken);
    }

    public async Task UpdateVoucherAsync(string id, VoucherUpdateCommand command, CancellationToken cancellationToken = default)
    {
        EnsureAccess("AdminVouchersController", "Update");
        await using var dao = BookStoreDao.Create();
        var result = await new AdminVoucherService(dao).UpdateAsync(id, command, cancellationToken);
        await WriteAuditAsync(dao, "AdminVouchersController", "Update", [id, command],
            result.Status switch { OperationStatus.NotFound => 404, OperationStatus.Invalid => 400, _ => 204 });
        EnsureSucceeded(result);
    }

    public async Task DeleteVoucherAsync(string id, CancellationToken cancellationToken = default)
    {
        EnsureAccess("AdminVouchersController", "Delete");
        await using var dao = BookStoreDao.Create();
        var result = await new AdminVoucherService(dao).DeleteAsync(id, cancellationToken);
        await WriteAuditAsync(dao, "AdminVouchersController", "Delete", [id],
            result.Status switch { OperationStatus.NotFound => 404, OperationStatus.Conflict => 409, _ => 204 });
        EnsureSucceeded(result);
    }

    public async Task<ManagerLookups> GetManagerLookupsAsync(CancellationToken ct = default)
    {
        EnsureAccess("AdminInventoryController", "GetLookups");
        await using var dao = BookStoreDao.Create();
        return await new AdminInventoryService(dao).GetLookupsAsync(ct);
    }

    public async Task<IReadOnlyList<SupplierOption>> GetManagerSuppliersAsync(CancellationToken ct = default)
    {
        EnsureAccess("AdminInventoryController", "GetSuppliers");
        await using var dao = BookStoreDao.Create();
        return await new AdminInventoryService(dao).GetSuppliersAsync(ct);
    }

    public async Task<IReadOnlyList<ManagerReceipt>> GetManagerReceiptsAsync(CancellationToken ct = default)
    {
        EnsureAccess("AdminInventoryController", "GetReceipts");
        await using var dao = BookStoreDao.Create();
        return await new AdminInventoryService(dao).GetReceiptsAsync(ct);
    }

    public async Task CreateManagerSupplierAsync(SupplierCommand command, CancellationToken ct = default) =>
        await RunInventoryWriteAsync("CreateSupplier", [command], service => service.CreateSupplierAsync(command, ct));
    public async Task UpdateManagerSupplierAsync(string id, SupplierCommand command, CancellationToken ct = default) =>
        await RunInventoryWriteAsync("UpdateSupplier", [id, command], service => service.UpdateSupplierAsync(id, command, ct));
    public async Task DeleteManagerSupplierAsync(string id, CancellationToken ct = default) =>
        await RunInventoryWriteAsync("DeleteSupplier", [id], service => service.DeleteSupplierAsync(id, ct));
    public async Task CreateManagerReceiptAsync(ReceiptCommand command, CancellationToken ct = default) =>
        await RunInventoryWriteAsync("CreateReceipt", [command], service => service.CreateReceiptAsync(command, session.AccountId, ct));
    public async Task UpdateManagerReceiptAsync(string id, ReceiptCommand command, CancellationToken ct = default) =>
        await RunInventoryWriteAsync("UpdateReceipt", [id, command], service => service.UpdateReceiptAsync(id, command, ct));
    public async Task CancelManagerReceiptAsync(string id, CancellationToken ct = default) =>
        await RunInventoryWriteAsync("CancelReceipt", [id], service => service.CancelReceiptAsync(id, ct));

    private async Task RunInventoryWriteAsync<T>(string action, object?[] args,
        Func<AdminInventoryService, Task<OperationResult<T>>> operation)
    {
        EnsureAccess("AdminInventoryController", action);
        await using var dao = BookStoreDao.Create();
        var result = await operation(new AdminInventoryService(dao));
        var code = result.Status switch
        {
            OperationStatus.Created => 201,
            OperationStatus.NotFound => 404,
            OperationStatus.Invalid => 400,
            OperationStatus.Conflict => 409,
            OperationStatus.Forbidden => 403,
            _ => 204
        };
        await WriteAuditAsync(dao, "AdminInventoryController", action, args, code);
        EnsureSucceeded(result);
    }

    public async Task<IReadOnlyList<ManagerPromotion>> GetManagerPromotionsAsync(CancellationToken ct = default)
    {
        EnsureAccess("AdminPromotionsController", "Get");
        await using var dao = BookStoreDao.Create();
        return await new AdminPromotionService(dao).GetAsync(ct);
    }
    public async Task CreateManagerPromotionAsync(CreatePromotion command, CancellationToken ct = default) =>
        await RunPromotionWriteAsync("Create", [command], service => service.CreateAsync(command, ct));
    public async Task UpdateManagerPromotionAsync(string id, UpdatePromotionDetails command, CancellationToken ct = default) =>
        await RunPromotionWriteAsync("Update", [id, command], service => service.UpdateAsync(id, command, ct));
    public async Task DeleteManagerPromotionAsync(string id, CancellationToken ct = default) =>
        await RunPromotionWriteAsync("Delete", [id], service => service.DeleteAsync(id, ct));
    public async Task UpdateManagerPromotionRulesAsync(string id, UpdatePromotionRules command, CancellationToken ct = default) =>
        await RunPromotionWriteAsync("UpdateRules", [id, command], service => service.UpdateRulesAsync(id, command, ct));

    private async Task RunPromotionWriteAsync<T>(string action, object?[] args, Func<AdminPromotionService, Task<OperationResult<T>>> operation)
    {
        EnsureAccess("AdminPromotionsController", action);
        await using var dao = BookStoreDao.Create();
        var result = await operation(new AdminPromotionService(dao));
        var code = result.Status switch { OperationStatus.Created => 201, OperationStatus.NotFound => 404, OperationStatus.Invalid => 400, OperationStatus.Conflict => 409, _ => 204 };
        await WriteAuditAsync(dao, "AdminPromotionsController", action, args, code);
        EnsureSucceeded(result);
    }

    public async Task<IReadOnlyList<ManagerOrderRow>> GetManagerOrdersAsync(string? status, CancellationToken ct = default)
    {
        EnsureAccess("AdminOrdersController", "GetOrders");
        await using var dao = BookStoreDao.Create();
        return await new AdminOrderService(dao, new BookPricingService(dao)).GetAsync(status, ct);
    }
    public async Task CreateManagerOrderAsync(CreateAdminOrder command, CancellationToken ct = default) =>
        await RunAdminOrderWriteAsync("CreateOrder", [command], service => service.CreateAsync(command, session.AccountId, ct));
    public async Task UpdateManagerOrderAsync(string id, UpdateAdminOrder command, CancellationToken ct = default) =>
        await RunAdminOrderWriteAsync("UpdateOrder", [id, command], service => service.UpdateAsync(id, command, ct));
    public async Task CancelManagerOrderAsync(string id, CancellationToken ct = default) =>
        await RunAdminOrderWriteAsync("DeleteAsCancelled", [id], service => service.UpdateStatusAsync(id, "Cancelled", session.AccountId, ct));
    public async Task UpdateManagerOrderStatusAsync(string id, string status, CancellationToken ct = default) =>
        await RunAdminOrderWriteAsync("UpdateStatus", [id, status], service => service.UpdateStatusAsync(id, status, session.AccountId, ct));
    public async Task MarkManagerTransferPaidAsync(string id, CancellationToken ct = default) =>
        await RunAdminOrderWriteAsync("MarkBankTransferPaid", [id], service => service.MarkBankTransferPaidAsync(id, ct));
    public async Task MarkManagerRefundCompletedAsync(string id, CancellationToken ct = default) =>
        await RunAdminOrderWriteAsync("MarkRefundCompleted", [id], service => service.MarkRefundCompletedAsync(id, ct));

    private async Task RunAdminOrderWriteAsync<T>(string action, object?[] args, Func<AdminOrderService, Task<OperationResult<T>>> operation)
    {
        EnsureAccess("AdminOrdersController", action);
        await using var dao = BookStoreDao.Create();
        var result = await operation(new AdminOrderService(dao, new BookPricingService(dao)));
        var code = result.Status switch { OperationStatus.Created => 201, OperationStatus.NotFound => 404, OperationStatus.Invalid => 400, OperationStatus.Conflict => 409, _ => 204 };
        await WriteAuditAsync(dao, "AdminOrdersController", action, args, code);
        EnsureSucceeded(result);
    }

    private AdminRecordsService CreateAdminRecordsService(BookStoreDao dao) => new(dao, passwordHasher);
    public async Task<IReadOnlyList<ManagerCategory>> GetManagerCategoriesAsync(CancellationToken ct = default)
    { EnsureAccess("AdminRecordsController", "Categories"); await using var dao = BookStoreDao.Create(); return await CreateAdminRecordsService(dao).GetCategoriesAsync(ct); }
    public async Task<IReadOnlyList<ManagerAuthor>> GetManagerAuthorsAsync(CancellationToken ct = default)
    { EnsureAccess("AdminRecordsController", "Authors"); await using var dao = BookStoreDao.Create(); return await CreateAdminRecordsService(dao).GetAuthorsAsync(ct); }
    public async Task<IReadOnlyList<ManagerPublisher>> GetManagerPublishersAsync(CancellationToken ct = default)
    { EnsureAccess("AdminRecordsController", "Publishers"); await using var dao = BookStoreDao.Create(); return await CreateAdminRecordsService(dao).GetPublishersAsync(ct); }
    public async Task<IReadOnlyList<ManagerCustomer>> GetManagerCustomersAsync(CancellationToken ct = default)
    { EnsureAccess("AdminRecordsController", "Customers"); await using var dao = BookStoreDao.Create(); return await CreateAdminRecordsService(dao).GetCustomersAsync(ct); }
    public async Task<IReadOnlyList<ManagerEmployee>> GetManagerEmployeesAsync(CancellationToken ct = default)
    { EnsureAccess("AdminRecordsController", "Employees"); await using var dao = BookStoreDao.Create(); return await CreateAdminRecordsService(dao).GetEmployeesAsync(ct); }
    public async Task<IReadOnlyList<StaffRoleOption>> GetManagerEmployeeRolesAsync(CancellationToken ct = default)
    { EnsureAccess("AdminRecordsController", "EmployeeRoles"); await using var dao = BookStoreDao.Create(); return await CreateAdminRecordsService(dao).GetEmployeeRolesAsync(ct); }

    public Task CreateManagerCategoryAsync(SaveCategory x, CancellationToken ct = default) => RunRecordWriteAsync("CreateCategory", [x], s => s.CreateCategoryAsync(x, ct));
    public Task UpdateManagerCategoryAsync(string id, SaveCategory x, CancellationToken ct = default) => RunRecordWriteAsync("UpdateCategory", [id, x], s => s.UpdateCategoryAsync(id, x, ct));
    public Task DeleteManagerCategoryAsync(string id, CancellationToken ct = default) => RunRecordWriteAsync("DeleteCategory", [id], s => s.DeleteCategoryAsync(id, ct));
    public Task CreateManagerAuthorAsync(SaveAuthor x, CancellationToken ct = default) => RunRecordWriteAsync("CreateAuthor", [x], s => s.CreateAuthorAsync(x, ct));
    public Task UpdateManagerAuthorAsync(int id, SaveAuthor x, CancellationToken ct = default) => RunRecordWriteAsync("UpdateAuthor", [id, x], s => s.UpdateAuthorAsync(id, x, ct));
    public Task DeleteManagerAuthorAsync(int id, CancellationToken ct = default) => RunRecordWriteAsync("DeleteAuthor", [id], s => s.DeleteAuthorAsync(id, ct));
    public Task CreateManagerPublisherAsync(SavePublisher x, CancellationToken ct = default) => RunRecordWriteAsync("CreatePublisher", [x], s => s.CreatePublisherAsync(x, ct));
    public Task UpdateManagerPublisherAsync(int id, SavePublisher x, CancellationToken ct = default) => RunRecordWriteAsync("UpdatePublisher", [id, x], s => s.UpdatePublisherAsync(id, x, ct));
    public Task DeleteManagerPublisherAsync(int id, CancellationToken ct = default) => RunRecordWriteAsync("DeletePublisher", [id], s => s.DeletePublisherAsync(id, ct));
    public Task CreateManagerCustomerAsync(SaveCustomer x, CancellationToken ct = default) => RunRecordWriteAsync("CreateCustomer", [x], s => s.CreateCustomerAsync(x, ct));
    public Task UpdateManagerCustomerAsync(string id, SaveCustomer x, CancellationToken ct = default) => RunRecordWriteAsync("UpdateCustomer", [id, x], s => s.UpdateCustomerAsync(id, x, ct));
    public Task DeleteManagerCustomerAsync(string id, CancellationToken ct = default) => RunRecordWriteAsync("DeleteCustomer", [id], s => s.DeleteCustomerAsync(id, ct));
    public Task CreateManagerEmployeeAsync(SaveEmployee x, CancellationToken ct = default) => RunRecordWriteAsync("CreateEmployee", [x], s => s.CreateEmployeeAsync(x, ct));
    public Task UpdateManagerEmployeeAsync(string id, SaveEmployee x, CancellationToken ct = default) => RunRecordWriteAsync("UpdateEmployee", [id, x], s => s.UpdateEmployeeAsync(id, x, session.AccountId, ct));
    public Task DeleteManagerEmployeeAsync(string id, CancellationToken ct = default) => RunRecordWriteAsync("DeleteEmployee", [id], s => s.DeleteEmployeeAsync(id, session.AccountId, ct));

    private async Task RunRecordWriteAsync<T>(string action, object?[] args, Func<AdminRecordsService, Task<OperationResult<T>>> operation)
    {
        EnsureAccess("AdminRecordsController", action);
        await using var dao = BookStoreDao.Create();
        var result = await operation(CreateAdminRecordsService(dao));
        var code = result.Status switch { OperationStatus.Created => 201, OperationStatus.NotFound => 404, OperationStatus.Invalid => 400, OperationStatus.Conflict => 409, _ => 204 };
        await WriteAuditAsync(dao, "AdminRecordsController", action, args, code);
        EnsureSucceeded(result);
    }

    private async Task<OperationResult<T>> RunAdminBookOperationAsync<T>(string action, object?[] args,
        Func<AdminBookService, Task<OperationResult<T>>> operation)
    {
        EnsureAccess("AdminBooksController", action);
        await using var dao = BookStoreDao.Create();
        var result = await operation(new AdminBookService(dao));
        var status = result.Status switch
        {
            OperationStatus.Created => 201,
            OperationStatus.NotFound => 404,
            OperationStatus.Invalid => 400,
            OperationStatus.Conflict => 409,
            OperationStatus.Forbidden => 403,
            OperationStatus.Unauthorized => 401,
            _ => 204
        };
        await WriteAuditAsync(dao, "AdminBooksController", action, args, status);
        return result;
    }

    private static T RequireValue<T>(OperationResult<T> result)
    {
        if (!result.Succeeded || result.Value is null) throw new InvalidOperationException(result.Message ?? "Không tìm thấy dữ liệu.");
        return result.Value;
    }

    private static void EnsureSucceeded<T>(OperationResult<T> result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(result.Message ?? "Thao tác không thành công.");
    }

    private void EnsureAccess(string controller, string action)
    {
        var role = session.Role;
        var area = (controller, action) switch
        {
            ("AdminDashboardController", _) => "dashboard",
            ("AdminReportsController", _) => "reports",
            ("AdminLogsController", _) => "logs",
            ("AdminBooksController", "GetBooks") => "productReaders",
            ("AdminBooksController", _) => "books",
            ("AdminRecordsController", "Categories" or "CreateCategory" or "UpdateCategory" or "DeleteCategory"
                or "Authors" or "CreateAuthor" or "UpdateAuthor" or "DeleteAuthor"
                or "Publishers" or "CreatePublisher" or "UpdatePublisher" or "DeletePublisher") => "records",
            ("AdminRecordsController", "Customers" or "CreateCustomer" or "UpdateCustomer" or "DeleteCustomer") => "customers",
            ("AdminRecordsController", "Employees" or "EmployeeRoles" or "CreateEmployee" or "UpdateEmployee" or "DeleteEmployee") => "employees",
            ("AdminOrdersController", _) => "orders",
            ("AdminInventoryController", "GetLookups") => "lookups",
            ("AdminInventoryController", _) => "inventory",
            ("AdminPromotionsController", _) or ("AdminVouchersController", _) => "promotions",
            _ => throw new UnauthorizedAccessException("Chức năng này không được cấp quyền cho tài khoản hiện tại.")
        };
        var permitted = area switch
        {
            "logs" or "employees" => role == StaffRoles.Manager,
            "lookups" => StaffPermissions.CanAccess(role, "inventory") || StaffPermissions.CanAccess(role, "books"),
            "productReaders" => role is StaffRoles.Manager or StaffRoles.Catalog or StaffRoles.Sales or StaffRoles.Inventory or StaffRoles.Marketing,
            _ => StaffPermissions.CanAccess(role, area)
        };
        if (!permitted) throw new UnauthorizedAccessException("Chức danh hiện tại không có quyền sử dụng chức năng này.");
    }

    private async Task WriteAuditAsync(BookStoreDao dao, string controller, string action,
        object?[] args, int status)
    {
        var accountId = session.AccountId;
        var account = accountId is null ? null : await dao.Accounts.FindWithEmployeeAsync(accountId.Value);
        dao.AuditLogs.Add(new SystemAuditLog
        {
            OccurredAt = DateTime.UtcNow,
            ActorAccountId = accountId,
            ActorName = account?.UserName ?? "Tài khoản WinForms",
            ActorCode = account?.Employee?.Id,
            ActorRole = session.Role ?? string.Empty,
            Action = action,
            Area = controller.Replace("Admin", "", StringComparison.Ordinal).Replace("Controller", "", StringComparison.Ordinal),
            Resource = args.FirstOrDefault()?.ToString() ?? string.Empty,
            StatusCode = status,
            IpAddress = "WinForms"
        });
        await dao.SaveChangesAsync();
    }

}
