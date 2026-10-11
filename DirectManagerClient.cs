using BookStore.Api.Contracts;
using BookStore.Api.Controllers;
using BookStore.Shared.Models;

namespace TrangSach.WinForms;

/// <summary>Admin operations call the existing EF-backed actions locally in-process, not over HTTP.</summary>
public sealed class DirectManagerClient(DirectControllerDispatcher dispatch)
{
    private async Task<T> Read<T>(string controller, string action, params object?[] args) where T : class =>
        await dispatch.InvokeAsync<T>(controller, action, args)
        ?? throw new InvalidOperationException("Không nhận được dữ liệu từ database.");

    private async Task Write(string controller, string action, params object?[] args) =>
        _ = await dispatch.InvokeAsync<object>(controller, action, args);

    public Task<ManagerDashboard> GetDashboardAsync(CancellationToken cancellationToken = default) => Read<ManagerDashboard>(nameof(AdminDashboardController), "GetSummary");
    public Task<IReadOnlyList<ManagerAuditLog>> GetAuditLogsAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<ManagerAuditLog>>(nameof(AdminLogsController), "GetLogs");
    public Task<IReadOnlyList<BookItem>> GetBooksAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<BookItem>>(nameof(AdminBooksController), "GetBooks");
    public Task<IReadOnlyList<BookItem>> GetBooksForPromotionsAsync(CancellationToken cancellationToken = default) => GetBooksAsync(cancellationToken);
    public Task<ManagerLookups> GetLookupsAsync(CancellationToken cancellationToken = default) => Read<ManagerLookups>(nameof(AdminInventoryController), "GetLookups");

    public Task<IReadOnlyList<ManagerCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<ManagerCategory>>(nameof(AdminRecordsController), "Categories");
    public Task SaveCategoryAsync(string? id, SaveCategory item, CancellationToken cancellationToken = default) => id is null
        ? Write(nameof(AdminRecordsController), "CreateCategory", new CategoryRequest { Id = item.Id, Name = item.Name })
        : Write(nameof(AdminRecordsController), "UpdateCategory", id, new CategoryRequest { Id = item.Id, Name = item.Name });
    public Task DeleteCategoryAsync(string id, CancellationToken cancellationToken = default) => Write(nameof(AdminRecordsController), "DeleteCategory", id);

    public Task<IReadOnlyList<ManagerAuthor>> GetAuthorsAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<ManagerAuthor>>(nameof(AdminRecordsController), "Authors");
    public Task SaveAuthorAsync(int? id, SaveAuthor item, CancellationToken cancellationToken = default) => id is null
        ? Write(nameof(AdminRecordsController), "CreateAuthor", new AuthorRequest { Name = item.Name, Biography = item.Biography })
        : Write(nameof(AdminRecordsController), "UpdateAuthor", id.Value, new AuthorRequest { Name = item.Name, Biography = item.Biography });
    public Task DeleteAuthorAsync(int id, CancellationToken cancellationToken = default) => Write(nameof(AdminRecordsController), "DeleteAuthor", id);

    public Task<IReadOnlyList<ManagerPublisher>> GetPublishersAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<ManagerPublisher>>(nameof(AdminRecordsController), "Publishers");
    public Task SavePublisherAsync(int? id, SavePublisher item, CancellationToken cancellationToken = default) => id is null
        ? Write(nameof(AdminRecordsController), "CreatePublisher", new PublisherRequest { Name = item.Name, Address = item.Address })
        : Write(nameof(AdminRecordsController), "UpdatePublisher", id.Value, new PublisherRequest { Name = item.Name, Address = item.Address });
    public Task DeletePublisherAsync(int id, CancellationToken cancellationToken = default) => Write(nameof(AdminRecordsController), "DeletePublisher", id);

    public Task<IReadOnlyList<ManagerCustomer>> GetCustomersAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<ManagerCustomer>>(nameof(AdminRecordsController), "Customers");
    public Task SaveCustomerAsync(string? id, SaveCustomer item, CancellationToken cancellationToken = default) =>
        id is null ? Write(nameof(AdminRecordsController), "CreateCustomer", ToCustomerRequest(item))
                   : Write(nameof(AdminRecordsController), "UpdateCustomer", id, ToCustomerRequest(item));
    public Task DeleteCustomerAsync(string id, CancellationToken cancellationToken = default) => Write(nameof(AdminRecordsController), "DeleteCustomer", id);
    private static CustomerRequest ToCustomerRequest(SaveCustomer item) => new()
    {
        Id = item.Id, FullName = item.FullName, PhoneNumber = item.PhoneNumber, Address = item.Address,
        UserName = item.UserName, Email = item.Email, Password = item.Password, AccountStatus = item.AccountStatus
    };

