using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookStore.Shared.Models;

namespace BookStore.Shared.Services;

public sealed class ManagerApiClient(HttpClient httpClient, ITokenStore tokenStore)
{
    public Task<ManagerDashboard> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        GetAsync<ManagerDashboard>("api/admin/dashboard", cancellationToken);

    public Task<IReadOnlyList<ManagerAuditLog>> GetAuditLogsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ManagerAuditLog>>("api/admin/logs", cancellationToken);

    public Task<IReadOnlyList<BookItem>> GetBooksAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<BookItem>>("api/admin/books", cancellationToken);

    public Task<IReadOnlyList<BookItem>> GetBooksForPromotionsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<BookItem>>("api/admin/books", cancellationToken, "promotions");

    public Task<ManagerLookups> GetLookupsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<ManagerLookups>("api/admin/lookups", cancellationToken);

    public Task<IReadOnlyList<ManagerCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyList<ManagerCategory>>("api/admin/categories", cancellationToken);
    public Task SaveCategoryAsync(string? id, SaveCategory item, CancellationToken cancellationToken = default) => SendAsync(id is null ? HttpMethod.Post : HttpMethod.Put, id is null ? "api/admin/categories" : $"api/admin/categories/{Uri.EscapeDataString(id)}", item, cancellationToken);
    public Task DeleteCategoryAsync(string id, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Delete, $"api/admin/categories/{Uri.EscapeDataString(id)}", null, cancellationToken);
    public Task<IReadOnlyList<ManagerAuthor>> GetAuthorsAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyList<ManagerAuthor>>("api/admin/authors", cancellationToken);
    public Task SaveAuthorAsync(int? id, SaveAuthor item, CancellationToken cancellationToken = default) => SendAsync(id is null ? HttpMethod.Post : HttpMethod.Put, id is null ? "api/admin/authors" : $"api/admin/authors/{id}", item, cancellationToken);
    public Task DeleteAuthorAsync(int id, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Delete, $"api/admin/authors/{id}", null, cancellationToken);
    public Task<IReadOnlyList<ManagerPublisher>> GetPublishersAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyList<ManagerPublisher>>("api/admin/publishers", cancellationToken);
    public Task SavePublisherAsync(int? id, SavePublisher item, CancellationToken cancellationToken = default) => SendAsync(id is null ? HttpMethod.Post : HttpMethod.Put, id is null ? "api/admin/publishers" : $"api/admin/publishers/{id}", item, cancellationToken);
    public Task DeletePublisherAsync(int id, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Delete, $"api/admin/publishers/{id}", null, cancellationToken);
    public Task<IReadOnlyList<ManagerCustomer>> GetCustomersAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyList<ManagerCustomer>>("api/admin/customers", cancellationToken);
    public Task SaveCustomerAsync(string? id, SaveCustomer item, CancellationToken cancellationToken = default) => SendAsync(id is null ? HttpMethod.Post : HttpMethod.Put, id is null ? "api/admin/customers" : $"api/admin/customers/{Uri.EscapeDataString(id)}", item, cancellationToken);
    public Task DeleteCustomerAsync(string id, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Delete, $"api/admin/customers/{Uri.EscapeDataString(id)}", null, cancellationToken);
    public Task<IReadOnlyList<ManagerEmployee>> GetEmployeesAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyList<ManagerEmployee>>("api/admin/employees", cancellationToken);
    public Task<IReadOnlyList<StaffRoleOption>> GetEmployeeRolesAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyList<StaffRoleOption>>("api/admin/employee-roles", cancellationToken);
    public Task SaveEmployeeAsync(string? id, SaveEmployee item, CancellationToken cancellationToken = default) => SendAsync(id is null ? HttpMethod.Post : HttpMethod.Put, id is null ? "api/admin/employees" : $"api/admin/employees/{Uri.EscapeDataString(id)}", item, cancellationToken);
    public Task DeleteEmployeeAsync(string id, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Delete, $"api/admin/employees/{Uri.EscapeDataString(id)}", null, cancellationToken);

