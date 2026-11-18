using System.Data;
using Microsoft.Data.SqlClient;
using TrangSach.Shared.DTO;

namespace TrangSach.DAO;

public class UserDao
{
    // -----------------------------------------------------------------------
    // Nhân viên (Employee)
    // -----------------------------------------------------------------------
    public async Task<List<ManagerEmployeeDto>> GetAllEmployeesAsync(CancellationToken ct = default)
    {
        var list = new List<ManagerEmployeeDto>();
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT nv.MaNV, nv.HoLot, nv.Ten, nv.NS, nv.DiaChi, nv.TrangThaiTaiKhoan, nv.LuongCB,
                   tk.IDTaiKhoan, tk.TenDangNhap, tk.Email, ISNULL(tk.TrangThai, nv.TrangThaiTaiKhoan), vt.TenVaiTro, nv.AnhChanDung
            FROM NV nv
            LEFT JOIN TaiKhoan tk ON nv.IDTaiKhoan = tk.IDTaiKhoan
            LEFT JOIN VaiTro vt ON tk.IDVaiTro = vt.IDVaiTro
            ORDER BY nv.Ten";
        await using var cmd = new SqlCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var id = r.GetString(0);
            var ho = r.GetString(1);
            var ten = r.GetString(2);
            var ns = r.IsDBNull(3) ? (DateOnly?)null : DateOnly.FromDateTime(r.GetDateTime(3));
            var addr = r.IsDBNull(4) ? "" : r.GetString(4);
            var status = r.GetString(5);
            var salary = r.GetDecimal(6);
            var accId = r.IsDBNull(7) ? (int?)null : r.GetInt32(7);
            var uName = r.IsDBNull(8) ? "" : r.GetString(8);
            var email = r.IsDBNull(9) ? "" : r.GetString(9);
            var accStatus = r.IsDBNull(10) ? "" : r.GetString(10);
            var role = r.IsDBNull(11) ? "" : r.GetString(11);
            var avatar = r.IsDBNull(12) ? null : r.GetString(12);

            list.Add(new ManagerEmployeeDto(id, ho, ten, ns, avatar, addr, status, salary, accId, uName, email, accStatus, role));
        }
        return list;
    }

    public async Task InsertEmployeeAsync(string id, string lastName, string firstName, DateOnly? birthDate, string? avatarPath,
        string? address, string accountStatus, decimal baseSalary, int? accountId, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"INSERT INTO NV (MaNV, HoLot, Ten, NS, AnhChanDung, DiaChi, TrangThaiTaiKhoan, LuongCB, IDTaiKhoan)
                    VALUES (@id, @ho, @ten, @ns, @avatar, @dc, @status, @luong, @accId)";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@ho", lastName.Trim());
        cmd.Parameters.AddWithValue("@ten", firstName.Trim());
        cmd.Parameters.AddWithValue("@ns", (object?)birthDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@avatar", (object?)avatarPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dc", (object?)address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", accountStatus);
        cmd.Parameters.AddWithValue("@luong", baseSalary);
        cmd.Parameters.AddWithValue("@accId", (object?)accountId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateEmployeeAsync(string id, string lastName, string firstName, DateOnly? birthDate, string? avatarPath,
        string? address, string accountStatus, decimal baseSalary, int? accountId, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"UPDATE NV SET HoLot = @ho, Ten = @ten, NS = @ns, AnhChanDung = @avatar, DiaChi = @dc,
                           TrangThaiTaiKhoan = @status, LuongCB = @luong, IDTaiKhoan = @accId
                    WHERE MaNV = @id";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@ho", lastName.Trim());
        cmd.Parameters.AddWithValue("@ten", firstName.Trim());
        cmd.Parameters.AddWithValue("@ns", (object?)birthDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@avatar", (object?)avatarPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dc", (object?)address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", accountStatus);
        cmd.Parameters.AddWithValue("@luong", baseSalary);
        cmd.Parameters.AddWithValue("@accId", (object?)accountId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteEmployeeAsync(string id, int? accountId, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var tx = conn.BeginTransaction();
        try
        {
            await using (var cmd = new SqlCommand("DELETE FROM NV WHERE MaNV = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            if (accountId.HasValue)
            {
                await using var cmdAcc = new SqlCommand("DELETE FROM TaiKhoan WHERE IDTaiKhoan = @accId", conn, tx);
                cmdAcc.Parameters.AddWithValue("@accId", accountId.Value);
                await cmdAcc.ExecuteNonQueryAsync(ct);
            }
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> EmployeeHasTransactionsAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = "SELECT (SELECT COUNT(1) FROM HD WHERE MaNV = @id) + (SELECT COUNT(1) FROM PNH WHERE MaNV = @id)";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var total = (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
        return total > 0;
    }

    // -----------------------------------------------------------------------
    // Khách hàng (Customer)
    // -----------------------------------------------------------------------
    public async Task<List<ManagerCustomerDto>> GetAllCustomersAsync(CancellationToken ct = default)
    {
        var list = new List<ManagerCustomerDto>();
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT kh.MaKH, kh.HoTen, kh.SoDienThoai, kh.DiaChiGiaoHang,
                   tk.IDTaiKhoan, tk.TenDangNhap, tk.Email, ISNULL(tk.TrangThai, N'Active')
            FROM KH kh
            LEFT JOIN TaiKhoan tk ON kh.IDTaiKhoan = tk.IDTaiKhoan
            ORDER BY kh.HoTen";
        await using var cmd = new SqlCommand(sql, conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new ManagerCustomerDto(
                r.GetString(0), r.GetString(1), r.IsDBNull(2) ? "" : r.GetString(2), r.IsDBNull(3) ? "" : r.GetString(3),
                r.IsDBNull(4) ? (int?)null : r.GetInt32(4), r.IsDBNull(5) ? "" : r.GetString(5), r.IsDBNull(6) ? "" : r.GetString(6), r.GetString(7)));
        }
        return list;
    }

    public async Task InsertCustomerAsync(string id, string fullName, string? phoneNumber, string? shippingAddress, int? accountId, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"INSERT INTO KH (MaKH, HoTen, SoDienThoai, DiaChiGiaoHang, IDTaiKhoan)
                    VALUES (@id, @name, @phone, @addr, @accId)";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", fullName.Trim());
        cmd.Parameters.AddWithValue("@phone", (object?)phoneNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@addr", (object?)shippingAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@accId", (object?)accountId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateCustomerAsync(string id, string fullName, string? phoneNumber, string? shippingAddress, int? accountId, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"UPDATE KH SET HoTen = @name, SoDienThoai = @phone, DiaChiGiaoHang = @addr, IDTaiKhoan = @accId
                    WHERE MaKH = @id";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", fullName.Trim());
        cmd.Parameters.AddWithValue("@phone", (object?)phoneNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@addr", (object?)shippingAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@accId", (object?)accountId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteCustomerAsync(string id, int? accountId, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var tx = conn.BeginTransaction();
        try
        {
            await using (var cmd = new SqlCommand("DELETE FROM KH WHERE MaKH = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            if (accountId.HasValue)
            {
                await using var cmdAcc = new SqlCommand("DELETE FROM TaiKhoan WHERE IDTaiKhoan = @accId", conn, tx);
                cmdAcc.Parameters.AddWithValue("@accId", accountId.Value);
                await cmdAcc.ExecuteNonQueryAsync(ct);
            }
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> CustomerHasOrdersAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = "SELECT COUNT(1) FROM HD WHERE MaKH = @id";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var count = (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
        return count > 0;
    }

    // -----------------------------------------------------------------------
    // Tài khoản & Xác thực (Accounts & Auth)
    // -----------------------------------------------------------------------
    public async Task<AuthenticatedAccountDto?> FindAccountAsync(string usernameOrEmail, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT tk.IDTaiKhoan, tk.TenDangNhap, tk.Email, vt.TenVaiTro,
                   kh.MaKH, tk.MatKhauHash, tk.TrangThai
            FROM TaiKhoan tk
            JOIN VaiTro vt ON tk.IDVaiTro = vt.IDVaiTro
            LEFT JOIN KH kh ON tk.IDTaiKhoan = kh.IDTaiKhoan
            WHERE tk.TenDangNhap = @u OR tk.Email = @u";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", usernameOrEmail.Trim());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new AuthenticatedAccountDto(
            r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3),
            r.IsDBNull(4) ? null : r.GetString(4));
    }

    public async Task<(int Id, string UserName, string Email, string PasswordHash, string Status, string RoleName, string? CustomerId, string? EmployeeId, string? EmployeeStatus)?>
        FindAccountWithDetailsAsync(string usernameOrEmail, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT tk.IDTaiKhoan, tk.TenDangNhap, tk.Email, tk.MatKhauHash, tk.TrangThai, vt.TenVaiTro,
                   kh.MaKH, nv.MaNV, nv.TrangThaiTaiKhoan
            FROM TaiKhoan tk
            JOIN VaiTro vt ON tk.IDVaiTro = vt.IDVaiTro
            LEFT JOIN KH kh ON tk.IDTaiKhoan = kh.IDTaiKhoan
            LEFT JOIN NV nv ON tk.IDTaiKhoan = nv.IDTaiKhoan
            WHERE tk.TenDangNhap = @u OR tk.Email = @u";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", usernameOrEmail.Trim());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return (
            r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5),
            r.IsDBNull(6) ? null : r.GetString(6),
            r.IsDBNull(7) ? null : r.GetString(7),
            r.IsDBNull(8) ? null : r.GetString(8));
    }

    public async Task<(int Id, string UserName, string Email, string Status, string RoleName, string? EmployeeStatus)?>
        FindAccountForTokenValidationAsync(int accountId, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT tk.IDTaiKhoan, tk.TenDangNhap, tk.Email, tk.TrangThai, vt.TenVaiTro, nv.TrangThaiTaiKhoan
            FROM TaiKhoan tk
            JOIN VaiTro vt ON tk.IDVaiTro = vt.IDVaiTro
            LEFT JOIN NV nv ON tk.IDTaiKhoan = nv.IDTaiKhoan
            WHERE tk.IDTaiKhoan = @id";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", accountId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return (
            r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
            r.IsDBNull(5) ? null : r.GetString(5));
    }

    public async Task<AccountProfileDto?> FindCurrentUserProfileAsync(int accountId, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT tk.TenDangNhap, tk.Email, vt.TenVaiTro,
                   ISNULL(kh.MaKH, ISNULL(nv.MaNV, CAST(tk.IDTaiKhoan AS NVARCHAR(20)))),
                   ISNULL(kh.HoTen, ISNULL(nv.HoLot + ' ' + nv.Ten, tk.TenDangNhap)),
                   kh.SoDienThoai,
                   ISNULL(kh.DiaChiGiaoHang, nv.DiaChi)
            FROM TaiKhoan tk
            JOIN VaiTro vt ON tk.IDVaiTro = vt.IDVaiTro
            LEFT JOIN KH kh ON tk.IDTaiKhoan = kh.IDTaiKhoan
            LEFT JOIN NV nv ON tk.IDTaiKhoan = nv.IDTaiKhoan
            WHERE tk.IDTaiKhoan = @id";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", accountId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return new AccountProfileDto(
            r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
            r.IsDBNull(5) ? null : r.GetString(5),
            r.IsDBNull(6) ? null : r.GetString(6));
    }

    public async Task<bool> ExistsUserNameOrEmailAsync(string userName, string email, int? excludeId = null, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM TaiKhoan WHERE (TenDangNhap = @u OR Email = @e) AND IDTaiKhoan <> @excludeId"
            : "SELECT COUNT(1) FROM TaiKhoan WHERE TenDangNhap = @u OR Email = @e";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", userName.Trim());
        cmd.Parameters.AddWithValue("@e", email.Trim());
        if (excludeId.HasValue) cmd.Parameters.AddWithValue("@excludeId", excludeId.Value);
        return (int)(await cmd.ExecuteScalarAsync(ct) ?? 0) > 0;
    }

    public async Task<int?> GetRoleIdByNameAsync(string roleName, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = "SELECT IDVaiTro FROM VaiTro WHERE TenVaiTro = @role";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@role", roleName);
        var res = await cmd.ExecuteScalarAsync(ct);
        return res != null ? (int)res : null;
    }

    public async Task<int> CountAccountsByRoleAsync(string roleName, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT COUNT(1)
            FROM TaiKhoan tk
            JOIN VaiTro vt ON tk.IDVaiTro = vt.IDVaiTro
            WHERE vt.TenVaiTro = @role";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@role", roleName);
        return (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
    }

    public async Task<int> CreateAccountAsync(string userName, string email, string passwordHash, int roleId, string status = "Active", CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            INSERT INTO TaiKhoan (TenDangNhap, Email, MatKhauHash, IDVaiTro, TrangThai)
            OUTPUT INSERTED.IDTaiKhoan
            VALUES (@u, @e, @p, @r, @s)";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", userName.Trim());
        cmd.Parameters.AddWithValue("@e", email.Trim());
        cmd.Parameters.AddWithValue("@p", passwordHash);
        cmd.Parameters.AddWithValue("@r", roleId);
        cmd.Parameters.AddWithValue("@s", status);
        return (int)(await cmd.ExecuteScalarAsync(ct) ?? 0);
    }

    public async Task UpdateAccountAsync(int accountId, string userName, string email, string? passwordHash, string? status, int? roleId, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            UPDATE TaiKhoan
            SET TenDangNhap = @u,
                Email = @e,
                MatKhauHash = COALESCE(@p, MatKhauHash),
                TrangThai = COALESCE(@s, TrangThai),
                IDVaiTro = COALESCE(@r, IDVaiTro)
            WHERE IDTaiKhoan = @id";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", accountId);
        cmd.Parameters.AddWithValue("@u", userName.Trim());
        cmd.Parameters.AddWithValue("@e", email.Trim());
        cmd.Parameters.AddWithValue("@p", (object?)passwordHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@s", (object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@r", (object?)roleId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<StaffRoleOptionDto>> GetAllRolesAsync(CancellationToken ct = default)
    {
        var list = new List<StaffRoleOptionDto>();
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand("SELECT TenVaiTro FROM VaiTro", conn);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var name = r.GetString(0);
            list.Add(new StaffRoleOptionDto(name, name));
        }
        return list;
    }

    // -----------------------------------------------------------------------
    // Audit Logs
    // -----------------------------------------------------------------------
    public async Task<List<ManagerAuditLogDto>> GetAuditLogsAsync(int take = 500, CancellationToken ct = default)
    {
        var list = new List<ManagerAuditLogDto>();
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT TOP (@take) Id, OccurredAt, ActorName, ActorCode, ActorRole, Action, Area, Resource, StatusCode, IpAddress
            FROM SystemAuditLog
            ORDER BY OccurredAt DESC";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@take", take);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new ManagerAuditLogDto(
                r.GetInt64(0), r.GetDateTime(1), r.IsDBNull(2) ? "" : r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3),
                r.IsDBNull(4) ? null : r.GetString(4), r.GetString(5), r.IsDBNull(6) ? null : r.GetString(6),
                r.IsDBNull(7) ? null : r.GetString(7), r.IsDBNull(8) ? 0 : r.GetInt32(8), r.IsDBNull(9) ? null : r.GetString(9)));
        }
        return list;
    }

    public async Task InsertAuditLogAsync(AuditLogCommandDto log, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"INSERT INTO SystemAuditLog (OccurredAt, ActorAccountId, ActorName, ActorCode, ActorRole, Action, Area, Resource, StatusCode, IpAddress)
                    VALUES (GETUTCDATE(), @accId, @actorName, @actorCode, @actorRole, @action, @area, @resource, @status, @ip)";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@accId", (object?)log.AccountId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@actorName", (object?)log.LoginIdentifier ?? "System");
        cmd.Parameters.AddWithValue("@actorCode", DBNull.Value);
        cmd.Parameters.AddWithValue("@actorRole", (object?)log.TokenRole ?? "Unknown");
        cmd.Parameters.AddWithValue("@action", log.Action);
        cmd.Parameters.AddWithValue("@area", (object?)log.Area ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@resource", (object?)log.Resource ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", (object?)log.StatusCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ip", (object?)log.IpAddress ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<(string? EmployeeCode, string? EmployeeName, string? CustomerCode, string? CustomerName, string RoleName, string UserName)?>
        GetAuditDetailsAsync(int? accountId, string? loginIdentifier, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT nv.MaNV, nv.HoLot + ' ' + nv.Ten, kh.MaKH, kh.HoTen, vt.TenVaiTro, tk.TenDangNhap
            FROM TaiKhoan tk
            JOIN VaiTro vt ON tk.IDVaiTro = vt.IDVaiTro
            LEFT JOIN NV nv ON tk.IDTaiKhoan = nv.IDTaiKhoan
            LEFT JOIN KH kh ON tk.IDTaiKhoan = kh.IDTaiKhoan
            WHERE (@accId IS NOT NULL AND tk.IDTaiKhoan = @accId)
               OR (@u IS NOT NULL AND (tk.TenDangNhap = @u OR tk.Email = @u))";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@accId", (object?)accountId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@u", (object?)loginIdentifier ?? DBNull.Value);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;

        return (
            r.IsDBNull(0) ? null : r.GetString(0),
            r.IsDBNull(1) ? null : r.GetString(1),
            r.IsDBNull(2) ? null : r.GetString(2),
            r.IsDBNull(3) ? null : r.GetString(3),
            r.GetString(4),
            r.GetString(5));
    }

    // -----------------------------------------------------------------------
    // Giỏ hàng (Cart)
    // -----------------------------------------------------------------------
    public async Task<List<(string BookId, string Title, string Author, decimal SalePrice, int AvailableStock, int Quantity)>>
        GetCustomerCartAsync(string customerId, CancellationToken ct = default)
    {
        var list = new List<(string, string, string, decimal, int, int)>();
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            SELECT gh.MaSach, s.TenSach,
                   ISNULL((SELECT TOP 1 tg.TenTacGia FROM TacGiaSach tgs JOIN TacGia tg ON tgs.IDTacGia = tg.IDTacGia WHERE tgs.MaSach = s.MaSach ORDER BY tg.TenTacGia), N'Chưa cập nhật'),
                   s.DGBan, s.SLCo, gh.SoLuong
            FROM GioHang gh
            JOIN Sach s ON gh.MaSach = s.MaSach
            WHERE gh.MaKH = @cusId";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@cusId", customerId);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add((r.GetString(0), r.GetString(1), r.GetString(2), r.GetDecimal(3), r.GetInt32(4), r.GetInt32(5)));
        }
        return list;
    }

    public async Task SetCartItemAsync(string customerId, string bookId, int quantity, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        var sql = @"
            IF EXISTS (SELECT 1 FROM GioHang WHERE MaKH = @c AND MaSach = @b)
                UPDATE GioHang SET SoLuong = @q WHERE MaKH = @c AND MaSach = @b
            ELSE
                INSERT INTO GioHang (MaKH, MaSach, SoLuong) VALUES (@c, @b, @q)";
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@c", customerId);
        cmd.Parameters.AddWithValue("@b", bookId);
        cmd.Parameters.AddWithValue("@q", quantity);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RemoveCartItemAsync(string customerId, string bookId, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand("DELETE FROM GioHang WHERE MaKH = @c AND MaSach = @b", conn);
        cmd.Parameters.AddWithValue("@c", customerId);
        cmd.Parameters.AddWithValue("@b", bookId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task ClearCartAsync(string customerId, CancellationToken ct = default)
    {
        await using var conn = await DbConnection.OpenAsync(ct);
        await using var cmd = new SqlCommand("DELETE FROM GioHang WHERE MaKH = @c", conn);
        cmd.Parameters.AddWithValue("@c", customerId);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
