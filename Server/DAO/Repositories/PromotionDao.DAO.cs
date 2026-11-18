using System.Data;
using Microsoft.Data.SqlClient;
using TrangSach.Shared.DTO;

namespace TrangSach.DAO;

public class PromotionDao
{
    // -----------------------------------------------------------------------
    // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
    // -----------------------------------------------------------------------
    public List<ManagerPromotionDto> GetAllPromotions()
    {
        var list = new List<ManagerPromotionDto>();
        using var conn = DbConnection.Open();
        var sql = "SELECT MaCTKM, TenCTKM, BeginDate, EndDate FROM CTKM ORDER BY BeginDate DESC";
        using var cmd = new SqlCommand(sql, conn);
        using var r = cmd.ExecuteReader();
        var promos = new List<(string Id, string Name, DateTime Start, DateTime End)>();
        while (r.Read()) promos.Add((r.GetString(0), r.GetString(1), r.GetDateTime(2), r.GetDateTime(3)));
        r.Close();

        foreach (var p in promos)
        {
            var bookRules = new List<PromotionBookRuleDto>();
            var bookOffers = new List<string>();
            using (var cmdBook = new SqlCommand(@"
                SELECT b.MaSach, ISNULL(s.TenSach, b.MaSach), b.PhanTramGiam
                FROM KMTheoSach b LEFT JOIN Sach s ON b.MaSach = s.MaSach
                WHERE b.MaCTKM = @id", conn))
            {
                cmdBook.Parameters.AddWithValue("@id", p.Id);
                using var rBook = cmdBook.ExecuteReader();
                while (rBook.Read())
                {
                    var pct = rBook.GetDecimal(2);
                    bookRules.Add(new PromotionBookRuleDto(rBook.GetString(0), pct));
                    bookOffers.Add($"{rBook.GetString(1)}: {pct:0.#}%");
                }
            }

            var orderRules = new List<PromotionOrderRuleDto>();
            var orderOffers = new List<string>();
            using (var cmdOrder = new SqlCommand("SELECT MucTienHD, PhanTramGiam FROM KMTheoHD WHERE MaCTKM = @id", conn))
            {
                cmdOrder.Parameters.AddWithValue("@id", p.Id);
                using var rOrder = cmdOrder.ExecuteReader();
                while (rOrder.Read())
                {
                    var minAmount = rOrder.GetDecimal(0);
                    var pct = rOrder.GetDecimal(1);
                    orderRules.Add(new PromotionOrderRuleDto(minAmount, pct));
                    orderOffers.Add($"Từ {minAmount:N0}₫: {pct:0.#}%");
                }
            }

            var voucherRules = new List<PromotionVoucherRuleDto>();
            var voucherOffers = new List<string>();
            using (var cmdVch = new SqlCommand("SELECT MaVoucher, PhanTramGiam FROM KMTheoVoucher WHERE MaCTKM = @id", conn))
            {
                cmdVch.Parameters.AddWithValue("@id", p.Id);
                using var rVch = cmdVch.ExecuteReader();
                while (rVch.Read())
                {
                    var pct = rVch.GetDecimal(1);
                    voucherRules.Add(new PromotionVoucherRuleDto(rVch.GetString(0), pct));
                    voucherOffers.Add($"{rVch.GetString(0)}: {pct:0.#}%");
                }
            }

            list.Add(new ManagerPromotionDto(p.Id, p.Name, p.Start, p.End, bookOffers, orderOffers, voucherOffers, bookRules, orderRules, voucherRules));
        }
        return list;
    }

    public void InsertPromotion(string id, string name, DateTime startsAt, DateTime endsAt,
        (string BookId, decimal DiscountPercent)? bookRule,
        (decimal MinOrder, decimal DiscountPercent)? orderRule,
        (string Code, decimal DiscountPercent, int? MaxUses)? voucherRule)
    {
        using var conn = DbConnection.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var sql = "INSERT INTO CTKM (MaCTKM, TenCTKM, BeginDate, EndDate) VALUES (@id, @name, @s, @e)";
            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@name", name.Trim());
                cmd.Parameters.AddWithValue("@s", startsAt);
                cmd.Parameters.AddWithValue("@e", endsAt);
                cmd.ExecuteNonQuery();
            }

            if (bookRule.HasValue)
            {
                var sqlBook = "INSERT INTO KMTheoSach (MaCTKM, MaSach, PhanTramGiam) VALUES (@id, @b, @pct)";
                using var cmdBook = new SqlCommand(sqlBook, conn, tx);
                cmdBook.Parameters.AddWithValue("@id", id);
                cmdBook.Parameters.AddWithValue("@b", bookRule.Value.BookId);
                cmdBook.Parameters.AddWithValue("@pct", bookRule.Value.DiscountPercent);
                cmdBook.ExecuteNonQuery();
            }

            if (orderRule.HasValue)
            {
                var sqlOrder = "INSERT INTO KMTheoHD (MaCTKM, MucTienHD, PhanTramGiam) VALUES (@id, @m, @pct)";
                using var cmdOrder = new SqlCommand(sqlOrder, conn, tx);
                cmdOrder.Parameters.AddWithValue("@id", id);
                cmdOrder.Parameters.AddWithValue("@m", orderRule.Value.MinOrder);
                cmdOrder.Parameters.AddWithValue("@pct", orderRule.Value.DiscountPercent);
                cmdOrder.ExecuteNonQuery();
            }

            if (voucherRule.HasValue)
            {
                var code = voucherRule.Value.Code.ToUpperInvariant();
                var sqlVoucher = @"
                    IF NOT EXISTS (SELECT 1 FROM Voucher WHERE MaVoucher = @code)
                        INSERT INTO Voucher (MaVoucher, TrangThai, HanSuDung, SoLanToiDa, SoLanDaSuDung)
                        VALUES (@code, 'Active', @exp, @max, 0)";
                using (var cmdV = new SqlCommand(sqlVoucher, conn, tx))
                {
                    cmdV.Parameters.AddWithValue("@code", code);
                    cmdV.Parameters.AddWithValue("@exp", endsAt);
                    cmdV.Parameters.AddWithValue("@max", (object?)voucherRule.Value.MaxUses ?? DBNull.Value);
                    cmdV.ExecuteNonQuery();
                }

                var sqlLink = "INSERT INTO KMTheoVoucher (MaCTKM, MaVoucher, PhanTramGiam) VALUES (@id, @code, @pct)";
                using var cmdLink = new SqlCommand(sqlLink, conn, tx);
                cmdLink.Parameters.AddWithValue("@id", id);
                cmdLink.Parameters.AddWithValue("@code", code);
                cmdLink.Parameters.AddWithValue("@pct", voucherRule.Value.DiscountPercent);
                cmdLink.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public void UpdatePromotionDetails(string id, string name, DateTime startsAt, DateTime endsAt)
    {
        using var conn = DbConnection.Open();
        var sql = "UPDATE CTKM SET TenCTKM = @name, BeginDate = @s, EndDate = @e WHERE MaCTKM = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", name.Trim());
        cmd.Parameters.AddWithValue("@s", startsAt);
        cmd.Parameters.AddWithValue("@e", endsAt);
        cmd.ExecuteNonQuery();
    }

    public void UpdatePromotionRules(string id,
        IReadOnlyList<PromotionBookRuleDto> books,
        IReadOnlyList<PromotionOrderRuleDto> orders,
        IReadOnlyList<PromotionVoucherRuleDto> vouchers)
    {
        using var conn = DbConnection.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand(@"
                DELETE FROM KMTheoSach WHERE MaCTKM = @id;
                DELETE FROM KMTheoHD WHERE MaCTKM = @id;
                DELETE FROM KMTheoVoucher WHERE MaCTKM = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }

            foreach (var b in books)
            {
                using var cmdB = new SqlCommand("INSERT INTO KMTheoSach (MaCTKM, MaSach, PhanTramGiam) VALUES (@id, @b, @pct)", conn, tx);
                cmdB.Parameters.AddWithValue("@id", id);
                cmdB.Parameters.AddWithValue("@b", b.BookId);
                cmdB.Parameters.AddWithValue("@pct", b.DiscountPercent);
                cmdB.ExecuteNonQuery();
            }

            foreach (var o in orders)
            {
                using var cmdO = new SqlCommand("INSERT INTO KMTheoHD (MaCTKM, MucTienHD, PhanTramGiam) VALUES (@id, @m, @pct)", conn, tx);
                cmdO.Parameters.AddWithValue("@id", id);
                cmdO.Parameters.AddWithValue("@m", o.MinimumOrderAmount);
                cmdO.Parameters.AddWithValue("@pct", o.DiscountPercent);
                cmdO.ExecuteNonQuery();
            }

            foreach (var v in vouchers)
            {
                using var cmdV = new SqlCommand("INSERT INTO KMTheoVoucher (MaCTKM, MaVoucher, PhanTramGiam) VALUES (@id, @v, @pct)", conn, tx);
                cmdV.Parameters.AddWithValue("@id", id);
                cmdV.Parameters.AddWithValue("@v", v.VoucherId.ToUpperInvariant());
                cmdV.Parameters.AddWithValue("@pct", v.DiscountPercent);
                cmdV.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public void DeletePromotion(string id)
    {
        using var conn = DbConnection.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand(@"
                DELETE FROM KMTheoSach WHERE MaCTKM = @id;
                DELETE FROM KMTheoHD WHERE MaCTKM = @id;
                DELETE FROM KMTheoVoucher WHERE MaCTKM = @id;
                DELETE FROM CTKM WHERE MaCTKM = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public List<VoucherSummaryDto> GetAllVouchers()
    {
        var list = new List<VoucherSummaryDto>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT v.MaVoucher, v.TrangThai, v.HanSuDung, v.SoLanToiDa, v.SoLanDaSuDung,
                   ISNULL((SELECT COUNT(1) FROM HD WHERE MaVoucher = v.MaVoucher), 0)
            FROM Voucher v
            ORDER BY v.HanSuDung DESC";
        using var cmd = new SqlCommand(sql, conn);
        using var r = cmd.ExecuteReader();
        var vchs = new List<(string Id, string Status, DateTime? Exp, int? Max, int Used, int Orders)>();
        while (r.Read())
        {
            vchs.Add((
                r.GetString(0), r.GetString(1),
                r.IsDBNull(2) ? null : r.GetDateTime(2),
                r.IsDBNull(3) ? null : r.GetInt32(3),
                r.GetInt32(4), r.GetInt32(5)));
        }
        r.Close();

        foreach (var v in vchs)
        {
            var promos = new List<string>();
            using (var cmdP = new SqlCommand(@"
                SELECT c.TenCTKM, pv.PhanTramGiam
                FROM KMTheoVoucher pv
                JOIN CTKM c ON pv.MaCTKM = c.MaCTKM
                WHERE pv.MaVoucher = @code", conn))
            {
                cmdP.Parameters.AddWithValue("@code", v.Id);
                using var rP = cmdP.ExecuteReader();
                while (rP.Read())
                {
                    var pct = rP.GetDecimal(1);
                    promos.Add($"{rP.GetString(0)}: {pct:0.#}%");
                }
            }

            list.Add(new VoucherSummaryDto(v.Id, v.Status, v.Exp, v.Max, v.Used, promos, v.Orders));
        }

        return list;
    }

    public VoucherSummaryDto? FindVoucher(string code)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT v.MaVoucher, v.TrangThai, v.HanSuDung, v.SoLanToiDa, v.SoLanDaSuDung,
                   ISNULL((SELECT COUNT(1) FROM HD WHERE MaVoucher = v.MaVoucher), 0)
            FROM Voucher v
            WHERE v.MaVoucher = @code";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@code", code.ToUpperInvariant());
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        var v = (
            Id: r.GetString(0), Status: r.GetString(1),
            Exp: r.IsDBNull(2) ? null : (DateTime?)r.GetDateTime(2),
            Max: r.IsDBNull(3) ? null : (int?)r.GetInt32(3),
            Used: r.GetInt32(4), Orders: r.GetInt32(5));
        r.Close();

        var promos = new List<string>();
        using (var cmdP = new SqlCommand(@"
            SELECT c.TenCTKM, pv.PhanTramGiam
            FROM KMTheoVoucher pv
            JOIN CTKM c ON pv.MaCTKM = c.MaCTKM
            WHERE pv.MaVoucher = @code", conn))
        {
            cmdP.Parameters.AddWithValue("@code", v.Id);
            using var rP = cmdP.ExecuteReader();
            while (rP.Read())
            {
                var pct = rP.GetDecimal(1);
                promos.Add($"{rP.GetString(0)}: {pct:0.#}%");
            }
        }

        return new VoucherSummaryDto(v.Id, v.Status, v.Exp, v.Max, v.Used, promos, v.Orders);
    }

    public void UpdateVoucher(string code, string status, DateTime? expiresAt, int? maxUsageCount)
    {
        using var conn = DbConnection.Open();
        var sql = "UPDATE Voucher SET TrangThai = @s, HanSuDung = @e, SoLanToiDa = @m WHERE MaVoucher = @code";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@code", code.ToUpperInvariant());
        cmd.Parameters.AddWithValue("@s", status);
        cmd.Parameters.AddWithValue("@e", (object?)expiresAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@m", (object?)maxUsageCount ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void DeleteVoucher(string code)
    {
        using var conn = DbConnection.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand(@"
                DELETE FROM KMTheoVoucher WHERE MaVoucher = @code;
                DELETE FROM Voucher WHERE MaVoucher = @code", conn, tx))
            {
                cmd.Parameters.AddWithValue("@code", code.ToUpperInvariant());
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public bool VoucherHasBeenUsed(string code)
    {
        using var conn = DbConnection.Open();
        var sql = "SELECT COUNT(1) FROM HD WHERE MaVoucher = @code";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@code", code.ToUpperInvariant());
        return (int)(cmd.ExecuteScalar() ?? 0) > 0;
    }
}
