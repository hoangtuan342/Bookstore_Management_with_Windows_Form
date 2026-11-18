using TrangSach.Shared.Security;
using TrangSach.Shared.DTO;
namespace TrangSach.Management.BUS;

/// <summary>Coordinates manager use cases and authorization for the in-process WinForms application.</summary>
public sealed class ManagementBusinessService(ManagementSessionService session, IPasswordHashingService passwordHasher)
{
    public async Task<ManagerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        EnsureAccess("dashboard");
        await using var dao = await BookStoreDao.CreateAsync();
        return await new AdminDashboardService(dao).GetSummaryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManagerAuditLogDto>> GetAuditLogsAsync(CancellationToken cancellationToken = default)
    {
        EnsureAccess("logs");
        await using var dao = await BookStoreDao.CreateAsync();
        return await new AdminLogsService(dao).GetLogsAsync(cancellationToken);
    }

    public async Task<ManagerReportDto> GetReportAsync(int days, CancellationToken cancellationToken = default)
    {
        EnsureAccess("reports");
        await using var dao = await BookStoreDao.CreateAsync();
        return await new AdminReportsService(dao).GetAsync(days, cancellationToken);
    }

    public async Task<ManagerReportDto> GetReportAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        EnsureAccess("reports");
        await using var dao = await BookStoreDao.CreateAsync();
        return await new AdminReportsService(dao).GetAsync(fromDate, toDate, cancellationToken);
    }
    public async Task<IReadOnlyList<BookItemDto>> GetManagerBooksAsync(CancellationToken cancellationToken = default)
    {
        EnsureAccess("productReaders");
        await using var dao = await BookStoreDao.CreateAsync();
        return await new AdminBookService(dao).GetBooksAsync(cancellationToken);
    }

    public async Task<BookEditDetailsDto> GetManagerBookDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await RunAdminBookOperationAsync("GetBookForEdit", [id],
            service => service.GetBookForEditAsync(id, cancellationToken));
        return RequireValue(result);
    }

    public async Task CreateManagerBookAsync(BookManagementCommandDto command, CancellationToken cancellationToken = default)
    {
        var result = await RunAdminBookOperationAsync("CreateBook", [command],
            service => service.CreateBookAsync(command, cancellationToken));
        EnsureSucceeded(result);
    }

    public async Task UpdateManagerBookAsync(string id, BookManagementCommandDto command, CancellationToken cancellationToken = default)
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
            service => service.SetVisibilityAsync(id, new BookVisibilityCommandDto(isActive), cancellationToken));
        EnsureSucceeded(result);
    }

    public async Task<IReadOnlyList<VoucherSummaryDto>> GetVouchersAsync(CancellationToken cancellationToken = default)
    {
        EnsureAccess("promotions");
        await using var dao = await BookStoreDao.CreateAsync();
        return await new AdminVoucherService(dao).GetAsync(cancellationToken);
    }

    public async Task UpdateVoucherAsync(string id, VoucherUpdateCommandDto command, CancellationToken cancellationToken = default)
    {
        EnsureAccess("promotions");
        await using var dao = await BookStoreDao.CreateAsync();
        var result = await new AdminVoucherService(dao).UpdateAsync(id, command, cancellationToken);
        await WriteAuditAsync(dao, "promotions", "Update", [id, command],
            result.Status switch { OperationStatus.NotFound => 404, OperationStatus.Invalid => 400, _ => 204 });
        EnsureSucceeded(result);
    }

    public async Task DeleteVoucherAsync(string id, CancellationToken cancellationToken = default)
    {
        EnsureAccess("promotions");
        await using var dao = await BookStoreDao.CreateAsync();
        var result = await new AdminVoucherService(dao).DeleteAsync(id, cancellationToken);
        await WriteAuditAsync(dao, "promotions", "Delete", [id],
            result.Status switch { OperationStatus.NotFound => 404, OperationStatus.Conflict => 409, _ => 204 });
        EnsureSucceeded(result);
    }

    public async Task<ManagerLookupsDto> GetManagerLookupsAsync(CancellationToken ct = default)
    {
        EnsureAccess("lookups");
        await using var dao = await BookStoreDao.CreateAsync();
        return await new AdminInventoryService(dao).GetLookupsAsync(ct);
    }

    public async Task<IReadOnlyList<SupplierOptionDto>> GetManagerSuppliersAsync(CancellationToken ct = default)
    {
        EnsureAccess("inventory");
        await using var dao = await BookStoreDao.CreateAsync();
        return await new AdminInventoryService(dao).GetSuppliersAsync(ct);
    }

    public async Task<IReadOnlyList<ManagerReceiptDto>> GetManagerReceiptsAsync(CancellationToken ct = default)
    {
        EnsureAccess("inventory");
        await using var dao = await BookStoreDao.CreateAsync();
        return await new AdminInventoryService(dao).GetReceiptsAsync(ct);
    }

    public async Task CreateManagerSupplierAsync(SupplierCommandDto command, CancellationToken ct = default) =>
        await RunInventoryWriteAsync("CreateSupplierDto", [command], service => service.CreateSupplierAsync(command, ct));
    public async Task UpdateManagerSupplierAsync(string id, SupplierCommandDto command, CancellationToken ct = default) =>
        await RunInventoryWriteAsync("UpdateSupplier", [id, command], service => service.UpdateSupplierAsync(id, command, ct));
    public async Task DeleteManagerSupplierAsync(string id, CancellationToken ct = default) =>
        await RunInventoryWriteAsync("DeleteSupplier", [id], service => service.DeleteSupplierAsync(id, ct));
    public async Task CreateManagerReceiptAsync(ReceiptCommandDto command, CancellationToken ct = default) =>
        await RunInventoryWriteAsync("CreateReceiptDto", [command], service => service.CreateReceiptAsync(command, session.AccountId, ct));
    public async Task UpdateManagerReceiptAsync(string id, ReceiptCommandDto command, CancellationToken ct = default) =>
        await RunInventoryWriteAsync("UpdateReceipt", [id, command], service => service.UpdateReceiptAsync(id, command, ct));
    public async Task CancelManagerReceiptAsync(string id, CancellationToken ct = default) =>
        await RunInventoryWriteAsync("CancelReceipt", [id], service => service.CancelReceiptAsync(id, ct));

    private async Task RunInventoryWriteAsync<T>(string action, object?[] args,
        Func<AdminInventoryService, Task<OperationResult<T>>> operation)
    {
        EnsureAccess("inventory");
        await using var dao = await BookStoreDao.CreateAsync();
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
        await WriteAuditAsync(dao, "inventory", action, args, code);
        EnsureSucceeded(result);
    }

    public async Task<IReadOnlyList<ManagerPromotionDto>> GetManagerPromotionsAsync(CancellationToken ct = default)
    {
        EnsureAccess("promotions");
        await using var dao = await BookStoreDao.CreateAsync();
        return await new AdminPromotionService(dao).GetAsync(ct);
    }
    public async Task CreateManagerPromotionAsync(CreatePromotionDto command, CancellationToken ct = default) =>
        await RunPromotionWriteAsync("Create", [command], service => service.CreateAsync(command, ct));
    public async Task UpdateManagerPromotionAsync(string id, UpdatePromotionDetailsDto command, CancellationToken ct = default) =>
        await RunPromotionWriteAsync("Update", [id, command], service => service.UpdateAsync(id, command, ct));
    public async Task DeleteManagerPromotionAsync(string id, CancellationToken ct = default) =>
        await RunPromotionWriteAsync("Delete", [id], service => service.DeleteAsync(id, ct));
    public async Task UpdateManagerPromotionRulesAsync(string id, UpdatePromotionRulesDto command, CancellationToken ct = default) =>
        await RunPromotionWriteAsync("UpdateRules", [id, command], service => service.UpdateRulesAsync(id, command, ct));

    private async Task RunPromotionWriteAsync<T>(string action, object?[] args, Func<AdminPromotionService, Task<OperationResult<T>>> operation)
    {
        EnsureAccess("promotions");
        await using var dao = await BookStoreDao.CreateAsync();
        var result = await operation(new AdminPromotionService(dao));
        var code = result.Status switch { OperationStatus.Created => 201, OperationStatus.NotFound => 404, OperationStatus.Invalid => 400, OperationStatus.Conflict => 409, _ => 204 };
        await WriteAuditAsync(dao, "promotions", action, args, code);
        EnsureSucceeded(result);
    }

    public async Task<IReadOnlyList<ManagerOrderRowDto>> GetManagerOrdersAsync(string? status, CancellationToken ct = default)
    {
        EnsureAccess("orders");
        await using var dao = await BookStoreDao.CreateAsync();
        return await new AdminOrderService(dao, new BookPricingService(dao)).GetAsync(status, ct);
    }
    public async Task CreateManagerOrderAsync(CreateAdminOrderDto command, CancellationToken ct = default) =>
        await RunAdminOrderWriteAsync("CreateOrder", [command], service => service.CreateAsync(command, session.AccountId, ct));
    public async Task UpdateManagerOrderAsync(string id, UpdateAdminOrderDto command, CancellationToken ct = default) =>
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
        EnsureAccess("orders");
        await using var dao = await BookStoreDao.CreateAsync();
        var result = await operation(new AdminOrderService(dao, new BookPricingService(dao)));
        var code = result.Status switch { OperationStatus.Created => 201, OperationStatus.NotFound => 404, OperationStatus.Invalid => 400, OperationStatus.Conflict => 409, _ => 204 };
        await WriteAuditAsync(dao, "orders", action, args, code);
        EnsureSucceeded(result);
    }

    private AdminRecordsService CreateAdminRecordsService(BookStoreDao dao) => new(dao, passwordHasher);
    public async Task<IReadOnlyList<ManagerCategoryDto>> GetManagerCategoriesAsync(CancellationToken ct = default)
    { EnsureAccess("records"); await using var dao = await BookStoreDao.CreateAsync(); return await CreateAdminRecordsService(dao).GetCategoriesAsync(ct); }
    public async Task<IReadOnlyList<ManagerAuthorDto>> GetManagerAuthorsAsync(CancellationToken ct = default)
    { EnsureAccess("records"); await using var dao = await BookStoreDao.CreateAsync(); return await CreateAdminRecordsService(dao).GetAuthorsAsync(ct); }
    public async Task<IReadOnlyList<ManagerPublisherDto>> GetManagerPublishersAsync(CancellationToken ct = default)
    { EnsureAccess("records"); await using var dao = await BookStoreDao.CreateAsync(); return await CreateAdminRecordsService(dao).GetPublishersAsync(ct); }
    public async Task<IReadOnlyList<ManagerCustomerDto>> GetManagerCustomersAsync(CancellationToken ct = default)
    { EnsureAccess("customers"); await using var dao = await BookStoreDao.CreateAsync(); return await CreateAdminRecordsService(dao).GetCustomersAsync(ct); }
    public async Task<IReadOnlyList<ManagerEmployeeDto>> GetManagerEmployeesAsync(CancellationToken ct = default)
    { EnsureAccess("employees"); await using var dao = await BookStoreDao.CreateAsync(); return await CreateAdminRecordsService(dao).GetEmployeesAsync(ct); }
    public async Task<IReadOnlyList<StaffRoleOptionDto>> GetManagerEmployeeRolesAsync(CancellationToken ct = default)
    { EnsureAccess("employees"); await using var dao = await BookStoreDao.CreateAsync(); return await CreateAdminRecordsService(dao).GetEmployeeRolesAsync(ct); }

    public Task CreateManagerCategoryAsync(SaveCategoryDto x, CancellationToken ct = default) => RunRecordWriteAsync("CreateCategory", [x], s => s.CreateCategoryAsync(x, ct));
    public Task UpdateManagerCategoryAsync(string id, SaveCategoryDto x, CancellationToken ct = default) => RunRecordWriteAsync("UpdateCategory", [id, x], s => s.UpdateCategoryAsync(id, x, ct));
    public Task DeleteManagerCategoryAsync(string id, CancellationToken ct = default) => RunRecordWriteAsync("DeleteCategory", [id], s => s.DeleteCategoryAsync(id, ct));
    public Task CreateManagerAuthorAsync(SaveAuthorDto x, CancellationToken ct = default) => RunRecordWriteAsync("CreateAuthor", [x], s => s.CreateAuthorAsync(x, ct));
    public Task UpdateManagerAuthorAsync(int id, SaveAuthorDto x, CancellationToken ct = default) => RunRecordWriteAsync("UpdateAuthor", [id, x], s => s.UpdateAuthorAsync(id, x, ct));
    public Task DeleteManagerAuthorAsync(int id, CancellationToken ct = default) => RunRecordWriteAsync("DeleteAuthor", [id], s => s.DeleteAuthorAsync(id, ct));
    public Task CreateManagerPublisherAsync(SavePublisherDto x, CancellationToken ct = default) => RunRecordWriteAsync("CreatePublisher", [x], s => s.CreatePublisherAsync(x, ct));
    public Task UpdateManagerPublisherAsync(int id, SavePublisherDto x, CancellationToken ct = default) => RunRecordWriteAsync("UpdatePublisher", [id, x], s => s.UpdatePublisherAsync(id, x, ct));
    public Task DeleteManagerPublisherAsync(int id, CancellationToken ct = default) => RunRecordWriteAsync("DeletePublisher", [id], s => s.DeletePublisherAsync(id, ct));
    public Task CreateManagerCustomerAsync(SaveCustomerDto x, CancellationToken ct = default) => RunRecordWriteAsync("CreateCustomer", [x], s => s.CreateCustomerAsync(x, ct));
    public Task UpdateManagerCustomerAsync(string id, SaveCustomerDto x, CancellationToken ct = default) => RunRecordWriteAsync("UpdateCustomer", [id, x], s => s.UpdateCustomerAsync(id, x, ct));
    public Task DeleteManagerCustomerAsync(string id, CancellationToken ct = default) => RunRecordWriteAsync("DeleteCustomer", [id], s => s.DeleteCustomerAsync(id, ct));
    public Task CreateManagerEmployeeAsync(SaveEmployeeDto x, CancellationToken ct = default) => RunRecordWriteAsync("CreateEmployee", [x], s => s.CreateEmployeeAsync(x, ct));
    public Task UpdateManagerEmployeeAsync(string id, SaveEmployeeDto x, CancellationToken ct = default) => RunRecordWriteAsync("UpdateEmployee", [id, x], s => s.UpdateEmployeeAsync(id, x, session.AccountId, ct));
    public Task DeleteManagerEmployeeAsync(string id, CancellationToken ct = default) => RunRecordWriteAsync("DeleteEmployee", [id], s => s.DeleteEmployeeAsync(id, session.AccountId, ct));

    private async Task RunRecordWriteAsync<T>(string action, object?[] args, Func<AdminRecordsService, Task<OperationResult<T>>> operation)
    {
        var area = GetRecordArea(action);
        EnsureAccess(area);
        await using var dao = await BookStoreDao.CreateAsync();
        var result = await operation(CreateAdminRecordsService(dao));
        var code = result.Status switch { OperationStatus.Created => 201, OperationStatus.NotFound => 404, OperationStatus.Invalid => 400, OperationStatus.Conflict => 409, _ => 204 };
        await WriteAuditAsync(dao, area, action, args, code);
        EnsureSucceeded(result);
    }

    private async Task<OperationResult<T>> RunAdminBookOperationAsync<T>(string action, object?[] args,
        Func<AdminBookService, Task<OperationResult<T>>> operation)
    {
        EnsureAccess("books");
        await using var dao = await BookStoreDao.CreateAsync();
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
        await WriteAuditAsync(dao, "books", action, args, status);
        return result;
    }

    private static T RequireValue<T>(OperationResult<T> result)
    {
        if (!result.Succeeded || result.Value is null) throw new InvalidOperationException(result.Message ?? "KhÃƒÂ´ng tÃƒÂ¬m thÃ¡ÂºÂ¥y dÃ¡Â»Â¯ liÃ¡Â»â€¡u.");
        return result.Value;
    }

    private static void EnsureSucceeded<T>(OperationResult<T> result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(result.Message ?? "Thao tÃƒÂ¡c khÃƒÂ´ng thÃƒÂ nh cÃƒÂ´ng.");
    }

    private static string GetRecordArea(string action) => action switch
    {
        "CreateCustomer" or "UpdateCustomer" or "DeleteCustomer" => "customers",
        "CreateEmployee" or "UpdateEmployee" or "DeleteEmployee" => "employees",
        "CreateCategory" or "UpdateCategory" or "DeleteCategory"
            or "CreateAuthor" or "UpdateAuthor" or "DeleteAuthor"
            or "CreatePublisher" or "UpdatePublisher" or "DeletePublisher" => "records",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Thao tÃ¡c há»“ sÆ¡ khÃ´ng Ä‘Æ°á»£c há»— trá»£.")
    };

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
        if (!permitted) throw new UnauthorizedAccessException("Chá»©c danh hiá»‡n táº¡i khÃ´ng cÃ³ quyá»n sá»­ dá»¥ng chá»©c nÄƒng nÃ y.");
    }
    private async Task WriteAuditAsync(BookStoreDao dao, string area, string action,
        object?[] args, int status)
    {
        var accountId = session.AccountId;
        var command = new AuditLogCommandDto(
            accountId,
            null,
            session.Role ?? string.Empty,
            action,
            area,
            args.FirstOrDefault()?.ToString() ?? string.Empty,
            status,
            "WinForms");
        await dao.Users.InsertAuditLogAsync(command);
    }
}
