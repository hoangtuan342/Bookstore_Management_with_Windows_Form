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

    public static void TestConnection()
    {
        using var conn = Open();
        EnsureDatabaseSchema(conn);
    }

    private static void EnsureDatabaseSchema(SqlConnection conn)
    {
        try
        {
            // Tự động kiểm tra và thêm cột NgaySinh vào bảng KH nếu chưa tồn tại
            var checkColumnSql = @"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'dbo.KH') AND name = 'NgaySinh'
                )
                BEGIN
                    ALTER TABLE dbo.KH ADD NgaySinh DATE NULL;
                END";
            using var cmd = new SqlCommand(checkColumnSql, conn);
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Bỏ qua nếu không có quyền DDL hoặc cột đã có
        }
    }

    public static int ExecuteNonQuery(string sql, SqlParameter[]? parameters = null, SqlTransaction? tx = null)
    {
        if (tx != null)
        {
            using var cmd = new SqlCommand(sql, tx.Connection, tx);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        using var conn = Open();
        using var cmd2 = new SqlCommand(sql, conn);
        if (parameters != null) cmd2.Parameters.AddRange(parameters);
        return cmd2.ExecuteNonQuery();
    }

    public static object? ExecuteScalar(string sql, SqlParameter[]? parameters = null, SqlTransaction? tx = null)
    {
        if (tx != null)
        {
            using var cmd = new SqlCommand(sql, tx.Connection, tx);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteScalar();
        }

        using var conn = Open();
        using var cmd2 = new SqlCommand(sql, conn);
        if (parameters != null) cmd2.Parameters.AddRange(parameters);
        return cmd2.ExecuteScalar();
    }
}
