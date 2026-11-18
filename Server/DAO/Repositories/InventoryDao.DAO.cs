using System.Data;
using Microsoft.Data.SqlClient;
using TrangSach.Shared.DTO;

namespace TrangSach.DAO;

public class InventoryDao
{
    // -----------------------------------------------------------------------
    // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
    // -----------------------------------------------------------------------
    public List<SupplierOptionDto> GetAllSuppliers()
    {
        var list = new List<SupplierOptionDto>();
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT MaNCC, TenNCC, SoDienThoai FROM NCC ORDER BY TenNCC", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new SupplierOptionDto(r.GetString(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2)));
        return list;
    }

    public void InsertSupplier(SupplierOptionDto s)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("INSERT INTO NCC (MaNCC, TenNCC, SoDienThoai) VALUES (@id, @name, @phone)", conn);
        cmd.Parameters.AddWithValue("@id", s.Id);
        cmd.Parameters.AddWithValue("@name", s.Name);
        cmd.Parameters.AddWithValue("@phone", (object?)s.PhoneNumber ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void UpdateSupplier(string id, SupplierCommandDto cmdDto)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("UPDATE NCC SET TenNCC = @name, SoDienThoai = @phone WHERE MaNCC = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", cmdDto.Name.Trim());
        cmd.Parameters.AddWithValue("@phone", (object?)cmdDto.PhoneNumber?.Trim() ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void DeleteSupplier(string id)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("DELETE FROM NCC WHERE MaNCC = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public bool SupplierHasReceipts(string id)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM PNH WHERE MaNCC = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return (int)(cmd.ExecuteScalar() ?? 0) > 0;
    }

    public List<ManagerReceiptDto> GetRecentReceipts(int take = 100)
    {
        var list = new List<ManagerReceiptDto>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT TOP (@take) p.MaPNH, p.NgayLap, p.TrangThai, p.MaNCC, ISNULL(ncc.TenNCC, N'Chưa có'), p.TongTien
            FROM PNH p
            LEFT JOIN NCC ncc ON p.MaNCC = ncc.MaNCC
            ORDER BY p.NgayLap DESC";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@take", take);
        using var r = cmd.ExecuteReader();
        var receipts = new List<(string Id, DateTime CreatedAt, string Status, string SupplierId, string SupplierName, decimal Total)>();
        while (r.Read()) receipts.Add((r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetDecimal(5)));
        r.Close();

        foreach (var rec in receipts)
        {
            var lines = new List<ManagerReceiptLineDto>();
            using var cmdLines = new SqlCommand(@"
                SELECT c.MaSach, ISNULL(s.TenSach, N'Chưa rõ'), c.SL, c.DG, c.ThanhTien
                FROM CTPNH c
                LEFT JOIN Sach s ON c.MaSach = s.MaSach
                WHERE c.MaPNH = @id", conn);
            cmdLines.Parameters.AddWithValue("@id", rec.Id);
            using var rLines = cmdLines.ExecuteReader();
            while (rLines.Read()) lines.Add(new ManagerReceiptLineDto(rLines.GetString(0), rLines.GetString(1), rLines.GetInt32(2), rLines.GetDecimal(3), rLines.GetDecimal(4)));
            rLines.Close();

            list.Add(new ManagerReceiptDto(rec.Id, rec.CreatedAt, rec.Status, rec.SupplierId, rec.SupplierName, rec.Total, lines));
        }

        return list;
    }

    public void InsertReceipt(string receiptId, DateTime createdAt, string? employeeId, string supplierId, decimal total, List<(string BookId, int Qty, decimal Cost, decimal LineTotal)> lines)
    {
        using var conn = DbConnection.Open();

        // Nếu employeeId chưa có, tự động lấy nhân viên đầu tiên trong hệ thống
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            using var cmdFindEmp = new SqlCommand("SELECT TOP 1 MaNV FROM NV ORDER BY MaNV", conn);
            var found = cmdFindEmp.ExecuteScalar()?.ToString();
            if (!string.IsNullOrWhiteSpace(found)) employeeId = found;
        }

        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand("INSERT INTO PNH (MaPNH, NgayLap, MaNV, MaNCC, TongTien, TrangThai) VALUES (@id, @date, @emp, @sup, @tot, 'Completed')", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", receiptId);
                cmd.Parameters.AddWithValue("@date", createdAt);
                cmd.Parameters.AddWithValue("@emp", (object?)employeeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sup", supplierId);
                cmd.Parameters.AddWithValue("@tot", total);
                cmd.ExecuteNonQuery();
            }

            foreach (var line in lines)
            {
                using (var cmdLine = new SqlCommand("INSERT INTO CTPNH (MaPNH, MaSach, SL, DG, ThanhTien) VALUES (@id, @bookId, @qty, @dg, @tt)", conn, tx))
                {
                    cmdLine.Parameters.AddWithValue("@id", receiptId);
                    cmdLine.Parameters.AddWithValue("@bookId", line.BookId);
                    cmdLine.Parameters.AddWithValue("@qty", line.Qty);
                    cmdLine.Parameters.AddWithValue("@dg", line.Cost);
                    cmdLine.Parameters.AddWithValue("@tt", line.LineTotal);
                    cmdLine.ExecuteNonQuery();
                }

                using (var cmdStock = new SqlCommand("UPDATE Sach SET SLCo = SLCo + @qty WHERE MaSach = @bookId", conn, tx))
                {
                    cmdStock.Parameters.AddWithValue("@qty", line.Qty);
                    cmdStock.Parameters.AddWithValue("@bookId", line.BookId);
                    cmdStock.ExecuteNonQuery();
                }
            }

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }
}
