using System.Data;
using Microsoft.Data.SqlClient;
using TrangSach.Shared.DTO;

namespace TrangSach.DAO;

/// <summary>
/// DAO báo cáo: thực thi SQL aggregation trực tiếp để tạo báo cáo kinh doanh.
/// </summary>
public class ReportDao
{
    // -----------------------------------------------------------------------
    // Dashboard
    // -----------------------------------------------------------------------
    public async Task<decimal> GetMonthRevenueAsync(DateTime monthStart, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            "SELECT ISNULL(SUM(TongTien),0) FROM HD WHERE TrangThaiDonHang='Completed' AND NgayLapHD >= @d", conn);
        cmd.Parameters.AddWithValue("@d", monthStart);
        return (decimal)(await cmd.ExecuteScalarAsync(ct) ?? 0m);
    }

    public async Task<int> CountOrdersTodayAsync(DateTime todayUtc, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            "SELECT COUNT(1) FROM HD WHERE NgayLapHD >= @d", conn);
        cmd.Parameters.AddWithValue("@d", todayUtc);
        return (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
    }

    public async Task<int> CountOrdersByStatusAsync(string status, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            "SELECT COUNT(1) FROM HD WHERE TrangThaiDonHang = @s", conn);
        cmd.Parameters.AddWithValue("@s", status);
        return (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
    }

    public async Task<int> CountActiveBooksAsync(CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand("SELECT COUNT(1) FROM Sach WHERE TrangThai = 1", conn);
        return (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
    }

    public async Task<int> CountLowStockBooksAsync(int threshold, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            "SELECT COUNT(1) FROM Sach WHERE TrangThai = 1 AND SLCo <= @t", conn);
        cmd.Parameters.AddWithValue("@t", threshold);
        return (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
    }

    public async Task<int> CountCustomersAsync(CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand("SELECT COUNT(1) FROM KH", conn);
        return (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
    }

    public async Task<List<ManagerRecentOrderDto>> GetRecentOrdersAsync(int take, CancellationToken ct = default)
    {
        var list = new List<ManagerRecentOrderDto>();
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT TOP (@take) h.MaHD, h.NgayLapHD, h.TrangThaiDonHang, h.TongTien,
                   ISNULL(kh.HoTen, N'Khách vãng lai')
            FROM HD h
            LEFT JOIN KH kh ON h.MaKH = kh.MaKH
            ORDER BY h.NgayLapHD DESC";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@take", take);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(new ManagerRecentOrderDto(r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetDecimal(3), r.GetString(4)));
        return list;
    }

    // -----------------------------------------------------------------------
    // Reports
    // -----------------------------------------------------------------------
    public async Task<List<(string Id, DateTime CreatedAt, string OrderStatus, decimal Total, int ItemCount)>>
        GetAllOrdersInPeriodAsync(DateTime fromUtc, DateTime untilUtc, CancellationToken ct = default)
    {
        var list = new List<(string, DateTime, string, decimal, int)>();
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT h.MaHD, h.NgayLapHD, h.TrangThaiDonHang, h.TongTien,
                   ISNULL((SELECT SUM(SL) FROM CTHD c WHERE c.MaHD = h.MaHD), 0)
            FROM HD h
            WHERE h.NgayLapHD >= @from AND h.NgayLapHD < @until";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add((r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetDecimal(3), r.GetInt32(4)));
        return list;
    }

    public async Task<List<(string OrderId, DateTime CreatedAt, string CustomerId, string CustomerName,
        string? EmployeeId, string EmployeeName, decimal Total, decimal Discount)>>
        GetCompletedOrdersInPeriodAsync(DateTime fromUtc, DateTime untilUtc, CancellationToken ct = default)
    {
        var list = new List<(string, DateTime, string, string, string?, string, decimal, decimal)>();
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT h.MaHD, h.NgayLapHD, ISNULL(h.MaKH,''), ISNULL(kh.HoTen,N'Khách vãng lai'),
                   h.MaNV, ISNULL(nv.HoLot+' '+nv.Ten,N''), h.TongTien, h.TongGiamGia
            FROM HD h
            LEFT JOIN KH kh ON h.MaKH = kh.MaKH
            LEFT JOIN NV nv ON h.MaNV = nv.MaNV
            WHERE h.TrangThaiDonHang = 'Completed'
              AND h.NgayLapHD >= @from AND h.NgayLapHD < @until";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add((r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetString(3),
                r.IsDBNull(4) ? null : r.GetString(4), r.GetString(5), r.GetDecimal(6), r.GetDecimal(7)));
        return list;
    }

    public async Task<List<(string OrderId, string BookId, string BookTitle, string? CategoryName, int Qty, decimal UnitPrice, decimal LineTotal)>>
        GetOrderLinesForPeriodAsync(DateTime fromUtc, DateTime untilUtc, CancellationToken ct = default)
    {
        var list = new List<(string, string, string, string?, int, decimal, decimal)>();
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT c.MaHD, c.MaSach, ISNULL(s.TenSach, c.MaSach),
                   ISNULL(ls.TenLoai, N'Chưa phân loại'), c.SL, c.DG, c.ThanhTien
            FROM CTHD c
            JOIN HD h ON c.MaHD = h.MaHD
            LEFT JOIN Sach s ON c.MaSach = s.MaSach
            LEFT JOIN LoaiSach ls ON s.MaLoai = ls.MaLoai
            WHERE h.TrangThaiDonHang = 'Completed'
              AND h.NgayLapHD >= @from AND h.NgayLapHD < @until";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add((r.GetString(0), r.GetString(1), r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3), r.GetInt32(4), r.GetDecimal(5), r.GetDecimal(6)));
        return list;
    }

    public async Task<decimal> GetPreviousPeriodRevenueAsync(DateTime fromUtc, DateTime untilUtc, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            "SELECT ISNULL(SUM(TongTien),0) FROM HD WHERE TrangThaiDonHang='Completed' AND NgayLapHD>=@from AND NgayLapHD<@until", conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        return (decimal)(await cmd.ExecuteScalarAsync(ct) ?? 0m);
    }

    public async Task<int> GetPreviousPeriodOrderCountAsync(DateTime fromUtc, DateTime untilUtc, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand(
            "SELECT COUNT(1) FROM HD WHERE TrangThaiDonHang='Completed' AND NgayLapHD>=@from AND NgayLapHD<@until", conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        return (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
    }

    public async Task<List<(string BookId, string? CategoryName, int Stock, decimal SalePrice)>>
        GetActiveInventoryAsync(CancellationToken ct = default)
    {
        var list = new List<(string, string?, int, decimal)>();
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT s.MaSach, ISNULL(ls.TenLoai, N'Chưa phân loại'), s.SLCo, s.DGBan
            FROM Sach s
            LEFT JOIN LoaiSach ls ON s.MaLoai = ls.MaLoai
            WHERE s.TrangThai = 1";
        await using var cmd = new SqlCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add((r.GetString(0), r.IsDBNull(1) ? null : r.GetString(1), r.GetInt32(2), r.GetDecimal(3)));
        return list;
    }

    public async Task<Dictionary<string, decimal>> GetAveragePurchaseCostsAsync(CancellationToken ct = default)
    {
        var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT c.MaSach, AVG(c.DG)
            FROM CTPNH c
            JOIN PNH p ON c.MaPNH = p.MaPNH
            WHERE p.TrangThai = 'Completed'
            GROUP BY c.MaSach";
        await using var cmd = new SqlCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            dict[r.GetString(0)] = r.GetDecimal(1);
        return dict;
    }

    public async Task<decimal> GetPurchaseSpendInPeriodAsync(DateTime fromUtc, DateTime untilUtc, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT ISNULL(SUM(c.ThanhTien), 0)
            FROM CTPNH c
            JOIN PNH p ON c.MaPNH = p.MaPNH
            WHERE p.TrangThai = 'Completed' AND p.NgayLap >= @from AND p.NgayLap < @until";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        return (decimal)(await cmd.ExecuteScalarAsync(ct) ?? 0m);
    }
}
