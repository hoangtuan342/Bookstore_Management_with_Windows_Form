using System.Data;
using Microsoft.Data.SqlClient;
using TrangSach.Shared.DTO;

namespace TrangSach.DAO;

public class PromotionDao
{
    public async Task<List<ManagerPromotionDto>> GetAllPromotionsAsync(CancellationToken ct = default)
    {
        var list = new List<ManagerPromotionDto>();
        await using var conn = await DbConnection.OpenAsync(ct).ConfigureAwait(false);
        var sql = "SELECT MaCTKM, TenCTKM, BeginDate, EndDate FROM CTKM ORDER BY BeginDate DESC";
        await using var cmd = new SqlCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var promos = new List<(string Id, string Name, DateTime Start, DateTime End)>();
        while (await r.ReadAsync(ct).ConfigureAwait(false))
            promos.Add((r.GetString(0), r.GetString(1), r.GetDateTime(2), r.GetDateTime(3)));
        await r.CloseAsync().ConfigureAwait(false);

        foreach (var p in promos)
        {
            var bookRules = new List<PromotionBookRuleDto>();
            var bookOffers = new List<string>();
            await using var cmdBook = new SqlCommand(@"
                SELECT b.MaSach, ISNULL(s.TenSach, b.MaSach), b.PhanTramGiam
                FROM KMTheoSach b LEFT JOIN Sach s ON b.MaSach = s.MaSach
                WHERE b.MaCTKM = @id", conn);
            cmdBook.Parameters.AddWithValue("@id", p.Id);
            await using var rBook = await cmdBook.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await rBook.ReadAsync(ct).ConfigureAwait(false))
            {
                var pct = rBook.GetDecimal(2);
                bookRules.Add(new PromotionBookRuleDto(rBook.GetString(0), pct));
                bookOffers.Add($"{rBook.GetString(1)}: {pct:0.#}%");
            }
            await rBook.CloseAsync().ConfigureAwait(false);

            var orderRules = new List<PromotionOrderRuleDto>();
            var orderOffers = new List<string>();
            await using var cmdOrder = new SqlCommand("SELECT MucTienHD, PhanTramGiam FROM KMTheoHD WHERE MaCTKM = @id", conn);
            cmdOrder.Parameters.AddWithValue("@id", p.Id);
            await using var rOrder = await cmdOrder.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await rOrder.ReadAsync(ct).ConfigureAwait(false))
            {
                var minAmount = rOrder.GetDecimal(0);
                var pct = rOrder.GetDecimal(1);
                orderRules.Add(new PromotionOrderRuleDto(minAmount, pct));
                orderOffers.Add($"Từ {minAmount:N0}₫: {pct:0.#}%");
            }
            await rOrder.CloseAsync().ConfigureAwait(false);

            var voucherRules = new List<PromotionVoucherRuleDto>();
            var voucherOffers = new List<string>();
            await using var cmdVch = new SqlCommand("SELECT MaVoucher, PhanTramGiam FROM KMTheoVoucher WHERE MaCTKM = @id", conn);
            cmdVch.Parameters.AddWithValue("@id", p.Id);
            await using var rVch = await cmdVch.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await rVch.ReadAsync(ct).ConfigureAwait(false))
            {
                var pct = rVch.GetDecimal(1);
                voucherRules.Add(new PromotionVoucherRuleDto(rVch.GetString(0), pct));
                voucherOffers.Add($"{rVch.GetString(0)}: {pct:0.#}%");
            }
            await rVch.CloseAsync().ConfigureAwait(false);

