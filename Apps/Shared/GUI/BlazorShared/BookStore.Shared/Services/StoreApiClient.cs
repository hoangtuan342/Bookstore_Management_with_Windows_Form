using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookStore.Shared.Models;

namespace BookStore.Shared.Services;

public sealed class StoreApiClient(HttpClient httpClient, ITokenStore tokenStore)
{
    public async Task<StoreApiResult> LoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default)
        => await LoginAtAsync("api/auth/login", userNameOrEmail, password, cancellationToken);

    public async Task<StoreApiResult> AdminLoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default)
        => await LoginAtAsync("api/auth/admin-login", userNameOrEmail, password, cancellationToken);

    private async Task<StoreApiResult> LoginAtAsync(string path, string userNameOrEmail, string password, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(path, new { userNameOrEmail, password }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new(false, await ReadErrorAsync(response, cancellationToken));
        }

        var auth = await response.Content.ReadFromJsonAsync<StoreAuthResult>(cancellationToken: cancellationToken);
        if (auth is null)
        {
            return new(false, "Máy chủ trả về dữ liệu đăng nhập không hợp lệ.");
        }

        await tokenStore.SetAsync(auth.AccessToken, auth.Role);
        return new(true, $"Xin chào {auth.UserName}!", auth.Role);
    }

    public async Task<StoreApiResult> RegisterAsync(string userName, string email, string password, string fullName, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/auth/register", new { userName, email, password, fullName, phoneNumber }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new(false, await ReadErrorAsync(response, cancellationToken));
        }

        var auth = await response.Content.ReadFromJsonAsync<StoreAuthResult>(cancellationToken: cancellationToken);
        if (auth is null)
        {
            return new(false, "Máy chủ trả về dữ liệu đăng ký không hợp lệ.");
        }

        await tokenStore.SetAsync(auth.AccessToken, auth.Role);
        return new(true, "Tạo tài khoản thành công.", auth.Role);
    }

    public async Task<StoreProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        using var request = await AuthorizedRequestAsync(HttpMethod.Get, "api/auth/me", null, cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<StoreProfile>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Máy chủ không trả về hồ sơ tài khoản.");
    }

    public async Task<StoreApiResult> SetupFirstManagerAsync(string userName, string email, string password, string fullName, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/auth/setup-manager", new { userName, email, password, fullName, phoneNumber }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new(false, await ReadErrorAsync(response, cancellationToken));
        }

        var auth = await response.Content.ReadFromJsonAsync<StoreAuthResult>(cancellationToken: cancellationToken);
        if (auth is null)
        {
            return new(false, "Máy chủ trả về dữ liệu quản lý không hợp lệ.");
        }

        await tokenStore.SetAsync(auth.AccessToken, auth.Role);
        return new(true, "Tạo tài khoản quản lý thành công.", auth.Role);
    }

    public async Task<IReadOnlyList<StoreCartLine>> GetCartAsync(CancellationToken cancellationToken = default)
    {
        using var request = await AuthorizedRequestAsync(HttpMethod.Get, "api/cart", null, cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<StoreCartLine>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task SetCartQuantityAsync(string bookId, int quantity, CancellationToken cancellationToken = default)
    {
        using var request = await AuthorizedRequestAsync(HttpMethod.Put, $"api/cart/items/{Uri.EscapeDataString(bookId)}", new { quantity }, cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task RemoveCartItemAsync(string bookId, CancellationToken cancellationToken = default)
    {
        using var request = await AuthorizedRequestAsync(HttpMethod.Delete, $"api/cart/items/{Uri.EscapeDataString(bookId)}", null, cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<StoreOrder> CheckoutAsync(CheckoutDetails details, CancellationToken cancellationToken = default)
    {
        using var request = await AuthorizedRequestAsync(HttpMethod.Post, "api/orders", details, cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<StoreOrder>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Máy chủ không trả về đơn hàng.");
    }

    public async Task<IReadOnlyList<StoreOrder>> GetMyOrdersAsync(CancellationToken cancellationToken = default)
    {
        using var request = await AuthorizedRequestAsync(HttpMethod.Get, "api/orders", null, cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<StoreOrder>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task CancelMyOrderAsync(string id, CancellationToken cancellationToken = default)
    {
        using var request = await AuthorizedRequestAsync(HttpMethod.Put, $"api/orders/{Uri.EscapeDataString(id)}/cancel", null, cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public Task SignOutAsync() => tokenStore.ClearAsync();

    public Task<string?> GetCurrentTokenAsync() => tokenStore.GetAsync();
    public Task<string?> GetCurrentRoleAsync() => tokenStore.GetRoleAsync();

    private async Task<HttpRequestMessage> AuthorizedRequestAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, path);
        var token = await tokenStore.GetAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            request.Dispose();
            throw new UnauthorizedAccessException("Hãy đăng nhập để tiếp tục.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
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
            throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: cancellationToken);
            if (body is not null && body.TryGetValue("message", out var message))
            {
                return message;
            }

            if (body is not null && body.TryGetValue("title", out var title))
            {
                return title;
            }
        }
        catch (System.Text.Json.JsonException)
        {
        }

        return $"Yêu cầu không thành công ({(int)response.StatusCode}).";
    }
}
