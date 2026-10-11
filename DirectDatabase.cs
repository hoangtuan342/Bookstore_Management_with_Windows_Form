using System.Text.Json;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace TrangSach.WinForms;

/// <summary>Opens the same SQL Server database configured by the original API project.</summary>
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

    public static BookStoreDbContext CreateContext()
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
        if (!File.Exists(configPath))
        {
            var sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", "Quản lý cửa hàng sách", "src", "BookStore.Api", "appsettings.json"));
            if (File.Exists(sourcePath)) configPath = sourcePath;
        }

        if (!File.Exists(configPath))
            throw new FileNotFoundException("Không tìm thấy appsettings.json của dự án gốc để đọc connection string.", configPath);

        using var document = JsonDocument.Parse(File.ReadAllText(configPath));
        var connectionString = document.RootElement.GetProperty("ConnectionStrings").GetProperty("BookStore").GetString();
        return !string.IsNullOrWhiteSpace(connectionString)
            ? connectionString
            : throw new InvalidOperationException("Connection string 'BookStore' không có trong appsettings.json.");
    }
}
