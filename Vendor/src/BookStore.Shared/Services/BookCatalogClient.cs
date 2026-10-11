using System.Net.Http.Json;

namespace BookStore.Shared.Services;

public sealed class BookCatalogClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<Models.BookItem>> GetBooksAsync(
        string? search = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            parameters.Add($"search={Uri.EscapeDataString(search)}");
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "Tất cả")
        {
            parameters.Add($"category={Uri.EscapeDataString(category)}");
        }

        var query = parameters.Count == 0 ? string.Empty : $"?{string.Join('&', parameters)}";
        return await httpClient.GetFromJsonAsync<List<Models.BookItem>>($"api/books{query}", cancellationToken)
            ?? [];
    }
}
