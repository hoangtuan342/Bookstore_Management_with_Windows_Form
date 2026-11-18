using TrangSach.Shared.DTO;
using System.Collections.Concurrent;
using TrangSach.BUS;
using TrangSach.DAO;

namespace TrangSach.Management.BUS;

public sealed class ManagementApplicationFacade : IDisposable
{
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

    public void Invalidate(params string[] keys)
    {
        foreach (var key in keys)
        {
            _syncCache.TryRemove(key, out _);
        }
    }
    public void Dispose()
    {
        _syncCache.Clear();
    }
}
