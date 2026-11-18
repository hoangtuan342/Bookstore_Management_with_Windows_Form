using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TrangSach.Shared.DTO;

namespace TrangSach.Management.Web.BUS;

/// <summary>Stateless management client; each call forwards the signed-in employee JWT to the server API.</summary>
public sealed class ManagementWebApiClient(HttpClient http)
{
    public Task<StoreAuthResultDto?> LoginAsync(string userNameOrEmail, string password, CancellationToken ct = default) =>
        SendAsync<StoreAuthResultDto>(HttpMethod.Post, "api/auth/admin-login", new { userNameOrEmail, password }, null, ct);
    public Task<StoreAuthResultDto?> SetupFirstManagerAsync(string userName, string email, string password, string fullName, string? phoneNumber, CancellationToken ct = default) =>
        SendAsync<StoreAuthResultDto>(HttpMethod.Post, "api/auth/setup-manager", new { userName, email, password, fullName, phoneNumber }, null, ct);
    public Task<AccountProfileDto?> GetProfileAsync(string token, CancellationToken ct = default) => GetAsync<AccountProfileDto>("api/auth/me", token, ct);

    public Task<ManagerDashboardDto?> GetDashboardAsync(string token, CancellationToken ct = default) => GetAsync<ManagerDashboardDto>("api/admin/dashboard", token, ct);
    public Task<IReadOnlyList<ManagerAuditLogDto>> GetAuditLogsAsync(string token, CancellationToken ct = default) => GetAsync<IReadOnlyList<ManagerAuditLogDto>>("api/admin/logs", token, ct)!;
    public Task<IReadOnlyList<BookItemDto>> GetBooksAsync(string token, CancellationToken ct = default) => GetAsync<IReadOnlyList<BookItemDto>>("api/admin/books", token, ct)!;
    public Task<BookEditDetailsDto?> GetBookForEditAsync(string token, string id, CancellationToken ct = default) => GetAsync<BookEditDetailsDto>($"api/admin/books/{E(id)}/edit", token, ct);
    public Task<BookItemDto?> CreateBookAsync(string token, BookManagementCommandDto command, CancellationToken ct = default) => SendAsync<BookItemDto>(HttpMethod.Post, "api/admin/books", command, token, ct);
    public Task UpdateBookAsync(string token, string id, BookManagementCommandDto command, CancellationToken ct = default) => SendAsync(HttpMethod.Put, $"api/admin/books/{E(id)}", command, token, ct);
    public Task HideBookAsync(string token, string id, CancellationToken ct = default) => SendAsync(HttpMethod.Delete, $"api/admin/books/{E(id)}", null, token, ct);
    public Task SetBookVisibilityAsync(string token, string id, bool visible, CancellationToken ct = default) => SendAsync(HttpMethod.Put, $"api/admin/books/{E(id)}/visibility", new { isActive = visible }, token, ct);

    public Task<IReadOnlyList<ManagerCategoryDto>> GetCategoriesAsync(string token, CancellationToken ct = default) => GetAsync<IReadOnlyList<ManagerCategoryDto>>("api/admin/categories", token, ct)!;
    public Task SaveCategoryAsync(string token, string? id, SaveCategoryDto item, CancellationToken ct = default) => SendAsync(id is null ? HttpMethod.Post : HttpMethod.Put, id is null ? "api/admin/categories" : $"api/admin/categories/{E(id)}", item, token, ct);
    public Task DeleteCategoryAsync(string token, string id, CancellationToken ct = default) => SendAsync(HttpMethod.Delete, $"api/admin/categories/{E(id)}", null, token, ct);
    public Task<IReadOnlyList<ManagerAuthorDto>> GetAuthorsAsync(string token, CancellationToken ct = default) => GetAsync<IReadOnlyList<ManagerAuthorDto>>("api/admin/authors", token, ct)!;
    public Task SaveAuthorAsync(string token, int? id, SaveAuthorDto item, CancellationToken ct = default) => SendAsync(id is null ? HttpMethod.Post : HttpMethod.Put, id is null ? "api/admin/authors" : $"api/admin/authors/{id}", item, token, ct);
    public Task DeleteAuthorAsync(string token, int id, CancellationToken ct = default) => SendAsync(HttpMethod.Delete, $"api/admin/authors/{id}", null, token, ct);
    public Task<IReadOnlyList<ManagerPublisherDto>> GetPublishersAsync(string token, CancellationToken ct = default) => GetAsync<IReadOnlyList<ManagerPublisherDto>>("api/admin/publishers", token, ct)!;
    public Task SavePublisherAsync(string token, int? id, SavePublisherDto item, CancellationToken ct = default) => SendAsync(id is null ? HttpMethod.Post : HttpMethod.Put, id is null ? "api/admin/publishers" : $"api/admin/publishers/{id}", item, token, ct);
    public Task DeletePublisherAsync(string token, int id, CancellationToken ct = default) => SendAsync(HttpMethod.Delete, $"api/admin/publishers/{id}", null, token, ct);

