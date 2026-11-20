using System.Data;
using Microsoft.Data.SqlClient;
using TrangSach.Shared.DTO;

namespace TrangSach.DAO;

public class OrderDao
{
    // -----------------------------------------------------------------------
    // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
    // -----------------------------------------------------------------------
    public List<ManagerOrderRowDto> GetAllOrders()
    {
        var list = new List<ManagerOrderRowDto>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT h.MaHD, h.NgayLapHD, h.TrangThaiDonHang, h.TrangThaiThanhToan, h.PhuongThucThanhToan, h.TongTien,
                   ISNULL(kh.HoTen, N'Khách vãng lai'),
                   ISNULL(nv.HoLot + ' ' + nv.Ten, N''),
                   ISNULL(h.SDTNhanHang, N''),
                   ISNULL(h.DiaChiGiaoHang, N''),
                   ISNULL((SELECT SUM(c.SL) FROM CTHD c WHERE c.MaHD = h.MaHD), 0)
            FROM HD h
            LEFT JOIN KH kh ON h.MaKH = kh.MaKH
            LEFT JOIN NV nv ON h.MaNV = nv.MaNV
            ORDER BY h.NgayLapHD DESC";
        using var cmd = new SqlCommand(sql, conn);
        using var r = cmd.ExecuteReader();
        var headers = new List<(string Id, DateTime Date, string OS, string PS, string PM, decimal Total, string Cus, string Emp, string Phone, string Addr, int Count)>();
        while (r.Read())
        {
            headers.Add((
                r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetDecimal(5),
                r.GetString(6), r.GetString(7), r.GetString(8), r.GetString(9), r.GetInt32(10)));
        }
        r.Close();

        foreach (var h in headers)
        {
            var lines = GetOrderLines(h.Id);
            list.Add(new ManagerOrderRowDto(
                h.Id, h.Date, h.OS, h.PS, h.PM, h.Total,
                h.Cus, h.Emp, h.Phone, h.Addr, h.Count, lines));
        }

        return list;
    }

