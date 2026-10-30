using TrangSach.DAO;

namespace TrangSach.Management.BUS;

/// <summary>Business-layer entry point for database setup and connection settings.</summary>
public sealed class DatabaseService
{
    public string ConnectionString => DirectDatabase.ConnectionString;
    public string ConnectionCaption => DirectDatabase.ConnectionCaption;
    public Task InitializeAsync(CancellationToken cancellationToken = default) => DirectDatabase.InitializeAsync(cancellationToken);
    public void SaveConnectionString(string value) => DirectDatabase.SaveConnectionString(value);
    public void ValidateConnectionString(string value) => DirectDatabase.ValidateConnectionString(value);
}