    public Task<IReadOnlyList<ManagerEmployee>> GetEmployeesAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<ManagerEmployee>>(nameof(AdminRecordsController), "Employees");
    public Task<IReadOnlyList<StaffRoleOption>> GetEmployeeRolesAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<StaffRoleOption>>(nameof(AdminRecordsController), "EmployeeRoles");
    public Task SaveEmployeeAsync(string? id, SaveEmployee item, CancellationToken cancellationToken = default) =>
        id is null ? Write(nameof(AdminRecordsController), "CreateEmployee", ToEmployeeRequest(item))
                   : Write(nameof(AdminRecordsController), "UpdateEmployee", id, ToEmployeeRequest(item));
    public Task DeleteEmployeeAsync(string id, CancellationToken cancellationToken = default) => Write(nameof(AdminRecordsController), "DeleteEmployee", id);
    private static EmployeeRequest ToEmployeeRequest(SaveEmployee item) => new()
    {
        Id = item.Id, LastName = item.LastName, FirstName = item.FirstName, BirthDate = item.BirthDate,
        AvatarPath = item.AvatarPath, Address = item.Address, AccountStatus = item.AccountStatus, BaseSalary = item.BaseSalary,
        UserName = item.UserName, Email = item.Email, Password = item.Password, AccountLoginStatus = item.AccountLoginStatus, RoleName = item.RoleName
    };

    public Task<IReadOnlyList<ManagerOrderRow>> GetOrdersAsync(string? status = null, CancellationToken cancellationToken = default) => Read<IReadOnlyList<ManagerOrderRow>>(nameof(AdminOrdersController), "GetOrders", status);
    public Task CreateOrderAsync(CreateAdminOrder order, CancellationToken cancellationToken = default) => Write(nameof(AdminOrdersController), "CreateOrder", new AdminCreateOrderRequest
    {
        CustomerId = order.CustomerId, ShippingAddress = order.ShippingAddress, ReceiverPhone = order.ReceiverPhone, PaymentMethod = order.PaymentMethod,
        Lines = order.Lines.Select(x => new AdminCreateOrderLineRequest { BookId = x.BookId, Quantity = x.Quantity }).ToArray()
    });
    public Task UpdateOrderAsync(string id, UpdateAdminOrder order, CancellationToken cancellationToken = default) => Write(nameof(AdminOrdersController), "UpdateOrder", id,
        new UpdateAdminOrderRequest { ShippingAddress = order.ShippingAddress, ReceiverPhone = order.ReceiverPhone, PaymentMethod = order.PaymentMethod });
    public Task DeleteOrderAsync(string id, CancellationToken cancellationToken = default) => Write(nameof(AdminOrdersController), "DeleteAsCancelled", id);
    public Task UpdateOrderStatusAsync(string id, string status, CancellationToken cancellationToken = default) => Write(nameof(AdminOrdersController), "UpdateStatus", id, new UpdateOrderStatusRequest { Status = status });
    public Task MarkBankTransferPaidAsync(string id, CancellationToken cancellationToken = default) => Write(nameof(AdminOrdersController), "MarkBankTransferPaid", id);
    public Task MarkRefundCompletedAsync(string id, CancellationToken cancellationToken = default) => Write(nameof(AdminOrdersController), "MarkRefundCompleted", id);

    public Task<IReadOnlyList<ManagerReceipt>> GetReceiptsAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<ManagerReceipt>>(nameof(AdminInventoryController), "GetReceipts");
    public Task CreateReceiptAsync(CreateReceipt receipt, CancellationToken cancellationToken = default) => Write(nameof(AdminInventoryController), "CreateReceipt", new PurchaseReceiptRequest
    {
        SupplierId = receipt.SupplierId,
        Lines = receipt.Lines.Select(x => new PurchaseReceiptLineRequest { BookId = x.BookId, Quantity = x.Quantity, UnitCost = x.UnitCost }).ToArray()
    });
    public Task UpdateReceiptAsync(string id, CreateReceipt receipt, CancellationToken cancellationToken = default) => Write(nameof(AdminInventoryController), "UpdateReceipt", id, new PurchaseReceiptRequest
    {
        SupplierId = receipt.SupplierId,
        Lines = receipt.Lines.Select(x => new PurchaseReceiptLineRequest { BookId = x.BookId, Quantity = x.Quantity, UnitCost = x.UnitCost }).ToArray()
    });
    public Task CancelReceiptAsync(string id, CancellationToken cancellationToken = default) => Write(nameof(AdminInventoryController), "CancelReceipt", id);

