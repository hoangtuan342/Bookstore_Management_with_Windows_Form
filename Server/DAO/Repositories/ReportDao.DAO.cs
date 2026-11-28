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
    // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
    // -----------------------------------------------------------------------
    public decimal GetMonthRevenue(DateTime monthStart)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT ISNULL(SUM(TongTien),0) FROM HD WHERE TrangThaiDonHang='Completed' AND NgayLapHD >= @d", conn);
        cmd.Parameters.AddWithValue("@d", monthStart);
        return (decimal)(cmd.ExecuteScalar() ?? 0m);
    }

    public int CountOrdersToday(DateTime todayUtc)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM HD WHERE NgayLapHD >= @d", conn);
        cmd.Parameters.AddWithValue("@d", todayUtc);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public int CountOrdersByStatus(string status)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM HD WHERE TrangThaiDonHang = @s", conn);
        cmd.Parameters.AddWithValue("@s", status);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public int CountActiveBooks()
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM Sach WHERE TrangThai = 1", conn);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public int CountLowStockBooks(int threshold)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM Sach WHERE TrangThai = 1 AND SLCo <= @t", conn);
        cmd.Parameters.AddWithValue("@t", threshold);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public int CountCustomers()
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM KH", conn);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public List<ManagerRecentOrderDto> GetRecentOrders(int take)
    {
        var list = new List<ManagerRecentOrderDto>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT TOP (@take) h.MaHD, h.NgayLapHD, h.TrangThaiDonHang, h.TongTien,
                   ISNULL(kh.HoTen, N'Khách vãng lai')
            FROM HD h
            LEFT JOIN KH kh ON h.MaKH = kh.MaKH
            ORDER BY h.NgayLapHD DESC";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@take", take);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new ManagerRecentOrderDto(r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetDecimal(3), r.GetString(4)));
        return list;
    }

    public List<(string Id, DateTime CreatedAt, string OrderStatus, decimal Total, int ItemCount)>
        GetAllOrdersInPeriod(DateTime fromUtc, DateTime untilUtc)
    {
        var list = new List<(string, DateTime, string, decimal, int)>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT h.MaHD, h.NgayLapHD, h.TrangThaiDonHang, h.TongTien,
                   ISNULL((SELECT SUM(SL) FROM CTHD c WHERE c.MaHD = h.MaHD), 0)
            FROM HD h
            WHERE h.NgayLapHD >= @from AND h.NgayLapHD < @until";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add((r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetDecimal(3), r.GetInt32(4)));
        return list;
    }

    public List<(string OrderId, DateTime CreatedAt, string CustomerId, string CustomerName,
        string? EmployeeId, string EmployeeName, decimal Total, decimal Discount)>
        GetCompletedOrdersInPeriod(DateTime fromUtc, DateTime untilUtc)
    {
        var list = new List<(string, DateTime, string, string, string?, string, decimal, decimal)>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT h.MaHD, h.NgayLapHD, ISNULL(h.MaKH,''), ISNULL(kh.HoTen,N'Khách vãng lai'),
                   h.MaNV, ISNULL(nv.HoLot+' '+nv.Ten,N''), h.TongTien, h.TongGiamGia
            FROM HD h
            LEFT JOIN KH kh ON h.MaKH = kh.MaKH
            LEFT JOIN NV nv ON h.MaNV = nv.MaNV
            WHERE h.TrangThaiDonHang = 'Completed'
              AND h.NgayLapHD >= @from AND h.NgayLapHD < @until";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add((r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetString(3),
                r.IsDBNull(4) ? null : r.GetString(4), r.GetString(5), r.GetDecimal(6), r.GetDecimal(7)));
        return list;
    }

    public List<(string OrderId, string BookId, string BookTitle, string? CategoryName, int Qty, decimal UnitPrice, decimal LineTotal)>
        GetOrderLinesForPeriod(DateTime fromUtc, DateTime untilUtc)
    {
        var list = new List<(string, string, string, string?, int, decimal, decimal)>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT c.MaHD, c.MaSach, ISNULL(s.TenSach, c.MaSach),
                   ISNULL(ls.TenLoai, N'Chưa phân loại'), c.SL, c.DG, c.ThanhTien
            FROM CTHD c
            JOIN HD h ON c.MaHD = h.MaHD
            LEFT JOIN Sach s ON c.MaSach = s.MaSach
            LEFT JOIN LoaiSach ls ON s.MaLoai = ls.MaLoai
            WHERE h.TrangThaiDonHang = 'Completed'
              AND h.NgayLapHD >= @from AND h.NgayLapHD < @until";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add((r.GetString(0), r.GetString(1), r.GetString(2),
                r.IsDBNull(3) ? null : r.GetString(3), r.GetInt32(4), r.GetDecimal(5), r.GetDecimal(6)));
        return list;
    }

    public decimal GetPreviousPeriodRevenue(DateTime fromUtc, DateTime untilUtc)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand(
            "SELECT ISNULL(SUM(TongTien),0) FROM HD WHERE TrangThaiDonHang='Completed' AND NgayLapHD>=@from AND NgayLapHD<@until", conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        return (decimal)(cmd.ExecuteScalar() ?? 0m);
    }

    public int GetPreviousPeriodOrderCount(DateTime fromUtc, DateTime untilUtc)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand(
            "SELECT COUNT(1) FROM HD WHERE TrangThaiDonHang='Completed' AND NgayLapHD>=@from AND NgayLapHD<@until", conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public List<(string BookId, string? CategoryName, int Stock, decimal SalePrice)> GetActiveInventory()
    {
        var list = new List<(string, string?, int, decimal)>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT s.MaSach, ISNULL(ls.TenLoai, N'Chưa phân loại'), s.SLCo, s.DGBan
            FROM Sach s
            LEFT JOIN LoaiSach ls ON s.MaLoai = ls.MaLoai
            WHERE s.TrangThai = 1";
        using var cmd = new SqlCommand(sql, conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add((r.GetString(0), r.IsDBNull(1) ? null : r.GetString(1), r.GetInt32(2), r.GetDecimal(3)));
        return list;
    }

    public Dictionary<string, decimal> GetAveragePurchaseCosts()
    {
        var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT c.MaSach, AVG(c.DG)
            FROM CTPNH c
            JOIN PNH p ON c.MaPNH = p.MaPNH
            WHERE p.TrangThai = 'Completed'
            GROUP BY c.MaSach";
        using var cmd = new SqlCommand(sql, conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            dict[r.GetString(0)] = r.GetDecimal(1);
        return dict;
    }

    public decimal GetPurchaseSpendInPeriod(DateTime fromUtc, DateTime untilUtc)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT ISNULL(SUM(c.ThanhTien), 0)
            FROM CTPNH c
            JOIN PNH p ON c.MaPNH = p.MaPNH
            WHERE p.TrangThai = 'Completed' AND p.NgayLap >= @from AND p.NgayLap < @until";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        return (decimal)(cmd.ExecuteScalar() ?? 0m);
    }

    public List<(string Id, DateTime CreatedAt, decimal Total)> GetCompletedPurchasesInPeriod(DateTime fromUtc, DateTime untilUtc)
    {
        var list = new List<(string, DateTime, decimal)>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT p.MaPNH, p.NgayLap, ISNULL(SUM(c.ThanhTien), p.TongTien)
            FROM PNH p
            LEFT JOIN CTPNH c ON c.MaPNH = p.MaPNH
            WHERE p.TrangThai = 'Completed' AND p.NgayLap >= @from AND p.NgayLap < @until
            GROUP BY p.MaPNH, p.NgayLap, p.TongTien";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@from", fromUtc);
        cmd.Parameters.AddWithValue("@until", untilUtc);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add((r.GetString(0), r.GetDateTime(1), r.GetDecimal(2)));
        return list;
    }

    // -----------------------------------------------------------------------
    // Dashboard
    // -----------------------------------------------------------------------
}
