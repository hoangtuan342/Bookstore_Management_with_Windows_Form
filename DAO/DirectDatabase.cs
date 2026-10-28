using System.Text.Json;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace TrangSach.DAO;

/// <summary>Connects directly to the shared SQL Server database.</summary>
public static class DirectDatabase
{
    private static readonly string OverridePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TrangSach", "database-connection.txt");

    public static string ConnectionString => LoadConnectionString();
    public static string ConnectionCaption
    {
        get
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString);
            return $"{builder.DataSource} / {builder.InitialCatalog}";
        }
    }

    public static void ValidateConnectionString(string value) => _ = new SqlConnectionStringBuilder(value);

    internal static BookStoreDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BookStoreDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new BookStoreDbContext(options);
    }

    public static async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = CreateContext();
        await BookStoreDbInitializer.InitializeAsync(db, cancellationToken);
    }

    public static void SaveConnectionString(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Connection string không được để trống.", nameof(value));
        Directory.CreateDirectory(Path.GetDirectoryName(OverridePath)!);
        File.WriteAllText(OverridePath, value.Trim());
    }

    private static string LoadConnectionString()
    {
        if (File.Exists(OverridePath)) return File.ReadAllText(OverridePath).Trim();

        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(configPath))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            var connectionString = document.RootElement.GetProperty("ConnectionStrings").GetProperty("BookStore").GetString();
            if (!string.IsNullOrWhiteSpace(connectionString)) return connectionString;
        }

        return "Server=LAPTOP-L9F9N1TE;Database=TrangSachDb;User Id=sa;Password=123456;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
    }
}
