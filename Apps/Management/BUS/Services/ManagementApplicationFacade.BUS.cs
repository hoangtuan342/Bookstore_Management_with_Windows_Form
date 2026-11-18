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

    public void InitializeDatabase() => BookStoreDao.Initialize();
    public Task InitializeDatabaseAsync() => BookStoreDao.InitializeAsync();

    // Synchronous reads (Phong cách thiết kế 3 lớp truyền thống)
    public ManagerDashboardDto GetDashboard(bool refresh = false) =>
        ReadCached("dashboard", () => Manager.GetDashboard(), refresh);
    public IReadOnlyList<BookItemDto> GetBooks(bool refresh = false) =>
        ReadCached("books", () => Manager.GetBooks(), refresh);
    public IReadOnlyList<ManagerCustomerDto> GetCustomers(bool refresh = false) =>
        ReadCached("customers", () => Manager.GetCustomers(), refresh);
    public IReadOnlyList<ManagerEmployeeDto> GetEmployees(bool refresh = false) =>
        ReadCached("employees", () => Manager.GetEmployees(), refresh);
    public IReadOnlyList<ManagerOrderRowDto> GetOrders(bool refresh = false) =>
        ReadCached("orders", () => Manager.GetOrders(), refresh);
    public IReadOnlyList<ManagerReceiptDto> GetReceipts(bool refresh = false) =>
        ReadCached("receipts", () => Manager.GetReceipts(), refresh);
    public ManagerLookupsDto GetLookups(bool refresh = false) =>
        ReadCached("lookups", () => Manager.GetLookups(), refresh);
    public IReadOnlyList<ManagerPromotionDto> GetPromotions(bool refresh = false) =>
        ReadCached("promotions", () => Manager.GetPromotions(), refresh);
    public IReadOnlyList<ManagerVoucherDto> GetVouchers(bool refresh = false) =>
        ReadCached("vouchers", () => Manager.GetVouchers(), refresh);
    public IReadOnlyList<ManagerAuditLogDto> GetAuditLogs(bool refresh = false) =>
        ReadCached("logs", () => Manager.GetAuditLogs(), refresh);
    public IReadOnlyList<ManagerCategoryDto> GetCategories(bool refresh = false) =>
        ReadCached("categories", () => Manager.GetCategories(), refresh);
    public IReadOnlyList<ManagerAuthorDto> GetAuthors(bool refresh = false) =>
        ReadCached("authors", () => Manager.GetAuthors(), refresh);
    public IReadOnlyList<ManagerPublisherDto> GetPublishers(bool refresh = false) =>
        ReadCached("publishers", () => Manager.GetPublishers(), refresh);

    private readonly ConcurrentDictionary<string, object> _syncCache = new(StringComparer.Ordinal);
    public T ReadCached<T>(string key, Func<T> load, bool refresh = false) where T : notnull
    {
        if (refresh) _syncCache.TryRemove(key, out _);
        return (T)_syncCache.GetOrAdd(key, _ => (object)load()!);
    }

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
        foreach (var key in keys)
        {
            _readCache.TryRemove(key, out _);
            _syncCache.TryRemove(key, out _);
        }
    }
    public void Dispose()
    {
        _readCache.Clear();
        _syncCache.Clear();
    }
}