    public Task<IReadOnlyList<ManagerCustomerDto>> GetCustomersAsync(string token, CancellationToken ct = default) => GetAsync<IReadOnlyList<ManagerCustomerDto>>("api/admin/customers", token, ct)!;
    public Task SaveCustomerAsync(string token, SaveCustomerDto item, CancellationToken ct = default) => SendAsync(item.Id is null ? HttpMethod.Post : HttpMethod.Put, item.Id is null ? "api/admin/customers" : $"api/admin/customers/{E(item.Id)}", item, token, ct);
    public Task DeleteCustomerAsync(string token, string id, CancellationToken ct = default) => SendAsync(HttpMethod.Delete, $"api/admin/customers/{E(id)}", null, token, ct);
    public Task<IReadOnlyList<ManagerEmployeeDto>> GetEmployeesAsync(string token, CancellationToken ct = default) => GetAsync<IReadOnlyList<ManagerEmployeeDto>>("api/admin/employees", token, ct)!;
    public Task<IReadOnlyList<StaffRoleOptionDto>> GetEmployeeRolesAsync(string token, CancellationToken ct = default) => GetAsync<IReadOnlyList<StaffRoleOptionDto>>("api/admin/employee-roles", token, ct)!;
    public Task SaveEmployeeAsync(string token, SaveEmployeeDto item, CancellationToken ct = default) => SendAsync(item.Id is null ? HttpMethod.Post : HttpMethod.Put, item.Id is null ? "api/admin/employees" : $"api/admin/employees/{E(item.Id)}", item, token, ct);
    public Task DeleteEmployeeAsync(string token, string id, CancellationToken ct = default) => SendAsync(HttpMethod.Delete, $"api/admin/employees/{E(id)}", null, token, ct);

