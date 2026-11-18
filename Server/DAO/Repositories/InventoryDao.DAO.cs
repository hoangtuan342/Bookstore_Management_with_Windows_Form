using System.Data;
using Microsoft.Data.SqlClient;
using TrangSach.Shared.DTO;

namespace TrangSach.DAO;

public class InventoryDao
{
    public async Task<List<SupplierOptionDto>> GetAllSuppliersAsync(CancellationToken ct = default)
    {
        var list = new List<SupplierOptionDto>();
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand("SELECT MaNCC, TenNCC, SoDienThoai FROM NCC ORDER BY TenNCC", conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(new SupplierOptionDto(r.GetString(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2)));
        return list;
    }

    public async Task InsertSupplierAsync(SupplierOptionDto s, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand("INSERT INTO NCC (MaNCC, TenNCC, SoDienThoai) VALUES (@id, @name, @phone)", conn);
        cmd.Parameters.AddWithValue("@id", s.Id);
        cmd.Parameters.AddWithValue("@name", s.Name);
        cmd.Parameters.AddWithValue("@phone", (object?)s.PhoneNumber ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateSupplierAsync(string id, SupplierCommandDto cmdDto, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand("UPDATE NCC SET TenNCC = @name, SoDienThoai = @phone WHERE MaNCC = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", cmdDto.Name.Trim());
        cmd.Parameters.AddWithValue("@phone", (object?)cmdDto.PhoneNumber?.Trim() ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteSupplierAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand("DELETE FROM NCC WHERE MaNCC = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> SupplierHasReceiptsAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand("SELECT COUNT(1) FROM PNH WHERE MaNCC = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        var count = (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
        return count > 0;
    }

    public async Task<List<ManagerReceiptDto>> GetRecentReceiptsAsync(int take = 100, CancellationToken ct = default)
    {
        var list = new List<ManagerReceiptDto>();
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT TOP (@take) p.MaPNH, p.NgayLap, p.TrangThai, p.MaNCC, ISNULL(ncc.TenNCC, N'Chưa có'), p.TongTien
            FROM PNH p
            LEFT JOIN NCC ncc ON p.MaNCC = ncc.MaNCC
            ORDER BY p.NgayLap DESC";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@take", take);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var receipts = new List<(string Id, DateTime CreatedAt, string Status, string SupplierId, string SupplierName, decimal Total)>();
        while (await r.ReadAsync(ct))
        {
            receipts.Add((r.GetString(0), r.GetDateTime(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetDecimal(5)));
        }
        await r.CloseAsync();

        foreach (var rec in receipts)
        {
            var lines = new List<ManagerReceiptLineDto>();
            await using var cmdLines = new SqlCommand(@"
                SELECT c.MaSach, ISNULL(s.TenSach, N'Chưa rõ'), c.SL, c.DG, c.ThanhTien
                FROM CTPNH c
                LEFT JOIN Sach s ON c.MaSach = s.MaSach
                WHERE c.MaPNH = @id", conn);
            cmdLines.Parameters.AddWithValue("@id", rec.Id);
            await using var rLines = await cmdLines.ExecuteReaderAsync(ct);
            while (await rLines.ReadAsync(ct))
            {
                lines.Add(new ManagerReceiptLineDto(rLines.GetString(0), rLines.GetString(1), rLines.GetInt32(2), rLines.GetDecimal(3), rLines.GetDecimal(4)));
            }
            await rLines.CloseAsync();

            list.Add(new ManagerReceiptDto(rec.Id, rec.CreatedAt, rec.Status, rec.SupplierId, rec.SupplierName, rec.Total, lines.ToArray()));
        }

        return list;
    }

    public async Task InsertReceiptAsync(string receiptId, DateTime createdAt, string? employeeId, string supplierId, decimal total, IReadOnlyList<(string BookId, int Qty, decimal Cost, decimal LineTotal)> lines, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var tx = conn.BeginTransaction();
        try
        {
            await using (var cmd = new SqlCommand("INSERT INTO PNH (MaPNH, NgayLap, MaNV, MaNCC, TongTien, TrangThai) VALUES (@id, @date, @emp, @sup, @tot, 'Completed')", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", receiptId);
                cmd.Parameters.AddWithValue("@date", createdAt);
                cmd.Parameters.AddWithValue("@emp", (object?)employeeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sup", supplierId);
                cmd.Parameters.AddWithValue("@tot", total);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            foreach (var line in lines)
            {
                await using (var cmdLine = new SqlCommand("INSERT INTO CTPNH (MaPNH, MaSach, SL, DG, ThanhTien) VALUES (@id, @bookId, @qty, @dg, @tt)", conn, tx))
                {
                    cmdLine.Parameters.AddWithValue("@id", receiptId);
                    cmdLine.Parameters.AddWithValue("@bookId", line.BookId);
                    cmdLine.Parameters.AddWithValue("@qty", line.Qty);
                    cmdLine.Parameters.AddWithValue("@dg", line.Cost);
                    cmdLine.Parameters.AddWithValue("@tt", line.LineTotal);
                    await cmdLine.ExecuteNonQueryAsync(ct);
                }

                await using (var cmdStock = new SqlCommand("UPDATE Sach SET SLCo = SLCo + @qty WHERE MaSach = @bookId", conn, tx))
                {
                    cmdStock.Parameters.AddWithValue("@qty", line.Qty);
                    cmdStock.Parameters.AddWithValue("@bookId", line.BookId);
                    await cmdStock.ExecuteNonQueryAsync(ct);
                }
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
