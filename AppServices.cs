namespace TrangSach.WinForms;

public sealed class AppServices : IDisposable
{
    public DirectCustomerClient Store { get; }
    public DirectManagerClient Manager { get; }
    public DirectBookCatalog Catalog { get; }
    public string DatabaseConnectionString => DirectDatabase.ConnectionString;

    public AppServices()
    {
        Store = new DirectCustomerClient();
        Manager = new DirectManagerClient(new DirectControllerDispatcher(Store));
        Catalog = new DirectBookCatalog();
    }

    public Task InitializeDatabaseAsync() => DirectDatabase.InitializeAsync();
    public void Dispose() { }
}

public static class MoneyFormatter
{
    public static string ToVnd(decimal amount) => amount.ToString("N0") + " ₫";
}
