using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Collections.Concurrent;
using TrangSach.BUS;
using TrangSach.BUS.Services;

namespace TrangSach.Management.Services;

internal sealed class ManagementAppServices : IDisposable
{
    private readonly DatabaseService _database = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<object>>> _readCache = new(StringComparer.Ordinal);
    public DirectCustomerClient Store { get; }
    public DirectManagerClient Manager { get; }
    public DirectBookCatalog Catalog { get; }
    public string DatabaseConnectionString => _database.ConnectionString;
    public string DatabaseConnectionCaption => _database.ConnectionCaption;

    public ManagementAppServices()
    {
        var passwordService = new WinFormsPasswordHashingService();
        Store = new DirectCustomerClient(passwordService);
        Manager = new DirectManagerClient(new ManagerApplicationService(Store, passwordService));
        Catalog = new DirectBookCatalog();
    }

    public Task InitializeDatabaseAsync() => _database.InitializeAsync();
    public void SaveDatabaseConnectionString(string value) => _database.SaveConnectionString(value);
    public void ValidateDatabaseConnectionString(string value) => _database.ValidateConnectionString(value);
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

    private sealed class WinFormsPasswordHashingService : IPasswordHashingService
    {
        private readonly PasswordHasher<UserAccount> _hasher = new();
        public string Hash(UserAccount account, string password) => _hasher.HashPassword(account, password);
        public bool Verify(UserAccount account, string password) =>
            _hasher.VerifyHashedPassword(account, account.PasswordHash, password) != PasswordVerificationResult.Failed;
    }
}