            list.Add(new ManagerPromotionDto(p.Id, p.Name, p.Start, p.End, bookOffers, orderOffers, voucherOffers, bookRules, orderRules, voucherRules));
        }
        return list;
    }

    public async Task InsertPromotionAsync(string id, string name, DateTime startsAt, DateTime endsAt,
        (string BookId, decimal DiscountPercent)? bookRule,
        (decimal MinOrder, decimal DiscountPercent)? orderRule,
        (string Code, decimal DiscountPercent, int? MaxUses)? voucherRule,
        CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = conn.BeginTransaction();
        try
        {
            var sql = "INSERT INTO CTKM (MaCTKM, TenCTKM, BeginDate, EndDate) VALUES (@id, @name, @s, @e)";
            await using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@name", name.Trim());
                cmd.Parameters.AddWithValue("@s", startsAt);
                cmd.Parameters.AddWithValue("@e", endsAt);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            if (bookRule.HasValue)
            {
                var sqlBook = "INSERT INTO KMTheoSach (MaCTKM, MaSach, PhanTramGiam) VALUES (@id, @b, @pct)";
                await using var cmdBook = new SqlCommand(sqlBook, conn, tx);
                cmdBook.Parameters.AddWithValue("@id", id);
                cmdBook.Parameters.AddWithValue("@b", bookRule.Value.BookId);
                cmdBook.Parameters.AddWithValue("@pct", bookRule.Value.DiscountPercent);
                await cmdBook.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            if (orderRule.HasValue)
            {
                var sqlOrder = "INSERT INTO KMTheoHD (MaCTKM, MucTienHD, PhanTramGiam) VALUES (@id, @m, @pct)";
                await using var cmdOrder = new SqlCommand(sqlOrder, conn, tx);
                cmdOrder.Parameters.AddWithValue("@id", id);
                cmdOrder.Parameters.AddWithValue("@m", orderRule.Value.MinOrder);
                cmdOrder.Parameters.AddWithValue("@pct", orderRule.Value.DiscountPercent);
                await cmdOrder.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            if (voucherRule.HasValue)
            {
                var code = voucherRule.Value.Code.ToUpperInvariant();
                var sqlVoucher = @"
                    IF NOT EXISTS (SELECT 1 FROM Voucher WHERE MaVoucher = @code)
                        INSERT INTO Voucher (MaVoucher, TrangThai, HanSuDung, SoLanToiDa, SoLanDaSuDung)
                        VALUES (@code, 'Active', @exp, @max, 0)";
                await using (var cmdV = new SqlCommand(sqlVoucher, conn, tx))
                {
                    cmdV.Parameters.AddWithValue("@code", code);
                    cmdV.Parameters.AddWithValue("@exp", endsAt);
                    cmdV.Parameters.AddWithValue("@max", (object?)voucherRule.Value.MaxUses ?? DBNull.Value);
                    await cmdV.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                var sqlLink = "INSERT INTO KMTheoVoucher (MaCTKM, MaVoucher, PhanTramGiam) VALUES (@id, @code, @pct)";
                await using var cmdLink = new SqlCommand(sqlLink, conn, tx);
                cmdLink.Parameters.AddWithValue("@id", id);
                cmdLink.Parameters.AddWithValue("@code", code);
                cmdLink.Parameters.AddWithValue("@pct", voucherRule.Value.DiscountPercent);
                await cmdLink.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task UpdatePromotionDetailsAsync(string id, string name, DateTime startsAt, DateTime endsAt, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct).ConfigureAwait(false);
        var sql = "UPDATE CTKM SET TenCTKM = @name, BeginDate = @s, EndDate = @e WHERE MaCTKM = @id";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", name.Trim());
        cmd.Parameters.AddWithValue("@s", startsAt);
        cmd.Parameters.AddWithValue("@e", endsAt);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdatePromotionRulesAsync(string id,
        IReadOnlyList<PromotionBookRuleDto> books,
        IReadOnlyList<PromotionOrderRuleDto> orders,
        IReadOnlyList<PromotionVoucherRuleDto> vouchers,
        CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = conn.BeginTransaction();
        try
        {
            await using (var cmd = new SqlCommand(@"
                DELETE FROM KMTheoSach WHERE MaCTKM = @id;
                DELETE FROM KMTheoHD WHERE MaCTKM = @id;
                DELETE FROM KMTheoVoucher WHERE MaCTKM = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            foreach (var b in books)
            {
                await using var cmdB = new SqlCommand("INSERT INTO KMTheoSach (MaCTKM, MaSach, PhanTramGiam) VALUES (@id, @b, @pct)", conn, tx);
                cmdB.Parameters.AddWithValue("@id", id);
                cmdB.Parameters.AddWithValue("@b", b.BookId);
                cmdB.Parameters.AddWithValue("@pct", b.DiscountPercent);
                await cmdB.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            foreach (var o in orders)
            {
                await using var cmdO = new SqlCommand("INSERT INTO KMTheoHD (MaCTKM, MucTienHD, PhanTramGiam) VALUES (@id, @m, @pct)", conn, tx);
                cmdO.Parameters.AddWithValue("@id", id);
                cmdO.Parameters.AddWithValue("@m", o.MinimumOrderAmount);
                cmdO.Parameters.AddWithValue("@pct", o.DiscountPercent);
                await cmdO.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            foreach (var v in vouchers)
            {
                await using var cmdV = new SqlCommand("INSERT INTO KMTheoVoucher (MaCTKM, MaVoucher, PhanTramGiam) VALUES (@id, @v, @pct)", conn, tx);
                cmdV.Parameters.AddWithValue("@id", id);
                cmdV.Parameters.AddWithValue("@v", v.VoucherId.ToUpperInvariant());
                cmdV.Parameters.AddWithValue("@pct", v.DiscountPercent);
                await cmdV.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task DeletePromotionAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = conn.BeginTransaction();
        try
        {
            await using (var cmd = new SqlCommand(@"
                DELETE FROM KMTheoSach WHERE MaCTKM = @id;
                DELETE FROM KMTheoHD WHERE MaCTKM = @id;
                DELETE FROM KMTheoVoucher WHERE MaCTKM = @id;
                DELETE FROM CTKM WHERE MaCTKM = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // Voucher
    // -----------------------------------------------------------------------
    public async Task<List<VoucherSummaryDto>> GetAllVouchersAsync(CancellationToken ct = default)
    {
        var list = new List<VoucherSummaryDto>();
        await using var conn = await DbConnection.OpenAsync(ct).ConfigureAwait(false);
        var sql = @"
            SELECT v.MaVoucher, v.TrangThai, v.HanSuDung, v.SoLanToiDa, v.SoLanDaSuDung,
                   ISNULL((SELECT COUNT(1) FROM HD WHERE MaVoucher = v.MaVoucher), 0)
            FROM Voucher v
            ORDER BY v.HanSuDung DESC";
        await using var cmd = new SqlCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var vchs = new List<(string Id, string Status, DateTime? Exp, int? Max, int Used, int Orders)>();
        while (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            vchs.Add((
                r.GetString(0), r.GetString(1),
                r.IsDBNull(2) ? null : r.GetDateTime(2),
                r.IsDBNull(3) ? null : r.GetInt32(3),
                r.GetInt32(4), r.GetInt32(5)));
        }
        await r.CloseAsync().ConfigureAwait(false);

        foreach (var v in vchs)
        {
            var promos = new List<string>();
            await using var cmdP = new SqlCommand(@"
                SELECT c.TenCTKM, pv.PhanTramGiam
                FROM KMTheoVoucher pv
                JOIN CTKM c ON pv.MaCTKM = c.MaCTKM
                WHERE pv.MaVoucher = @code", conn);
            cmdP.Parameters.AddWithValue("@code", v.Id);
            await using var rP = await cmdP.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await rP.ReadAsync(ct).ConfigureAwait(false))
            {
                var pct = rP.GetDecimal(1);
                promos.Add($"{rP.GetString(0)}: {pct:0.#}%");
            }
            await rP.CloseAsync().ConfigureAwait(false);

            list.Add(new VoucherSummaryDto(v.Id, v.Status, v.Exp, v.Max, v.Used, promos, v.Orders));
        }

        return list;
    }

    public async Task<VoucherSummaryDto?> FindVoucherAsync(string code, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct).ConfigureAwait(false);
        var sql = @"
            SELECT v.MaVoucher, v.TrangThai, v.HanSuDung, v.SoLanToiDa, v.SoLanDaSuDung,
                   ISNULL((SELECT COUNT(1) FROM HD WHERE MaVoucher = v.MaVoucher), 0)
            FROM Voucher v
            WHERE v.MaVoucher = @code";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@code", code.ToUpperInvariant());
        await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await r.ReadAsync(ct).ConfigureAwait(false)) return null;

        var v = (
            Id: r.GetString(0), Status: r.GetString(1),
            Exp: r.IsDBNull(2) ? null : (DateTime?)r.GetDateTime(2),
            Max: r.IsDBNull(3) ? null : (int?)r.GetInt32(3),
            Used: r.GetInt32(4), Orders: r.GetInt32(5));
        await r.CloseAsync().ConfigureAwait(false);

        var promos = new List<string>();
        await using var cmdP = new SqlCommand(@"
            SELECT c.TenCTKM, pv.PhanTramGiam
            FROM KMTheoVoucher pv
            JOIN CTKM c ON pv.MaCTKM = c.MaCTKM
            WHERE pv.MaVoucher = @code", conn);
        cmdP.Parameters.AddWithValue("@code", v.Id);
        await using var rP = await cmdP.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await rP.ReadAsync(ct).ConfigureAwait(false))
        {
            var pct = rP.GetDecimal(1);
            promos.Add($"{rP.GetString(0)}: {pct:0.#}%");
        }

        return new VoucherSummaryDto(v.Id, v.Status, v.Exp, v.Max, v.Used, promos, v.Orders);
    }

    public async Task UpdateVoucherAsync(string code, string status, DateTime? expiresAt, int? maxUsageCount, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct).ConfigureAwait(false);
        var sql = "UPDATE Voucher SET TrangThai = @s, HanSuDung = @e, SoLanToiDa = @m WHERE MaVoucher = @code";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@code", code.ToUpperInvariant());
        cmd.Parameters.AddWithValue("@s", status);
        cmd.Parameters.AddWithValue("@e", (object?)expiresAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@m", (object?)maxUsageCount ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteVoucherAsync(string code, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = conn.BeginTransaction();
        try
        {
            await using (var cmd = new SqlCommand(@"
                DELETE FROM KMTheoVoucher WHERE MaVoucher = @code;
                DELETE FROM Voucher WHERE MaVoucher = @code", conn, tx))
            {
                cmd.Parameters.AddWithValue("@code", code.ToUpperInvariant());
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<bool> VoucherHasBeenUsedAsync(string code, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct).ConfigureAwait(false);
        var sql = "SELECT COUNT(1) FROM HD WHERE MaVoucher = @code";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@code", code.ToUpperInvariant());
        return (int)(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) ?? 0) > 0;
    }
}
