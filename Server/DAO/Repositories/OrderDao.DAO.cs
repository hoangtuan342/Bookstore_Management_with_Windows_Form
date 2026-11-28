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
                   ISNULL((SELECT SUM(c.SL) FROM CTHD c WHERE c.MaHD = h.MaHD), 0),
                   ISNULL(h.TongGiamGia, 0),
                   h.MaVoucher
            FROM HD h
            LEFT JOIN KH kh ON h.MaKH = kh.MaKH
            LEFT JOIN NV nv ON h.MaNV = nv.MaNV
            ORDER BY h.NgayLapHD DESC";
        using var cmd = new SqlCommand(sql, conn);
        using var r = cmd.ExecuteReader();
        var headers = new List<(string Id, DateTime Date, string OS, string PS, string PM, decimal Total, string Cus, string Emp, string Phone, string Addr, int Count, decimal Disc, string? Vch)>();
        while (r.Read())
        {
            headers.Add((
                r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetDecimal(5),
                r.GetString(6), r.GetString(7), r.GetString(8), r.GetString(9), r.GetInt32(10),
                r.GetDecimal(11), r.IsDBNull(12) ? null : r.GetString(12)));
        }
        r.Close();

        foreach (var h in headers)
        {
            var lines = GetOrderLines(h.Id, conn);
            var note = BuildDiscountDescription(h.Disc, h.Vch, lines, conn);
            list.Add(new ManagerOrderRowDto(
                h.Id, h.Date, h.OS, h.PS, h.PM, h.Total,
                h.Cus, h.Emp, h.Phone, h.Addr, h.Count, lines,
                discountAmount: h.Disc, discountDescription: note, voucherCode: h.Vch));
        }

        return list;
    }

    public List<ManagerOrderLineDto> GetOrderLines(string orderId)
    {
        using var conn = DbConnection.Open();
        return GetOrderLines(orderId, conn);
    }

    private List<ManagerOrderLineDto> GetOrderLines(string orderId, SqlConnection conn)
    {
        var list = new List<ManagerOrderLineDto>();
        var sql = @"
            SELECT c.MaSach, ISNULL(s.TenSach, N'Chưa rõ'), c.SL, c.DG, c.ThanhTien,
                   ISNULL(s.DGBan, c.DG) AS GiaGoc,
                   ISNULL((
                       SELECT TOP 1 ctk.TenCTKM
                       FROM KMTheoSach kms
                       JOIN CTKM ctk ON kms.MaCTKM = ctk.MaCTKM
                       WHERE kms.MaSach = c.MaSach
                       ORDER BY ctk.BeginDate DESC
                   ), N'') AS TenKM
            FROM CTHD c
            LEFT JOIN Sach s ON c.MaSach = s.MaSach
            WHERE c.MaHD = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", orderId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var bookId = r.GetString(0);
            var title = r.GetString(1);
            var qty = r.GetInt32(2);
            var dg = r.GetDecimal(3);
            var tt = r.GetDecimal(4);
            var origPrice = r.GetDecimal(5);
            var kmName = r.IsDBNull(6) ? "" : r.GetString(6);
            decimal pct = 0;
            if (origPrice > dg && origPrice > 0)
            {
                pct = decimal.Round((origPrice - dg) * 100m / origPrice, 1);
            }
            list.Add(new ManagerOrderLineDto(bookId, title, qty, dg, tt, origPrice, pct, kmName));
        }
        return list;
    }

    private static string BuildDiscountDescription(decimal totalDiscount, string? voucherCode, IEnumerable<ManagerOrderLineDto> lines, SqlConnection? conn = null)
    {
        if (totalDiscount <= 0) return string.Empty;
        var parts = new List<string>();
        var itemDiscounts = lines.Where(l => l.DiscountPercent > 0).ToList();
        var itemDiscAmount = itemDiscounts.Sum(l => (l.OriginalPrice - l.UnitPrice) * l.Quantity);
        if (itemDiscounts.Count > 0)
        {
            var promoNames = itemDiscounts.Select(l => !string.IsNullOrWhiteSpace(l.PromotionName) ? l.PromotionName : "Giảm giá sản phẩm")
                                          .Distinct().ToList();
            parts.Add($"Giảm theo sách: {string.Join(", ", promoNames)} (-{itemDiscAmount:N0} ₫)");
        }

        var orderDiscAmount = totalDiscount - itemDiscAmount;
        if (orderDiscAmount > 0)
        {
            if (!string.IsNullOrWhiteSpace(voucherCode))
            {
                decimal vPct = 0;
                string vPromo = "";
                if (conn != null)
                {
                    try
                    {
                        using var cmd = new SqlCommand(@"
                            SELECT TOP 1 kv.PhanTramGiam, c.TenCTKM 
                            FROM KMTheoVoucher kv
                            LEFT JOIN CTKM c ON kv.MaCTKM = c.MaCTKM
                            WHERE kv.MaVoucher = @vch
                            ORDER BY kv.PhanTramGiam DESC", conn);
                        cmd.Parameters.AddWithValue("@vch", voucherCode);
                        using var r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            vPct = r.GetDecimal(0);
                            vPromo = r.IsDBNull(1) ? "" : r.GetString(1);
                        }
                    }
                    catch { }
                }
                var promoSuffix = !string.IsNullOrWhiteSpace(vPromo) ? $" ({vPromo})" : "";
                if (vPct > 0)
                    parts.Add($"Voucher {voucherCode}: giảm {vPct:0.#}% (-{orderDiscAmount:N0} ₫){promoSuffix}");
                else
                    parts.Add($"Voucher {voucherCode} (-{orderDiscAmount:N0} ₫)");
            }
            else
            {
                decimal oMin = 0;
                decimal oPct = 0;
                string oPromo = "";
                if (conn != null)
                {
                    try
                    {
                        using var cmd = new SqlCommand(@"
                            SELECT TOP 1 kh.MucTienHD, kh.PhanTramGiam, c.TenCTKM
                            FROM KMTheoHD kh
                            LEFT JOIN CTKM c ON kh.MaCTKM = c.MaCTKM
                            ORDER BY kh.MucTienHD DESC", conn);
                        using var r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            oMin = r.GetDecimal(0);
                            oPct = r.GetDecimal(1);
                            oPromo = r.IsDBNull(2) ? "" : r.GetString(2);
                        }
                    }
                    catch { }
                }
                var promoSuffix = !string.IsNullOrWhiteSpace(oPromo) ? $" ({oPromo})" : "";
                if (oMin > 0 && oPct > 0)
                    parts.Add($"Giảm {oPct:0.#}% cho đơn từ {oMin:N0} ₫ (-{orderDiscAmount:N0} ₫){promoSuffix}");
                else
                    parts.Add($"Ưu đãi hóa đơn (-{orderDiscAmount:N0} ₫)");
            }
        }
        else if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            parts.Add($"Voucher {voucherCode}");
        }

        return string.Join("; ", parts);
    }

    public void InsertOrder(string orderId, DateTime createdAt, string? customerId, string? employeeId, string? voucherId,
        decimal total, decimal discount, string orderStatus, string paymentStatus, string paymentMethod, string shippingAddress, string receiverPhone,
        List<(string BookId, int Qty, decimal Price, decimal LineTotal)> lines, string discountDescription = "")
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
        decimal total, decimal discount, List<(string BookId, int Qty, decimal Price, decimal LineTotal)> lines, string? employeeId = null, string discountDescription = "")
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
                   DiaChiGiaoHang, SDTNhanHang, TongTien, TongGiamGia,
                   MaVoucher
            FROM HD
            WHERE MaKH = @cusId
            ORDER BY NgayLapHD DESC";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@cusId", customerId);
        using var r = cmd.ExecuteReader();
        var headers = new List<(string Id, DateTime Date, string OS, string PS, string PM, string Addr, string Phone, decimal Total, decimal Disc, string? Vch)>();
        while (r.Read())
        {
            headers.Add((
                r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetString(3), r.GetString(4),
                r.IsDBNull(5) ? "" : r.GetString(5),
                r.IsDBNull(6) ? "" : r.GetString(6),
                r.GetDecimal(7), r.GetDecimal(8),
                r.IsDBNull(9) ? null : r.GetString(9)));
        }
        r.Close();

        foreach (var h in headers)
        {
            var lines = new List<StoreOrderLineDto>();
            using var cmdLines = new SqlCommand(@"
                SELECT c.MaSach, ISNULL(s.TenSach, N'Chưa rõ'), c.SL, c.DG, c.ThanhTien, s.CoverImagePath,
                       ISNULL(s.DGBan, c.DG) AS GiaGoc,
                       ISNULL((
                           SELECT TOP 1 ctk.TenCTKM
                           FROM KMTheoSach kms
                           JOIN CTKM ctk ON kms.MaCTKM = ctk.MaCTKM
                           WHERE kms.MaSach = c.MaSach
                           ORDER BY ctk.BeginDate DESC
                       ), N'') AS TenKM
                FROM CTHD c
                LEFT JOIN Sach s ON c.MaSach = s.MaSach
                WHERE c.MaHD = @id", conn);
            cmdLines.Parameters.AddWithValue("@id", h.Id);
            using var rLines = cmdLines.ExecuteReader();
            while (rLines.Read())
            {
                var bookId = rLines.GetString(0);
                var title = rLines.GetString(1);
                var qty = rLines.GetInt32(2);
                var dg = rLines.GetDecimal(3);
                var tt = rLines.GetDecimal(4);
                var cover = rLines.IsDBNull(5) ? null : rLines.GetString(5);
                var origPrice = rLines.GetDecimal(6);
                var kmName = rLines.IsDBNull(7) ? "" : rLines.GetString(7);
                decimal pct = 0;
                if (origPrice > dg && origPrice > 0)
                {
                    pct = decimal.Round((origPrice - dg) * 100m / origPrice, 1);
                }
                lines.Add(new StoreOrderLineDto(bookId, title, qty, dg, tt, cover,
                    originalPrice: origPrice, discountPercent: pct, promotionName: kmName));
            }
            rLines.Close();

            var note = BuildCustomerDiscountDescription(h.Disc, h.Vch, lines, conn);
            list.Add(new StoreOrderDto(
                h.Id, h.Date, h.OS, h.PS, h.PM, h.Addr, h.Phone, h.Total, h.Disc, lines,
                discountDescription: note, voucherCode: h.Vch));
        }

        return list;
    }

    public StoreOrderDto? FindCustomerOrder(string orderId, string customerId)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT MaHD, NgayLapHD, TrangThaiDonHang, TrangThaiThanhToan, PhuongThucThanhToan,
                   DiaChiGiaoHang, SDTNhanHang, TongTien, TongGiamGia,
                   MaVoucher
            FROM HD
            WHERE MaHD = @id AND (MaKH = @cusId OR @cusId = '*')";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", orderId);
        cmd.Parameters.AddWithValue("@cusId", customerId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        var header = (
            Id: r.GetString(0), Date: r.GetDateTime(1), OS: r.GetString(2), PS: r.GetString(3), PM: r.GetString(4),
            Addr: r.IsDBNull(5) ? "" : r.GetString(5), Phone: r.IsDBNull(6) ? "" : r.GetString(6),
            Total: r.GetDecimal(7), Disc: r.GetDecimal(8),
            Vch: r.IsDBNull(9) ? null : r.GetString(9));
        r.Close();

        var lines = new List<StoreOrderLineDto>();
        using var cmdLines = new SqlCommand(@"
            SELECT c.MaSach, ISNULL(s.TenSach, N'Chưa rõ'), c.SL, c.DG, c.ThanhTien, s.CoverImagePath,
                   ISNULL(s.DGBan, c.DG) AS GiaGoc,
                   ISNULL((
                       SELECT TOP 1 ctk.TenCTKM
                       FROM KMTheoSach kms
                       JOIN CTKM ctk ON kms.MaCTKM = ctk.MaCTKM
                       WHERE kms.MaSach = c.MaSach
                       ORDER BY ctk.BeginDate DESC
                   ), N'') AS TenKM
            FROM CTHD c
            LEFT JOIN Sach s ON c.MaSach = s.MaSach
            WHERE c.MaHD = @id", conn);
        cmdLines.Parameters.AddWithValue("@id", header.Id);
        using var rLines = cmdLines.ExecuteReader();
        while (rLines.Read())
        {
            var bookId = rLines.GetString(0);
            var title = rLines.GetString(1);
            var qty = rLines.GetInt32(2);
            var dg = rLines.GetDecimal(3);
            var tt = rLines.GetDecimal(4);
            var cover = rLines.IsDBNull(5) ? null : rLines.GetString(5);
            var origPrice = rLines.GetDecimal(6);
            var kmName = rLines.IsDBNull(7) ? "" : rLines.GetString(7);
            decimal pct = 0;
            if (origPrice > dg && origPrice > 0)
            {
                pct = decimal.Round((origPrice - dg) * 100m / origPrice, 1);
            }
            lines.Add(new StoreOrderLineDto(bookId, title, qty, dg, tt, cover,
                originalPrice: origPrice, discountPercent: pct, promotionName: kmName));
        }

        var note = BuildCustomerDiscountDescription(header.Disc, header.Vch, lines, conn);
        return new StoreOrderDto(
            header.Id, header.Date, header.OS, header.PS, header.PM, header.Addr, header.Phone, header.Total, header.Disc, lines,
            discountDescription: note, voucherCode: header.Vch);
    }

    private static string BuildCustomerDiscountDescription(decimal totalDiscount, string? voucherCode, IEnumerable<StoreOrderLineDto> lines, SqlConnection? conn = null)
    {
        if (totalDiscount <= 0) return string.Empty;
        var parts = new List<string>();
        var itemDiscounts = lines.Where(l => l.DiscountPercent > 0).ToList();
        var itemDiscAmount = itemDiscounts.Sum(l => (l.OriginalPrice - l.UnitPrice) * l.Quantity);
        if (itemDiscounts.Count > 0)
        {
            var promoNames = itemDiscounts.Select(l => !string.IsNullOrWhiteSpace(l.PromotionName) ? l.PromotionName : "Giảm giá sản phẩm")
                                          .Distinct().ToList();
            parts.Add($"Giảm theo sách: {string.Join(", ", promoNames)} (-{itemDiscAmount:N0} ₫)");
        }

        var orderDiscAmount = totalDiscount - itemDiscAmount;
        if (orderDiscAmount > 0)
        {
            if (!string.IsNullOrWhiteSpace(voucherCode))
            {
                decimal vPct = 0;
                string vPromo = "";
                if (conn != null)
                {
                    try
                    {
                        using var cmd = new SqlCommand(@"
                            SELECT TOP 1 kv.PhanTramGiam, c.TenCTKM 
                            FROM KMTheoVoucher kv
                            LEFT JOIN CTKM c ON kv.MaCTKM = c.MaCTKM
                            WHERE kv.MaVoucher = @vch
                            ORDER BY kv.PhanTramGiam DESC", conn);
                        cmd.Parameters.AddWithValue("@vch", voucherCode);
                        using var r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            vPct = r.GetDecimal(0);
                            vPromo = r.IsDBNull(1) ? "" : r.GetString(1);
                        }
                    }
                    catch { }
                }
                var promoSuffix = !string.IsNullOrWhiteSpace(vPromo) ? $" ({vPromo})" : "";
                if (vPct > 0)
                    parts.Add($"Voucher {voucherCode}: giảm {vPct:0.#}% (-{orderDiscAmount:N0} ₫){promoSuffix}");
                else
                    parts.Add($"Voucher {voucherCode} (-{orderDiscAmount:N0} ₫)");
            }
            else
            {
                decimal oMin = 0;
                decimal oPct = 0;
                string oPromo = "";
                if (conn != null)
                {
                    try
                    {
                        using var cmd = new SqlCommand(@"
                            SELECT TOP 1 kh.MucTienHD, kh.PhanTramGiam, c.TenCTKM
                            FROM KMTheoHD kh
                            LEFT JOIN CTKM c ON kh.MaCTKM = c.MaCTKM
                            ORDER BY kh.MucTienHD DESC", conn);
                        using var r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            oMin = r.GetDecimal(0);
                            oPct = r.GetDecimal(1);
                            oPromo = r.IsDBNull(2) ? "" : r.GetString(2);
                        }
                    }
                    catch { }
                }
                var promoSuffix = !string.IsNullOrWhiteSpace(oPromo) ? $" ({oPromo})" : "";
                if (oMin > 0 && oPct > 0)
                    parts.Add($"Giảm {oPct:0.#}% cho đơn từ {oMin:N0} ₫ (-{orderDiscAmount:N0} ₫){promoSuffix}");
                else
                    parts.Add($"Ưu đãi hóa đơn (-{orderDiscAmount:N0} ₫)");
            }
        }
        else if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            parts.Add($"Voucher {voucherCode}");
        }

        return string.Join("; ", parts);
    }
}