    public Task<IReadOnlyList<ManagerPromotion>> GetPromotionsAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<ManagerPromotion>>(nameof(AdminPromotionsController), "Get");
    public Task CreatePromotionAsync(CreatePromotion promotion, CancellationToken cancellationToken = default) => Write(nameof(AdminPromotionsController), "Create", new CreatePromotionRequest
    {
        Name = promotion.Name, StartsAt = promotion.StartsAt, EndsAt = promotion.EndsAt, BookId = promotion.BookId,
        BookDiscountPercent = promotion.BookDiscountPercent, MinimumOrderAmount = promotion.MinimumOrderAmount,
        OrderDiscountPercent = promotion.OrderDiscountPercent, VoucherCode = promotion.VoucherCode,
        VoucherDiscountPercent = promotion.VoucherDiscountPercent, VoucherMaxUses = promotion.VoucherMaxUses
    });
    public Task UpdatePromotionAsync(string id, UpdatePromotionDetails item, CancellationToken cancellationToken = default) => Write(nameof(AdminPromotionsController), "Update", id,
        new PromotionDetailsRequest { Name = item.Name, StartsAt = item.StartsAt, EndsAt = item.EndsAt });
    public Task UpdatePromotionRulesAsync(string id, UpdatePromotionRules item, CancellationToken cancellationToken = default) => Write(nameof(AdminPromotionsController), "UpdateRules", id,
        new PromotionRulesRequest
        {
            Books = item.Books.Select(x => new BookStore.Api.Controllers.PromotionBookRule { BookId = x.BookId, DiscountPercent = x.DiscountPercent }).ToArray(),
            Orders = item.Orders.Select(x => new BookStore.Api.Controllers.PromotionOrderRule { MinimumOrderAmount = x.MinimumOrderAmount, DiscountPercent = x.DiscountPercent }).ToArray(),
            Vouchers = item.Vouchers.Select(x => new BookStore.Api.Controllers.PromotionVoucherRule { VoucherId = x.VoucherId, DiscountPercent = x.DiscountPercent }).ToArray()
        });
    public Task DeletePromotionAsync(string id, CancellationToken cancellationToken = default) => Write(nameof(AdminPromotionsController), "Delete", id);

    public Task<IReadOnlyList<ManagerVoucher>> GetVouchersAsync(CancellationToken cancellationToken = default) => Read<IReadOnlyList<ManagerVoucher>>(nameof(AdminVouchersController), "Get");
    public Task UpdateVoucherAsync(string id, UpdateVoucher item, CancellationToken cancellationToken = default) => Write(nameof(AdminVouchersController), "Update", id,
        new VoucherUpdateRequest { Status = item.Status, ExpiresAt = item.ExpiresAt, MaxUsageCount = item.MaxUsageCount });
    public Task DeleteVoucherAsync(string id, CancellationToken cancellationToken = default) => Write(nameof(AdminVouchersController), "Delete", id);
    public Task<ManagerReport> GetReportAsync(int days = 30, CancellationToken cancellationToken = default) => Read<ManagerReport>(nameof(AdminReportsController), "Get", days);

    public Task CreateBookAsync(CreateManagerBook book, CancellationToken cancellationToken = default) => Write(nameof(AdminBooksController), "CreateBook", ToBookRequest(book));
    public Task<ManagerBookDetails> GetBookDetailsAsync(string id, CancellationToken cancellationToken = default) => Read<ManagerBookDetails>(nameof(AdminBooksController), "GetBookForEdit", id);
    public Task UpdateBookAsync(string id, CreateManagerBook book, CancellationToken cancellationToken = default) => Write(nameof(AdminBooksController), "UpdateBook", id, ToBookRequest(book));
    public Task HideBookAsync(string id, CancellationToken cancellationToken = default) => Write(nameof(AdminBooksController), "HideBook", id);
    public Task SetBookVisibilityAsync(string id, bool isActive, CancellationToken cancellationToken = default) => Write(nameof(AdminBooksController), "SetVisibility", id, new BookVisibilityRequest { IsActive = isActive });
    private static BookManagementRequest ToBookRequest(CreateManagerBook item) => new()
    {
        Title = item.Title, SalePrice = item.SalePrice, Unit = item.Unit, CategoryId = item.CategoryId, PublisherId = item.PublisherId, AuthorIds = item.AuthorIds
    };

    public Task CreateSupplierAsync(CreateSupplier supplier, CancellationToken cancellationToken = default) => Write(nameof(AdminInventoryController), "CreateSupplier", new SupplierRequest { Name = supplier.Name, PhoneNumber = supplier.PhoneNumber });
    public Task UpdateSupplierAsync(string id, CreateSupplier supplier, CancellationToken cancellationToken = default) => Write(nameof(AdminInventoryController), "UpdateSupplier", id, new SupplierRequest { Name = supplier.Name, PhoneNumber = supplier.PhoneNumber });
    public Task DeleteSupplierAsync(string id, CancellationToken cancellationToken = default) => Write(nameof(AdminInventoryController), "DeleteSupplier", id);
}
