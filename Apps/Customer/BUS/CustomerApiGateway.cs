using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookStore.Shared.Models;

namespace TrangSach.Customer.BUS;

/// <summary>WinForms customer facade. It communicates with the server API and has no database dependency.</summary>
public sealed class CustomerApiGateway : IDisposable
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;

    public string BaseAddress => _httpClient.BaseAddress!.ToString().TrimEnd('/');
    public string? CustomerId { get; private set; }
    public string? Role { get; private set; }

    public CustomerApiGateway(Uri baseAddress, HttpMessageHandler? handler = null)
    {
        if (!baseAddress.IsAbsoluteUri || baseAddress.Scheme is not ("https" or "http"))
            throw new ArgumentException("Địa chỉ API phải là URL HTTP hoặc HTTPS tuyệt đối.", nameof(baseAddress));
        if (baseAddress.Scheme != Uri.UriSchemeHttps && !baseAddress.IsLoopback)
            throw new ArgumentException("Chỉ cho phép HTTP trên máy localhost; máy chủ bên ngoài phải dùng HTTPS.", nameof(baseAddress));

        _httpClient = handler is null ? new HttpClient() : new HttpClient(handler);
        var normalizedAddress = baseAddress.AbsoluteUri.EndsWith('/') ? baseAddress.AbsoluteUri : baseAddress.AbsoluteUri + "/";
        _httpClient.BaseAddress = new Uri(normalizedAddress);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<IReadOnlyList<BookItem>> GetBooksAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<List<BookItem>>("api/books", cancellationToken) ?? [];

    public Task<StoreApiResult> LoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default) =>
        AuthenticateAsync("api/auth/login", new { userNameOrEmail, password }, cancellationToken);

    public Task<StoreApiResult> RegisterAsync(string userName, string email, string password, string fullName, string? phoneNumber, CancellationToken cancellationToken = default) =>
        AuthenticateAsync("api/auth/register", new { userName, email, password, fullName, phoneNumber }, cancellationToken, "Tạo tài khoản thành công.");

    public async Task<IReadOnlyList<StoreCartLine>> GetCartAsync(CancellationToken cancellationToken = default) =>
        await SendAuthorizedAsync<List<StoreCartLine>>(HttpMethod.Get, "api/cart", null, cancellationToken) ?? [];

    public Task SetCartQuantityAsync(string bookId, int quantity, CancellationToken cancellationToken = default) =>
        SendAuthorizedAsync(HttpMethod.Put, $"api/cart/items/{Uri.EscapeDataString(bookId)}", new { quantity }, cancellationToken);

    public async Task<IReadOnlyList<StoreOrder>> GetMyOrdersAsync(CancellationToken cancellationToken = default) =>
        await SendAuthorizedAsync<List<StoreOrder>>(HttpMethod.Get, "api/orders", null, cancellationToken) ?? [];

    public async Task<StoreOrder> CheckoutAsync(CheckoutDetails details, CancellationToken cancellationToken = default) =>
        await SendAuthorizedAsync<StoreOrder>(HttpMethod.Post, "api/orders", details, cancellationToken)
        ?? throw new InvalidOperationException("Máy chủ không trả về thông tin đơn hàng.");

    public Task SignOutAsync()
    {
        _accessToken = null;
        CustomerId = null;
        Role = null;
        return Task.CompletedTask;
    }

    public void Dispose() => _httpClient.Dispose();

    private async Task<StoreApiResult> AuthenticateAsync(string path, object request, CancellationToken cancellationToken, string successMessage = "")
    {
        using var response = await _httpClient.PostAsJsonAsync(path, request, cancellationToken);
        if (!response.IsSuccessStatusCode) return new(false, await ReadErrorAsync(response, cancellationToken));

        var auth = await response.Content.ReadFromJsonAsync<StoreAuthResult>(cancellationToken: cancellationToken);
        if (auth is null || string.IsNullOrWhiteSpace(auth.AccessToken)) return new(false, "Máy chủ trả về thông tin đăng nhập không hợp lệ.");
        _accessToken = auth.AccessToken;
        CustomerId = auth.CustomerId;
        Role = auth.Role;
        return new(true, successMessage.Length == 0 ? $"Xin chào {auth.UserName}!" : successMessage, auth.Role);
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private async Task SendAuthorizedAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var response = await SendAuthorizedCoreAsync(method, path, body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<T?> SendAuthorizedAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var response = await SendAuthorizedCoreAsync(method, path, body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NoContent) return default;
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAuthorizedCoreAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_accessToken)) throw new UnauthorizedAccessException("Vui lòng đăng nhập để tiếp tục.");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await SignOutAsync();
            throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }
        if (response.IsSuccessStatusCode) return;
        throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: cancellationToken);
            if (body is not null && body.TryGetValue("message", out var message)) return message;
            if (body is not null && body.TryGetValue("title", out var title)) return title;
        }
        catch (System.Text.Json.JsonException) { }
        return $"Yêu cầu không thành công ({(int)response.StatusCode}).";
    }
}