    public Task<IReadOnlyList<ManagerOrderRowDto>> GetOrdersAsync(string token, string? status = null, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ManagerOrderRowDto>>("api/admin/orders" + (string.IsNullOrWhiteSpace(status) ? "" : "?status=" + E(status)), token, ct)!;
    public Task CreateOrderAsync(string token, CreateAdminOrderDto item, CancellationToken ct = default) => SendAsync(HttpMethod.Post, "api/admin/orders", item, token, ct);
    public Task UpdateOrderAsync(string token, string id, UpdateAdminOrderDto item, CancellationToken ct = default) => SendAsync(HttpMethod.Put, $"api/admin/orders/{E(id)}", item, token, ct);
    public Task CancelOrderAsync(string token, string id, CancellationToken ct = default) => SendAsync(HttpMethod.Delete, $"api/admin/orders/{E(id)}", null, token, ct);
    public Task UpdateOrderStatusAsync(string token, string id, string status, CancellationToken ct = default) => SendAsync(HttpMethod.Put, $"api/admin/orders/{E(id)}/status", new { status }, token, ct);
    public Task MarkTransferPaidAsync(string token, string id, CancellationToken ct = default) => SendAsync(HttpMethod.Put, $"api/admin/orders/{E(id)}/payment", null, token, ct);
    public Task MarkRefundCompletedAsync(string token, string id, CancellationToken ct = default) => SendAsync(HttpMethod.Put, $"api/admin/orders/{E(id)}/refund", null, token, ct);

    public Task<ManagerLookupsDto?> GetLookupsAsync(string token, CancellationToken ct = default) => GetAsync<ManagerLookupsDto>("api/admin/lookups", token, ct);
    public Task<IReadOnlyList<SupplierOptionDto>> GetSuppliersAsync(string token, CancellationToken ct = default) => GetAsync<IReadOnlyList<SupplierOptionDto>>("api/admin/suppliers", token, ct)!;
    public Task SaveSupplierAsync(string token, string? id, CreateSupplierDto item, CancellationToken ct = default) => SendAsync(id is null ? HttpMethod.Post : HttpMethod.Put, id is null ? "api/admin/suppliers" : $"api/admin/suppliers/{E(id)}", item, token, ct);
    public Task DeleteSupplierAsync(string token, string id, CancellationToken ct = default) => SendAsync(HttpMethod.Delete, $"api/admin/suppliers/{E(id)}", null, token, ct);
    public Task<IReadOnlyList<ManagerReceiptDto>> GetReceiptsAsync(string token, CancellationToken ct = default) => GetAsync<IReadOnlyList<ManagerReceiptDto>>("api/admin/receipts", token, ct)!;
    public Task CreateReceiptAsync(string token, CreateReceiptDto item, CancellationToken ct = default) => SendAsync(HttpMethod.Post, "api/admin/receipts", item, token, ct);
    public Task UpdateReceiptAsync(string token, string id, CreateReceiptDto item, CancellationToken ct = default) => SendAsync(HttpMethod.Put, $"api/admin/receipts/{E(id)}", item, token, ct);
    public Task CancelReceiptAsync(string token, string id, CancellationToken ct = default) => SendAsync(HttpMethod.Delete, $"api/admin/receipts/{E(id)}", null, token, ct);

    public Task<IReadOnlyList<ManagerPromotionDto>> GetPromotionsAsync(string token, CancellationToken ct = default) => GetAsync<IReadOnlyList<ManagerPromotionDto>>("api/admin/promotions", token, ct)!;
    public Task CreatePromotionAsync(string token, CreatePromotionDto item, CancellationToken ct = default) => SendAsync(HttpMethod.Post, "api/admin/promotions", item, token, ct);
    public Task UpdatePromotionAsync(string token, string id, UpdatePromotionDetailsDto item, CancellationToken ct = default) => SendAsync(HttpMethod.Put, $"api/admin/promotions/{E(id)}", item, token, ct);
    public Task UpdatePromotionRulesAsync(string token, string id, UpdatePromotionRulesDto item, CancellationToken ct = default) => SendAsync(HttpMethod.Put, $"api/admin/promotions/{E(id)}/rules", item, token, ct);
    public Task DeletePromotionAsync(string token, string id, CancellationToken ct = default) => SendAsync(HttpMethod.Delete, $"api/admin/promotions/{E(id)}", null, token, ct);
    public Task<IReadOnlyList<VoucherSummaryDto>> GetVouchersAsync(string token, CancellationToken ct = default) => GetAsync<IReadOnlyList<VoucherSummaryDto>>("api/admin/vouchers", token, ct)!;
    public Task UpdateVoucherAsync(string token, string id, VoucherUpdateCommandDto item, CancellationToken ct = default) => SendAsync(HttpMethod.Put, $"api/admin/vouchers/{E(id)}", item, token, ct);
    public Task DeleteVoucherAsync(string token, string id, CancellationToken ct = default) => SendAsync(HttpMethod.Delete, $"api/admin/vouchers/{E(id)}", null, token, ct);
    public Task<ManagerReportDto?> GetReportAsync(string token, int days, CancellationToken ct = default) => GetAsync<ManagerReportDto>($"api/admin/reports?days={Math.Clamp(days, 1, 3650)}", token, ct);
    public Task<ManagerReportDto?> GetReportAsync(string token, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) =>
        GetAsync<ManagerReportDto>($"api/admin/reports?fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}", token, ct);

    private static string E(string value) => Uri.EscapeDataString(value);
    private Task<T?> GetAsync<T>(string path, string token, CancellationToken ct) => SendAsync<T>(HttpMethod.Get, path, null, token, ct);
    private async Task SendAsync(HttpMethod method, string path, object? body, string token, CancellationToken ct) => _ = await SendAsync<JsonElement?>(method, path, body, token, ct);

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body, string? token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ErrorAsync(response, ct);
            var message = $"API {method} {request.RequestUri} returned {(int)response.StatusCode} ({response.ReasonPhrase}): {detail}";
            if (response.StatusCode == HttpStatusCode.Unauthorized) throw new ManagementApiUnauthorizedException(message);
            throw new InvalidOperationException(message);
        }
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0) return default;
        try { return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct); }
        catch (JsonException) { return default; }
    }

    private static async Task<string> ErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized) return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
        try
        {
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (json.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String) return message.GetString()!;
            if (json.RootElement.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String) return detail.GetString()!;
            if (json.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String) return title.GetString()!;
            if (json.RootElement.TryGetProperty("errors", out var errors)) return errors.ToString();
        }
        catch (JsonException) { }
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(body)) return body;
        return $"Máy chủ từ chối yêu cầu ({(int)response.StatusCode}).";
    }
}

public sealed class ManagementApiUnauthorizedException(string message) : InvalidOperationException(message);
