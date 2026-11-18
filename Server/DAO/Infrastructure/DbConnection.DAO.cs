using System.Data;
using Microsoft.Data.SqlClient;

namespace TrangSach.DAO;

/// <summary>
/// Quản lý kết nối SQL Server và thực thi câu lệnh SQL thuần (theo phong cách MySqlConnect trong giáo trình).
/// </summary>
public static class DbConnection
{
    public const string ConnectionString = "Server=LAPTOP-L9F9N1TE;Database=TrangSachDb;User Id=sa;Password=123456;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=15";

    public static string ConnectionCaption
    {
        get
        {
            var b = new SqlConnectionStringBuilder(ConnectionString);
            return $"{b.DataSource} / {b.InitialCatalog}";
        }
    }

    public static SqlConnection Create() => new(ConnectionString);

    public static SqlConnection Open()
    {
        var conn = Create();
        conn.Open();
        return conn;
    }

    public static async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var conn = Create();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        return conn;
    }

    public static async Task TestConnectionAsync(CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
    }

    public static async Task<int> ExecuteNonQueryAsync(string sql, SqlParameter[]? parameters = null, SqlTransaction? tx = null, CancellationToken ct = default)
    {
        if (tx != null)
        {
            await using var cmd = new SqlCommand(sql, tx.Connection, tx);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        await using var cmd2 = new SqlCommand(sql, conn);
        if (parameters != null) cmd2.Parameters.AddRange(parameters);
        return await cmd2.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public static async Task<object?> ExecuteScalarAsync(string sql, SqlParameter[]? parameters = null, SqlTransaction? tx = null, CancellationToken ct = default)
    {
        if (tx != null)
        {
            await using var cmd = new SqlCommand(sql, tx.Connection, tx);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        }

        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        await using var cmd2 = new SqlCommand(sql, conn);
        if (parameters != null) cmd2.Parameters.AddRange(parameters);
        return await cmd2.ExecuteScalarAsync(ct).ConfigureAwait(false);
    }
}
