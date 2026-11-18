using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TrangSach.Shared.DTO;

namespace TrangSach.Customer.BUS;

/// <summary>Stateless BUS-side client for the customer website to call the bookstore API.</summary>
public sealed class CustomerWebApiClient(HttpClient httpClient)
{
    public async Task<List<BookItemDto>> GetBooksAsync(string? search, string? category, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add("search=" + Uri.EscapeDataString(search.Trim()));
        if (!string.IsNullOrWhiteSpace(category)) query.Add("category=" + Uri.EscapeDataString(category.Trim()));
        var path = "api/books" + (query.Count == 0 ? "" : "?" + string.Join("&", query));
        return await GetAsync<List<BookItemDto>>(path, null, ct) ?? [];
    }

    public Task<BookItemDto?> GetBookAsync(string id, CancellationToken ct = default) =>
        GetAsync<BookItemDto>($"api/books/{Uri.EscapeDataString(id)}", null, ct);

    public Task<StoreAuthResultDto?> LoginAsync(string userNameOrEmail, string password, CancellationToken ct = default) =>
        PostAsync<StoreAuthResultDto>("api/auth/login", new { userNameOrEmail, password }, null, ct);

    public Task<StoreAuthResultDto?> RegisterAsync(string userName, string email, string password, string fullName, string? phoneNumber, CancellationToken ct = default) =>
        PostAsync<StoreAuthResultDto>("api/auth/register", new { userName, email, password, fullName, phoneNumber }, null, ct);

    public Task<AccountProfileDto?> GetProfileAsync(string token, CancellationToken ct = default) =>
        GetAsync<AccountProfileDto>("api/auth/me", token, ct);

    public async Task<List<StoreCartLineDto>> GetCartAsync(string token, CancellationToken ct = default) =>
        await GetAsync<List<StoreCartLineDto>>("api/cart", token, ct) ?? [];

    public Task SetCartQuantityAsync(string token, string bookId, int quantity, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Put, $"api/cart/items/{Uri.EscapeDataString(bookId)}", new { quantity }, token, ct);

    public Task RemoveCartItemAsync(string token, string bookId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"api/cart/items/{Uri.EscapeDataString(bookId)}", null, token, ct);

    public async Task<StoreOrderDto> CheckoutAsync(string token, CheckoutDetailsDto details, CancellationToken ct = default) =>
        await SendAsync<StoreOrderDto>(HttpMethod.Post, "api/orders", details, token, ct)
        ?? throw new InvalidOperationException("Máy chủ không trả về thông tin đơn hàng.");

    public async Task<List<StoreOrderDto>> GetOrdersAsync(string token, CancellationToken ct = default) =>
        await GetAsync<List<StoreOrderDto>>("api/orders", token, ct) ?? [];

    public Task<StoreOrderDto?> GetOrderAsync(string token, string orderId, CancellationToken ct = default) =>
        GetAsync<StoreOrderDto>($"api/orders/{Uri.EscapeDataString(orderId)}", token, ct);

    public Task CancelOrderAsync(string token, string orderId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Put, $"api/orders/{Uri.EscapeDataString(orderId)}/cancel", null, token, ct);

    private async Task<T?> GetAsync<T>(string path, string? token, CancellationToken ct) =>
        await SendAsync<T>(HttpMethod.Get, path, null, token, ct);

    private async Task<T?> PostAsync<T>(string path, object body, string? token, CancellationToken ct) =>
        await SendAsync<T>(HttpMethod.Post, path, body, token, ct);

    private async Task SendAsync(HttpMethod method, string path, object? body, string? token, CancellationToken ct)
    {
        using var response = await SendCoreAsync(method, path, body, token, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body, string? token, CancellationToken ct)
    {
        using var response = await SendCoreAsync(method, path, body, token, ct);
        await EnsureSuccessAsync(response, ct);
        if (response.StatusCode == HttpStatusCode.NoContent) return default;
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string path, object? body, string? token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await httpClient.SendAsync(request, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        var message = $"Yêu cầu tới máy chủ không thành công ({(int)response.StatusCode}).";
        try
        {
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (body.RootElement.TryGetProperty("message", out var detail) && detail.ValueKind == JsonValueKind.String)
                message = detail.GetString() ?? message;
            else if (body.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                message = title.GetString() ?? message;
        }
        catch (JsonException) { }
        throw new InvalidOperationException(message);
    }
}
