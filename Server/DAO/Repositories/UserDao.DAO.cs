using System.Data;
using Microsoft.Data.SqlClient;
using TrangSach.Shared.DTO;

namespace TrangSach.DAO;

public class UserDao
{
    // -----------------------------------------------------------------------
    // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
    // -----------------------------------------------------------------------
    public List<ManagerEmployeeDto> GetAllEmployees()
    {
        var list = new List<ManagerEmployeeDto>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT nv.MaNV, nv.HoLot, nv.Ten, nv.NS, nv.DiaChi, nv.TrangThaiTaiKhoan, nv.LuongCB,
                   tk.IDTaiKhoan, tk.TenDangNhap, tk.Email, ISNULL(tk.TrangThai, nv.TrangThaiTaiKhoan), vt.TenVaiTro, nv.AnhChanDung
            FROM NV nv
            LEFT JOIN TaiKhoan tk ON nv.IDTaiKhoan = tk.IDTaiKhoan
            LEFT JOIN VaiTro vt ON tk.IDVaiTro = vt.IDVaiTro
            ORDER BY nv.Ten";
        using var cmd = new SqlCommand(sql, conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
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

    public void InsertEmployee(string id, string lastName, string firstName, DateOnly? birthDate, string? avatarPath,
        string? address, string accountStatus, decimal baseSalary, int? accountId)
    {
        using var conn = DbConnection.Open();
        var sql = @"INSERT INTO NV (MaNV, HoLot, Ten, NS, AnhChanDung, DiaChi, TrangThaiTaiKhoan, LuongCB, IDTaiKhoan)
                    VALUES (@id, @ho, @ten, @ns, @avatar, @dc, @status, @luong, @accId)";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@ho", lastName.Trim());
        cmd.Parameters.AddWithValue("@ten", firstName.Trim());
        cmd.Parameters.AddWithValue("@ns", (object?)birthDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@avatar", (object?)avatarPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dc", (object?)address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", accountStatus);
        cmd.Parameters.AddWithValue("@luong", baseSalary);
        cmd.Parameters.AddWithValue("@accId", (object?)accountId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void UpdateEmployee(string id, string lastName, string firstName, DateOnly? birthDate, string? avatarPath,
        string? address, string accountStatus, decimal baseSalary, int? accountId)
    {
        using var conn = DbConnection.Open();
        var sql = @"UPDATE NV SET HoLot = @ho, Ten = @ten, NS = @ns, AnhChanDung = @avatar, DiaChi = @dc,
                           TrangThaiTaiKhoan = @status, LuongCB = @luong, IDTaiKhoan = @accId
                    WHERE MaNV = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@ho", lastName.Trim());
        cmd.Parameters.AddWithValue("@ten", firstName.Trim());
        cmd.Parameters.AddWithValue("@ns", (object?)birthDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@avatar", (object?)avatarPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dc", (object?)address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", accountStatus);
        cmd.Parameters.AddWithValue("@luong", baseSalary);
        cmd.Parameters.AddWithValue("@accId", (object?)accountId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void DeleteEmployee(string id, int? accountId)
    {
        using var conn = DbConnection.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand("DELETE FROM NV WHERE MaNV = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            if (accountId.HasValue)
            {
                using var cmdAcc = new SqlCommand("DELETE FROM TaiKhoan WHERE IDTaiKhoan = @accId", conn, tx);
                cmdAcc.Parameters.AddWithValue("@accId", accountId.Value);
                cmdAcc.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public bool EmployeeHasTransactions(string id)
    {
        using var conn = DbConnection.Open();
        var sql = "SELECT (SELECT COUNT(1) FROM HD WHERE MaNV = @id) + (SELECT COUNT(1) FROM PNH WHERE MaNV = @id)";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var total = (int)(cmd.ExecuteScalar() ?? 0);
        return total > 0;
    }

    public List<ManagerCustomerDto> GetAllCustomers()
    {
        var list = new List<ManagerCustomerDto>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT kh.MaKH, kh.HoTen, kh.SoDienThoai, kh.DiaChiGiaoHang,
                   tk.IDTaiKhoan, tk.TenDangNhap, tk.Email, ISNULL(tk.TrangThai, N'Active'),
                   kh.NgaySinh
            FROM KH kh
            LEFT JOIN TaiKhoan tk ON kh.IDTaiKhoan = tk.IDTaiKhoan
            ORDER BY kh.HoTen";
        using var cmd = new SqlCommand(sql, conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var birthDate = r.IsDBNull(8) ? (DateOnly?)null : DateOnly.FromDateTime(r.GetDateTime(8));
            list.Add(new ManagerCustomerDto(
                r.GetString(0), r.GetString(1), birthDate, r.IsDBNull(2) ? "" : r.GetString(2), r.IsDBNull(3) ? "" : r.GetString(3),
                r.IsDBNull(4) ? (int?)null : r.GetInt32(4), r.IsDBNull(5) ? "" : r.GetString(5), r.IsDBNull(6) ? "" : r.GetString(6), r.GetString(7)));
        }
        return list;
    }

    public void InsertCustomer(string id, string fullName, DateOnly? birthDate, string? phoneNumber, string? shippingAddress, int? accountId)
    {
        using var conn = DbConnection.Open();
        var sql = @"INSERT INTO KH (MaKH, HoTen, NgaySinh, SoDienThoai, DiaChiGiaoHang, IDTaiKhoan)
                    VALUES (@id, @name, @birth, @phone, @addr, @accId)";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", fullName.Trim());
        cmd.Parameters.AddWithValue("@birth", birthDate.HasValue ? birthDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        cmd.Parameters.AddWithValue("@phone", (object?)phoneNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@addr", (object?)shippingAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@accId", (object?)accountId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void InsertCustomer(string id, string fullName, string? phoneNumber, string? shippingAddress, int? accountId)
    {
        InsertCustomer(id, fullName, null, phoneNumber, shippingAddress, accountId);
    }

    public void UpdateCustomer(string id, string fullName, DateOnly? birthDate, string? phoneNumber, string? shippingAddress, int? accountId)
    {
        using var conn = DbConnection.Open();
        var sql = @"UPDATE KH SET HoTen = @name, NgaySinh = @birth, SoDienThoai = @phone, DiaChiGiaoHang = @addr, IDTaiKhoan = @accId
                    WHERE MaKH = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", fullName.Trim());
        cmd.Parameters.AddWithValue("@birth", birthDate.HasValue ? birthDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        cmd.Parameters.AddWithValue("@phone", (object?)phoneNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@addr", (object?)shippingAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@accId", (object?)accountId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void UpdateCustomer(string id, string fullName, string? phoneNumber, string? shippingAddress, int? accountId)
    {
        UpdateCustomer(id, fullName, null, phoneNumber, shippingAddress, accountId);
    }

    public void DeleteCustomer(string id, int? accountId)
    {
        using var conn = DbConnection.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var cmd = new SqlCommand("DELETE FROM KH WHERE MaKH = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            if (accountId.HasValue)
            {
                using var cmdAcc = new SqlCommand("DELETE FROM TaiKhoan WHERE IDTaiKhoan = @accId", conn, tx);
                cmdAcc.Parameters.AddWithValue("@accId", accountId.Value);
                cmdAcc.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public bool CustomerHasOrders(string id)
    {
        using var conn = DbConnection.Open();
        var sql = "SELECT COUNT(1) FROM HD WHERE MaKH = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var count = (int)(cmd.ExecuteScalar() ?? 0);
        return count > 0;
    }

    public AuthenticatedAccountDto? FindAccount(string usernameOrEmail)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT tk.IDTaiKhoan, tk.TenDangNhap, tk.Email, vt.TenVaiTro,
                   kh.MaKH, tk.MatKhauHash, tk.TrangThai
            FROM TaiKhoan tk
            JOIN VaiTro vt ON tk.IDVaiTro = vt.IDVaiTro
            LEFT JOIN KH kh ON tk.IDTaiKhoan = kh.IDTaiKhoan
            WHERE tk.TenDangNhap = @u OR tk.Email = @u";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", usernameOrEmail.Trim());
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return new AuthenticatedAccountDto(
            r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3),
            r.IsDBNull(4) ? null : r.GetString(4));
    }

    public (int Id, string UserName, string Email, string PasswordHash, string Status, string RoleName, string? CustomerId, string? EmployeeId, string? EmployeeStatus)?
        FindAccountWithDetails(string usernameOrEmail)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT tk.IDTaiKhoan, tk.TenDangNhap, tk.Email, tk.MatKhauHash, tk.TrangThai, vt.TenVaiTro,
                   kh.MaKH, nv.MaNV, nv.TrangThaiTaiKhoan
            FROM TaiKhoan tk
            JOIN VaiTro vt ON tk.IDVaiTro = vt.IDVaiTro
            LEFT JOIN KH kh ON tk.IDTaiKhoan = kh.IDTaiKhoan
            LEFT JOIN NV nv ON tk.IDTaiKhoan = nv.IDTaiKhoan
            WHERE tk.TenDangNhap = @u OR tk.Email = @u";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", usernameOrEmail.Trim());
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return (
            r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5),
            r.IsDBNull(6) ? null : r.GetString(6),
            r.IsDBNull(7) ? null : r.GetString(7),
            r.IsDBNull(8) ? null : r.GetString(8));
    }

        public (int Id, string UserName, string Email, string Status, string RoleName, string? EmployeeStatus)?
        FindAccountForTokenValidation(int accountId)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT tk.IDTaiKhoan, tk.TenDangNhap, tk.Email, tk.TrangThai, vt.TenVaiTro, nv.TrangThaiTaiKhoan
            FROM TaiKhoan tk
            JOIN VaiTro vt ON tk.IDVaiTro = vt.IDVaiTro
            LEFT JOIN NV nv ON tk.IDTaiKhoan = nv.IDTaiKhoan
            WHERE tk.IDTaiKhoan = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", accountId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return (
            r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
            r.IsDBNull(5) ? null : r.GetString(5));
    }

    public AccountProfileDto? FindCurrentUserProfile(int accountId)
    {
        using var conn = DbConnection.Open();
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
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", accountId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return new AccountProfileDto(
            r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
            r.IsDBNull(5) ? null : r.GetString(5),
            r.IsDBNull(6) ? null : r.GetString(6));
    }

    public void UpdateCustomerProfile(int accountId, string fullName, string? phoneNumber, string? address)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            IF EXISTS (SELECT 1 FROM KH WHERE IDTaiKhoan = @accId)
            BEGIN
                UPDATE KH
                SET HoTen = @name,
                    SoDienThoai = @phone,
                    DiaChiGiaoHang = @addr
                WHERE IDTaiKhoan = @accId;
            END";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@accId", accountId);
        cmd.Parameters.AddWithValue("@name", fullName.Trim());
        cmd.Parameters.AddWithValue("@phone", (object?)phoneNumber?.Trim() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@addr", (object?)address?.Trim() ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public bool ExistsUserNameOrEmail(string userName, string email, int? excludeId = null)
    {
        using var conn = DbConnection.Open();
        var sql = excludeId.HasValue
            ? "SELECT COUNT(1) FROM TaiKhoan WHERE (TenDangNhap = @u OR Email = @e) AND IDTaiKhoan <> @excludeId"
            : "SELECT COUNT(1) FROM TaiKhoan WHERE TenDangNhap = @u OR Email = @e";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", userName.Trim());
        cmd.Parameters.AddWithValue("@e", email.Trim());
        if (excludeId.HasValue) cmd.Parameters.AddWithValue("@excludeId", excludeId.Value);
        return (int)(cmd.ExecuteScalar() ?? 0) > 0;
    }

    public int? GetRoleIdByName(string roleName)
    {
        using var conn = DbConnection.Open();
        var sql = "SELECT IDVaiTro FROM VaiTro WHERE TenVaiTro = @role";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@role", roleName);
        var res = cmd.ExecuteScalar();
        return res != null ? (int)res : null;
    }

    public int CountAccountsByRole(string roleName)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT COUNT(1)
            FROM TaiKhoan tk
            JOIN VaiTro vt ON tk.IDVaiTro = vt.IDVaiTro
            WHERE vt.TenVaiTro = @role";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@role", roleName);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public int CreateAccount(string userName, string email, string passwordHash, int roleId, string status = "Active")
    {
        using var conn = DbConnection.Open();
        var sql = @"
            INSERT INTO TaiKhoan (TenDangNhap, Email, MatKhauHash, IDVaiTro, TrangThai)
            OUTPUT INSERTED.IDTaiKhoan
            VALUES (@u, @e, @p, @r, @s)";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@u", userName.Trim());
        cmd.Parameters.AddWithValue("@e", email.Trim());
        cmd.Parameters.AddWithValue("@p", passwordHash);
        cmd.Parameters.AddWithValue("@r", roleId);
        cmd.Parameters.AddWithValue("@s", status);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public void UpdateAccount(int accountId, string userName, string email, string? passwordHash, string? status, int? roleId)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            UPDATE TaiKhoan
            SET TenDangNhap = @u,
                Email = @e,
                MatKhauHash = COALESCE(@p, MatKhauHash),
                TrangThai = COALESCE(@s, TrangThai),
                IDVaiTro = COALESCE(@r, IDVaiTro)
            WHERE IDTaiKhoan = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", accountId);
        cmd.Parameters.AddWithValue("@u", userName.Trim());
        cmd.Parameters.AddWithValue("@e", email.Trim());
        cmd.Parameters.AddWithValue("@p", (object?)passwordHash ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@s", (object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@r", (object?)roleId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<StaffRoleOptionDto> GetAllRoles()
    {
        var list = new List<StaffRoleOptionDto>();
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT TenVaiTro FROM VaiTro", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new StaffRoleOptionDto(r.GetString(0), r.GetString(0)));
        return list;
    }

    public List<ManagerAuditLogDto> GetAuditLogs(int take = 500)
    {
        var list = new List<ManagerAuditLogDto>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT TOP (@take) Id, OccurredAt, ActorName, ActorCode, ActorRole, Action, Area, Resource, StatusCode, IpAddress
            FROM SystemAuditLog
            ORDER BY OccurredAt DESC";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@take", take);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ManagerAuditLogDto(
                r.GetInt64(0), r.GetDateTime(1), r.IsDBNull(2) ? "" : r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3),
                r.IsDBNull(4) ? "Unknown" : r.GetString(4), r.GetString(5), r.IsDBNull(6) ? "" : r.GetString(6),
                r.IsDBNull(7) ? "" : r.GetString(7), r.IsDBNull(8) ? 0 : r.GetInt32(8), r.IsDBNull(9) ? null : r.GetString(9)));
        }
        return list;
    }

    public void InsertAuditLog(AuditLogCommandDto log)
    {
        using var conn = DbConnection.Open();
        var sql = @"INSERT INTO SystemAuditLog (OccurredAt, ActorAccountId, ActorName, ActorCode, ActorRole, Action, Area, Resource, StatusCode, IpAddress)
                    VALUES (GETUTCDATE(), @accId, @actorName, @actorCode, @actorRole, @action, @area, @resource, @status, @ip)";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@accId", (object?)log.AccountId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@actorName", (object?)log.LoginIdentifier ?? "System");
        cmd.Parameters.AddWithValue("@actorCode", DBNull.Value);
        cmd.Parameters.AddWithValue("@actorRole", (object?)log.TokenRole ?? "Unknown");
        cmd.Parameters.AddWithValue("@action", log.Action);
        cmd.Parameters.AddWithValue("@area", (object?)log.Area ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@resource", (object?)log.Resource ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", (object?)log.StatusCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ip", (object?)log.IpAddress ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<(string BookId, string Title, string Author, decimal SalePrice, int AvailableStock, int Quantity, string? CoverImagePath)>
        GetCustomerCart(string customerId)
    {
        var list = new List<(string, string, string, decimal, int, int, string?)>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT gh.MaSach, s.TenSach,
                   ISNULL((SELECT TOP 1 tg.TenTacGia FROM TacGiaSach tgs JOIN TacGia tg ON tgs.IDTacGia = tg.IDTacGia WHERE tgs.MaSach = s.MaSach ORDER BY tg.TenTacGia), N'Chưa cập nhật'),
                   s.DGBan, s.SLCo, gh.SoLuong, s.CoverImagePath
            FROM GioHang gh
            JOIN Sach s ON gh.MaSach = s.MaSach
            WHERE gh.MaKH = @cusId";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@cusId", customerId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var cover = r.IsDBNull(6) ? null : r.GetString(6);
            list.Add((r.GetString(0), r.GetString(1), r.GetString(2), r.GetDecimal(3), r.GetInt32(4), r.GetInt32(5), cover));
        }
        return list;
    }

    public void SetCartItem(string customerId, string bookId, int quantity)
    {
        using var conn = DbConnection.Open();
        var sql = @"
            IF EXISTS (SELECT 1 FROM GioHang WHERE MaKH = @c AND MaSach = @b)
                UPDATE GioHang SET SoLuong = @q WHERE MaKH = @c AND MaSach = @b
            ELSE
                INSERT INTO GioHang (MaKH, MaSach, SoLuong) VALUES (@c, @b, @q)";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@c", customerId);
        cmd.Parameters.AddWithValue("@b", bookId);
        cmd.Parameters.AddWithValue("@q", quantity);
        cmd.ExecuteNonQuery();
    }

    public void RemoveCartItem(string customerId, string bookId)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("DELETE FROM GioHang WHERE MaKH = @c AND MaSach = @b", conn);
        cmd.Parameters.AddWithValue("@c", customerId);
        cmd.Parameters.AddWithValue("@b", bookId);
        cmd.ExecuteNonQuery();
    }

    public void ClearCart(string customerId)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("DELETE FROM GioHang WHERE MaKH = @c", conn);
        cmd.Parameters.AddWithValue("@c", customerId);
        cmd.ExecuteNonQuery();
    }

    // -----------------------------------------------------------------------
    // Nhân viên (Employee)
    // -----------------------------------------------------------------------
}
