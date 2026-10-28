using System.Text.Json;

namespace TrangSach.Customer.Services;

internal sealed class CustomerAppServices : IDisposable
{
    public CustomerApiGateway Store { get; }
    public CustomerApiGateway Catalog => Store;
    public string ApiAddress => Store.BaseAddress;

    public CustomerAppServices()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var baseAddress = "https://localhost:7215/";
        if (File.Exists(settingsPath))
        {
            using var settings = JsonDocument.Parse(File.ReadAllText(settingsPath));
            if (settings.RootElement.TryGetProperty("Api", out var api)
                && api.TryGetProperty("BaseAddress", out var value)
                && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                baseAddress = value.GetString()!;
            }
        }

        if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("Cấu hình Api:BaseAddress trong appsettings.json không hợp lệ.");
        Store = new CustomerApiGateway(uri);
    }

    public void Dispose() => Store.Dispose();
}