    public List<ManagerOrderLineDto> GetOrderLines(string orderId)
    {
        var list = new List<ManagerOrderLineDto>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT c.MaSach, ISNULL(s.TenSach, N'Chưa rõ'), c.SL, c.DG, c.ThanhTien
            FROM CTHD c
            LEFT JOIN Sach s ON c.MaSach = s.MaSach
            WHERE c.MaHD = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", orderId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ManagerOrderLineDto(r.GetString(0), r.GetString(1), r.GetInt32(2), r.GetDecimal(3), r.GetDecimal(4)));
        }
        return list;
    }

    public void InsertOrder(string orderId, DateTime createdAt, string? customerId, string? employeeId, string? voucherId,
        decimal total, decimal discount, string orderStatus, string paymentStatus, string paymentMethod, string shippingAddress, string receiverPhone,
        List<(string BookId, int Qty, decimal Price, decimal LineTotal)> lines)
    {
        using var conn = DbConnection.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var sqlOrder = @"INSERT INTO HD (MaHD, NgayLapHD, MaNV, MaKH, TongTien, TongGiamGia, TrangThaiDonHang, TrangThaiThanhToan, PhuongThucThanhToan, DiaChiGiaoHang, SDTNhanHang, MaVoucher)
                             VALUES (@id, @date, @emp, @cus, @tot, @disc, @os, @ps, @pm, @addr, @phone, @vch)";
            using (var cmd = new SqlCommand(sqlOrder, conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", orderId);
                cmd.Parameters.AddWithValue("@date", createdAt);
                cmd.Parameters.AddWithValue("@emp", (object?)employeeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cus", (object?)customerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tot", total);
                cmd.Parameters.AddWithValue("@disc", discount);
                cmd.Parameters.AddWithValue("@os", orderStatus);
                cmd.Parameters.AddWithValue("@ps", paymentStatus);
                cmd.Parameters.AddWithValue("@pm", paymentMethod);
                cmd.Parameters.AddWithValue("@addr", shippingAddress);
                cmd.Parameters.AddWithValue("@phone", receiverPhone);
                cmd.Parameters.AddWithValue("@vch", (object?)voucherId ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            foreach (var line in lines)
            {
                using (var cmdLine = new SqlCommand("INSERT INTO CTHD (MaHD, MaSach, SL, DG, ThanhTien) VALUES (@id, @bookId, @qty, @dg, @tt)", conn, tx))
                {
                    cmdLine.Parameters.AddWithValue("@id", orderId);
                    cmdLine.Parameters.AddWithValue("@bookId", line.BookId);
                    cmdLine.Parameters.AddWithValue("@qty", line.Qty);
                    cmdLine.Parameters.AddWithValue("@dg", line.Price);
                    cmdLine.Parameters.AddWithValue("@tt", line.LineTotal);
                    cmdLine.ExecuteNonQuery();
                }

                using (var cmdStock = new SqlCommand("UPDATE Sach SET SLCo = CASE WHEN SLCo >= @qty THEN SLCo - @qty ELSE 0 END WHERE MaSach = @bookId", conn, tx))
                {
                    cmdStock.Parameters.AddWithValue("@qty", line.Qty);
                    cmdStock.Parameters.AddWithValue("@bookId", line.BookId);
                    cmdStock.ExecuteNonQuery();
                }
            }

            if (!string.IsNullOrWhiteSpace(voucherId))
            {
                using var cmdVoucher = new SqlCommand("UPDATE Voucher SET SoLanDaSuDung = SoLanDaSuDung + 1 WHERE MaVoucher = @vch", conn, tx);
                cmdVoucher.Parameters.AddWithValue("@vch", voucherId);
                cmdVoucher.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public void UpdateOrderStatus(string orderId, string orderStatus, string? paymentStatus)
    {
        using var conn = DbConnection.Open();
        var sql = paymentStatus != null
            ? "UPDATE HD SET TrangThaiDonHang = @os, TrangThaiThanhToan = @ps WHERE MaHD = @id"
            : "UPDATE HD SET TrangThaiDonHang = @os WHERE MaHD = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", orderId);
        cmd.Parameters.AddWithValue("@os", orderStatus);
        if (paymentStatus != null) cmd.Parameters.AddWithValue("@ps", paymentStatus);
        cmd.ExecuteNonQuery();
    }

    public void UpdateOrderInfo(string orderId, string shippingAddress, string receiverPhone, string paymentMethod, string? employeeId = null)
    {
        using var conn = DbConnection.Open();
        var sql = "UPDATE HD SET DiaChiGiaoHang = @addr, SDTNhanHang = @phone, PhuongThucThanhToan = @pm, MaNV = ISNULL(@emp, MaNV) WHERE MaHD = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", orderId);
        cmd.Parameters.AddWithValue("@addr", shippingAddress);
        cmd.Parameters.AddWithValue("@phone", receiverPhone);
        cmd.Parameters.AddWithValue("@pm", paymentMethod);
        cmd.Parameters.AddWithValue("@emp", (object?)employeeId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void UpdateOrderFull(string orderId, string shippingAddress, string receiverPhone, string paymentMethod, string paymentStatus,
        decimal total, decimal discount, List<(string BookId, int Qty, decimal Price, decimal LineTotal)> lines, string? employeeId = null)
    {
        using var conn = DbConnection.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            // 1. Phục hồi số lượng tồn kho của các sách cũ trong CTHD
            using (var cmdRestore = new SqlCommand(@"
                UPDATE s
                SET s.SLCo = s.SLCo + c.SL
                FROM Sach s
                JOIN CTHD c ON s.MaSach = c.MaSach
                WHERE c.MaHD = @id", conn, tx))
            {
                cmdRestore.Parameters.AddWithValue("@id", orderId);
                cmdRestore.ExecuteNonQuery();
            }

            // 2. Xóa các dòng CTHD cũ
            using (var cmdDelLines = new SqlCommand("DELETE FROM CTHD WHERE MaHD = @id", conn, tx))
            {
                cmdDelLines.Parameters.AddWithValue("@id", orderId);
                cmdDelLines.ExecuteNonQuery();
            }

            // 3. Cập nhật thông tin đơn hàng
            using (var cmdUpdateOrder = new SqlCommand(@"
                UPDATE HD
                SET DiaChiGiaoHang = @addr,
                    SDTNhanHang = @phone,
                    PhuongThucThanhToan = @pm,
                    TrangThaiThanhToan = @ps,
                    TongTien = @tot,
                    TongGiamGia = @disc,
                    MaNV = ISNULL(@emp, MaNV)
                WHERE MaHD = @id", conn, tx))
            {
                cmdUpdateOrder.Parameters.AddWithValue("@id", orderId);
                cmdUpdateOrder.Parameters.AddWithValue("@addr", shippingAddress);
                cmdUpdateOrder.Parameters.AddWithValue("@phone", receiverPhone);
                cmdUpdateOrder.Parameters.AddWithValue("@pm", paymentMethod);
                cmdUpdateOrder.Parameters.AddWithValue("@ps", paymentStatus);
                cmdUpdateOrder.Parameters.AddWithValue("@tot", total);
                cmdUpdateOrder.Parameters.AddWithValue("@disc", discount);
                cmdUpdateOrder.Parameters.AddWithValue("@emp", (object?)employeeId ?? DBNull.Value);
                cmdUpdateOrder.ExecuteNonQuery();
            }

            // 4. Thêm các dòng CTHD mới và trừ kho tương ứng
            foreach (var line in lines)
            {
                using (var cmdLine = new SqlCommand("INSERT INTO CTHD (MaHD, MaSach, SL, DG, ThanhTien) VALUES (@id, @bookId, @qty, @dg, @tt)", conn, tx))
                {
                    cmdLine.Parameters.AddWithValue("@id", orderId);
                    cmdLine.Parameters.AddWithValue("@bookId", line.BookId);
                    cmdLine.Parameters.AddWithValue("@qty", line.Qty);
                    cmdLine.Parameters.AddWithValue("@dg", line.Price);
                    cmdLine.Parameters.AddWithValue("@tt", line.LineTotal);
                    cmdLine.ExecuteNonQuery();
                }

                using (var cmdStock = new SqlCommand("UPDATE Sach SET SLCo = CASE WHEN SLCo >= @qty THEN SLCo - @qty ELSE 0 END WHERE MaSach = @bookId", conn, tx))
                {
                    cmdStock.Parameters.AddWithValue("@qty", line.Qty);
                    cmdStock.Parameters.AddWithValue("@bookId", line.BookId);
                    cmdStock.ExecuteNonQuery();
                }
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void RestoreStock(string orderId)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            UPDATE s
            SET s.SLCo = s.SLCo + c.SL
            FROM Sach s
            JOIN CTHD c ON s.MaSach = c.MaSach
            WHERE c.MaHD = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", orderId);
        cmd.ExecuteNonQuery();
    }

    public void DecrementVoucherUsage(string orderId)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            UPDATE v
            SET v.SoLanDaSuDung = CASE WHEN v.SoLanDaSuDung > 0 THEN v.SoLanDaSuDung - 1 ELSE 0 END
            FROM Voucher v
            JOIN HD h ON v.MaVoucher = h.MaVoucher
            WHERE h.MaHD = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", orderId);
        cmd.ExecuteNonQuery();
    }

    public List<StoreOrderDto> GetCustomerOrders(string customerId)
    {
        var list = new List<StoreOrderDto>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT MaHD, NgayLapHD, TrangThaiDonHang, TrangThaiThanhToan, PhuongThucThanhToan,
                   DiaChiGiaoHang, SDTNhanHang, TongTien, TongGiamGia
            FROM HD
            WHERE MaKH = @cusId
            ORDER BY NgayLapHD DESC";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@cusId", customerId);
        using var r = cmd.ExecuteReader();
        var headers = new List<(string Id, DateTime Date, string OS, string PS, string PM, string Addr, string Phone, decimal Total, decimal Disc)>();
        while (r.Read())
        {
            headers.Add((
                r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetString(3), r.GetString(4),
                r.IsDBNull(5) ? "" : r.GetString(5),
                r.IsDBNull(6) ? "" : r.GetString(6),
                r.GetDecimal(7), r.GetDecimal(8)));
        }
        r.Close();

        foreach (var h in headers)
        {
            var lines = new List<StoreOrderLineDto>();
            using var cmdLines = new SqlCommand(@"
                SELECT c.MaSach, ISNULL(s.TenSach, N'Chưa rõ'), c.SL, c.DG, c.ThanhTien
                FROM CTHD c
                LEFT JOIN Sach s ON c.MaSach = s.MaSach
                WHERE c.MaHD = @id", conn);
            cmdLines.Parameters.AddWithValue("@id", h.Id);
            using var rLines = cmdLines.ExecuteReader();
            while (rLines.Read())
            {
                lines.Add(new StoreOrderLineDto(rLines.GetString(0), rLines.GetString(1), rLines.GetInt32(2), rLines.GetDecimal(3), rLines.GetDecimal(4)));
            }
            rLines.Close();

            list.Add(new StoreOrderDto(
                h.Id, h.Date, h.OS, h.PS, h.PM, h.Addr, h.Phone, h.Total, h.Disc, lines));
        }

        return list;
    }

    public StoreOrderDto? FindCustomerOrder(string orderId, string customerId)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT MaHD, NgayLapHD, TrangThaiDonHang, TrangThaiThanhToan, PhuongThucThanhToan,
                   DiaChiGiaoHang, SDTNhanHang, TongTien, TongGiamGia
            FROM HD
            WHERE MaHD = @id AND MaKH = @cusId";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", orderId);
        cmd.Parameters.AddWithValue("@cusId", customerId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        var header = (
            Id: r.GetString(0), Date: r.GetDateTime(1), OS: r.GetString(2), PS: r.GetString(3), PM: r.GetString(4),
            Addr: r.IsDBNull(5) ? "" : r.GetString(5), Phone: r.IsDBNull(6) ? "" : r.GetString(6),
            Total: r.GetDecimal(7), Disc: r.GetDecimal(8));
        r.Close();

        var lines = new List<StoreOrderLineDto>();
        using var cmdLines = new SqlCommand(@"
            SELECT c.MaSach, ISNULL(s.TenSach, N'Chưa rõ'), c.SL, c.DG, c.ThanhTien
            FROM CTHD c
            LEFT JOIN Sach s ON c.MaSach = s.MaSach
            WHERE c.MaHD = @id", conn);
        cmdLines.Parameters.AddWithValue("@id", header.Id);
        using var rLines = cmdLines.ExecuteReader();
        while (rLines.Read())
        {
            lines.Add(new StoreOrderLineDto(rLines.GetString(0), rLines.GetString(1), rLines.GetInt32(2), rLines.GetDecimal(3), rLines.GetDecimal(4)));
        }

        return new StoreOrderDto(
            header.Id, header.Date, header.OS, header.PS, header.PM, header.Addr, header.Phone, header.Total, header.Disc, lines);
    }
}
