using BookStore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using TrangSach.BUS;
using TrangSach.BUS.Services;

namespace TrangSach.Management.Services;

internal sealed class ManagementAppServices : IDisposable
{
    private readonly DatabaseService _database = new();
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
    public void Dispose() { }

    private sealed class WinFormsPasswordHashingService : IPasswordHashingService
    {
        private readonly PasswordHasher<UserAccount> _hasher = new();
        public string Hash(UserAccount account, string password) => _hasher.HashPassword(account, password);
        public bool Verify(UserAccount account, string password) =>
            _hasher.VerifyHashedPassword(account, account.PasswordHash, password) != PasswordVerificationResult.Failed;
    }
}