    public Task<IReadOnlyList<ManagerOrderRow>> GetOrdersAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(status)
            ? "api/admin/orders"
            : $"api/admin/orders?status={Uri.EscapeDataString(status)}";
        return GetAsync<IReadOnlyList<ManagerOrderRow>>(path, cancellationToken);
    }

    public Task CreateOrderAsync(CreateAdminOrder order, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/admin/orders", order, cancellationToken);

    public Task<IReadOnlyList<ManagerReceipt>> GetReceiptsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ManagerReceipt>>("api/admin/receipts", cancellationToken);

    public Task<IReadOnlyList<ManagerPromotion>> GetPromotionsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ManagerPromotion>>("api/admin/promotions", cancellationToken);

    public Task CreatePromotionAsync(CreatePromotion promotion, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/admin/promotions", promotion, cancellationToken);

    public Task UpdatePromotionAsync(string id, UpdatePromotionDetails item, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Put, $"api/admin/promotions/{Uri.EscapeDataString(id)}", item, cancellationToken);
    public Task UpdatePromotionRulesAsync(string id, UpdatePromotionRules item, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Put, $"api/admin/promotions/{Uri.EscapeDataString(id)}/rules", item, cancellationToken);
    public Task DeletePromotionAsync(string id, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Delete, $"api/admin/promotions/{Uri.EscapeDataString(id)}", null, cancellationToken);
    public Task<IReadOnlyList<ManagerVoucher>> GetVouchersAsync(CancellationToken cancellationToken = default) => GetAsync<IReadOnlyList<ManagerVoucher>>("api/admin/vouchers", cancellationToken);
    public Task UpdateVoucherAsync(string id, UpdateVoucher item, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Put, $"api/admin/vouchers/{Uri.EscapeDataString(id)}", item, cancellationToken);
    public Task DeleteVoucherAsync(string id, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Delete, $"api/admin/vouchers/{Uri.EscapeDataString(id)}", null, cancellationToken);

    public Task<ManagerReport> GetReportAsync(int days = 30, CancellationToken cancellationToken = default) =>
        GetAsync<ManagerReport>($"api/admin/reports?days={days}", cancellationToken);

    public Task CreateBookAsync(CreateManagerBook book, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/admin/books", book, cancellationToken);

    public Task<ManagerBookDetails> GetBookDetailsAsync(string id, CancellationToken cancellationToken = default) =>
        GetAsync<ManagerBookDetails>($"api/admin/books/{Uri.EscapeDataString(id)}/edit", cancellationToken);

    public Task UpdateBookAsync(string id, CreateManagerBook book, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/admin/books/{Uri.EscapeDataString(id)}", book, cancellationToken);

    public Task HideBookAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/admin/books/{Uri.EscapeDataString(id)}", null, cancellationToken);

    public Task SetBookVisibilityAsync(string id, bool isActive, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/admin/books/{Uri.EscapeDataString(id)}/visibility", new { isActive }, cancellationToken);

    public Task CreateSupplierAsync(CreateSupplier supplier, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/admin/suppliers", supplier, cancellationToken);

    public Task UpdateSupplierAsync(string id, CreateSupplier supplier, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/admin/suppliers/{Uri.EscapeDataString(id)}", supplier, cancellationToken);

    public Task DeleteSupplierAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/admin/suppliers/{Uri.EscapeDataString(id)}", null, cancellationToken);

    public Task CreateReceiptAsync(CreateReceipt receipt, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/admin/receipts", receipt, cancellationToken);

    public Task UpdateReceiptAsync(string id, CreateReceipt receipt, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/admin/receipts/{Uri.EscapeDataString(id)}", receipt, cancellationToken);

    public Task UpdateOrderAsync(string id, UpdateAdminOrder order, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/admin/orders/{Uri.EscapeDataString(id)}", order, cancellationToken);

    public Task DeleteOrderAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/admin/orders/{Uri.EscapeDataString(id)}", null, cancellationToken);

    public Task UpdateOrderStatusAsync(string id, string status, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/admin/orders/{Uri.EscapeDataString(id)}/status", new { status }, cancellationToken);

    public Task MarkBankTransferPaidAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/admin/orders/{Uri.EscapeDataString(id)}/payment", null, cancellationToken);

    public Task MarkRefundCompletedAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/admin/orders/{Uri.EscapeDataString(id)}/refund", null, cancellationToken);

    public Task CancelReceiptAsync(string id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/admin/receipts/{Uri.EscapeDataString(id)}", null, cancellationToken);

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken, string? auditArea = null)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, path, null, auditArea);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Máy chủ trả về dữ liệu rỗng.");
    }

    private async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(method, path, body, null);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string path, object? body, string? auditArea)
    {
        var token = await tokenStore.GetAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException("Hãy đăng nhập bằng tài khoản nhân viên.");
        }

        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (!string.IsNullOrWhiteSpace(auditArea)) request.Headers.Add("X-Audit-Area", auditArea);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await tokenStore.ClearAsync();
            throw new UnauthorizedAccessException("Phiên đăng nhập quản lý đã hết hạn. Vui lòng đăng nhập lại.");
        }

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: cancellationToken);
                if (body is not null && body.TryGetValue("message", out var message))
                {
                    throw new InvalidOperationException(message);
                }
            }
            catch (System.Text.Json.JsonException)
            {
            }

            throw new InvalidOperationException($"Yêu cầu không thành công ({(int)response.StatusCode}).");
        }
    }
}
