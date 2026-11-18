using TrangSach.Shared.DTO;
using System.Collections.Concurrent;
using TrangSach.BUS;
using TrangSach.DAO;

namespace TrangSach.Management.BUS;

public sealed class ManagementApplicationFacade : IDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<Task<object>>> _readCache = new(StringComparer.Ordinal);
    public ManagementSessionService Store { get; }
    public ManagementOperationsService Manager { get; }
    public string DatabaseConnectionCaption => BookStoreDao.ConnectionCaption;

    public ManagementApplicationFacade()
    {
        var passwordService = new PasswordHashingService();
        Store = new ManagementSessionService(passwordService);
        Manager = new ManagementOperationsService(new ManagementBusinessService(Store, passwordService));
    }

    public Task InitializeDatabaseAsync() => BookStoreDao.InitializeAsync();

    public Task<ManagerDashboardDto> GetDashboardAsync(bool refresh = false) =>
        ReadCachedAsync("dashboard", () => Manager.GetDashboardAsync(), refresh);
    public Task<IReadOnlyList<BookItemDto>> GetBooksAsync(bool refresh = false) =>
        ReadCachedAsync("books", () => Manager.GetBooksAsync(), refresh);
    public Task<IReadOnlyList<ManagerCustomerDto>> GetCustomersAsync(bool refresh = false) =>
        ReadCachedAsync("customers", () => Manager.GetCustomersAsync(), refresh);
    public Task<IReadOnlyList<ManagerEmployeeDto>> GetEmployeesAsync(bool refresh = false) =>
        ReadCachedAsync("employees", () => Manager.GetEmployeesAsync(), refresh);
    public Task<IReadOnlyList<ManagerOrderRowDto>> GetOrdersAsync(bool refresh = false) =>
        ReadCachedAsync("orders", () => Manager.GetOrdersAsync(), refresh);
    public Task<IReadOnlyList<ManagerReceiptDto>> GetReceiptsAsync(bool refresh = false) =>
        ReadCachedAsync("receipts", () => Manager.GetReceiptsAsync(), refresh);
    public Task<ManagerLookupsDto> GetLookupsAsync(bool refresh = false) =>
        ReadCachedAsync("lookups", () => Manager.GetLookupsAsync(), refresh);
    public Task<IReadOnlyList<ManagerPromotionDto>> GetPromotionsAsync(bool refresh = false) =>
        ReadCachedAsync("promotions", () => Manager.GetPromotionsAsync(), refresh);
    public Task<IReadOnlyList<ManagerVoucherDto>> GetVouchersAsync(bool refresh = false) =>
        ReadCachedAsync("vouchers", () => Manager.GetVouchersAsync(), refresh);
    public Task<IReadOnlyList<ManagerAuditLogDto>> GetAuditLogsAsync(bool refresh = false) =>
        ReadCachedAsync("logs", () => Manager.GetAuditLogsAsync(), refresh);
    public Task<IReadOnlyList<ManagerCategoryDto>> GetCategoriesAsync(bool refresh = false) =>
        ReadCachedAsync("categories", () => Manager.GetCategoriesAsync(), refresh);
    public Task<IReadOnlyList<ManagerAuthorDto>> GetAuthorsAsync(bool refresh = false) =>
        ReadCachedAsync("authors", () => Manager.GetAuthorsAsync(), refresh);
    public Task<IReadOnlyList<ManagerPublisherDto>> GetPublishersAsync(bool refresh = false) =>
        ReadCachedAsync("publishers", () => Manager.GetPublishersAsync(), refresh);

    public async Task<T> ReadCachedAsync<T>(string key, Func<Task<T>> load, bool refresh = false) where T : notnull
    {
        if (refresh) _readCache.TryRemove(key, out _);
        var cached = _readCache.GetOrAdd(key, _ => new Lazy<Task<object>>(
            async () => (object)(await load())!, LazyThreadSafetyMode.ExecutionAndPublication));
        try { return (T)await cached.Value; }
        catch
        {
            _readCache.TryRemove(key, out _);
            throw;
        }
    }
    public void Invalidate(params string[] keys)
    {
        foreach (var key in keys) _readCache.TryRemove(key, out _);
    }
    public void Dispose() => _readCache.Clear();
}
