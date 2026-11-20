using System.Data;
using Microsoft.Data.SqlClient;
using TrangSach.Shared.DTO;

namespace TrangSach.DAO;

public class CatalogDao
{
    // -----------------------------------------------------------------------
    // Synchronous Methods (Phong cách thiết kế 3 lớp truyền thống)
    // -----------------------------------------------------------------------
    public List<CategoryOptionDto> GetAllCategories()
    {
        var list = new List<CategoryOptionDto>();
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT MaLoai, TenLoai FROM LoaiSach ORDER BY TenLoai", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new CategoryOptionDto(r.GetString(0), r.GetString(1)));
        return list;
    }

    public List<IdNameOptionDto> GetAllAuthors()
    {
        var list = new List<IdNameOptionDto>();
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT IDTacGia, TenTacGia FROM TacGia ORDER BY TenTacGia", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new IdNameOptionDto(r.GetInt32(0), r.GetString(1)));
        return list;
    }

    public List<IdNameOptionDto> GetAllPublishers()
    {
        var list = new List<IdNameOptionDto>();
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT IDNhaXuatBan, TenNhaXuatBan FROM NhaXuatBan ORDER BY TenNhaXuatBan", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new IdNameOptionDto(r.GetInt32(0), r.GetString(1)));
        return list;
    }

    public List<BookItemDto> GetAllBooks()
    {
        var list = new List<BookItemDto>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT s.MaSach, s.TenSach,
                   ISNULL(
                       (SELECT STRING_AGG(CAST(tg.TenTacGia AS NVARCHAR(MAX)), N', ')
                        FROM TacGiaSach tgs
                        JOIN TacGia tg ON tgs.IDTacGia = tg.IDTacGia
                        WHERE tgs.MaSach = s.MaSach),
                       N'Chưa cập nhật'
                   ) AS TacGia,
                   ISNULL(ls.TenLoai, N'Chưa phân loại') AS TheLoai,
                   ISNULL(s.DGBan, 0) AS DGBan,
                   ISNULL(s.TrangThai, 1) AS TrangThai,
                   ISNULL(s.MoTa, N'') AS MoTa,
                   ISNULL(s.SLCo, 0) AS SLCo,
                   ISNULL(nxb.TenNhaXuatBan, N'Chưa cập nhật') AS NXB,
                   ISNULL(s.CoverImagePath, N'') AS CoverImagePath,
                   ISNULL(s.ISBN, N'') AS ISBN,
                   ISNULL(s.DVTinh, N'Cuốn') AS DVTinh
            FROM Sach s
            LEFT JOIN LoaiSach ls ON s.MaLoai = ls.MaLoai
            LEFT JOIN NhaXuatBan nxb ON s.IDNhaXuatBan = nxb.IDNhaXuatBan
            ORDER BY s.TenSach";
        using var cmd = new SqlCommand(sql, conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var id = r.IsDBNull(0) ? "" : r.GetString(0);
            var title = r.IsDBNull(1) ? "" : r.GetString(1);
            var author = r.IsDBNull(2) ? "Chưa cập nhật" : r.GetString(2);
            var category = r.IsDBNull(3) ? "Chưa phân loại" : r.GetString(3);
            var price = r.IsDBNull(4) ? 0m : r.GetDecimal(4);
            var isActive = !r.IsDBNull(5) && r.GetBoolean(5);
            var desc = r.IsDBNull(6) ? "" : r.GetString(6);
            var stock = r.IsDBNull(7) ? 0 : r.GetInt32(7);
            var pub = r.IsDBNull(8) ? "Chưa cập nhật" : r.GetString(8);
            var cover = r.IsDBNull(9) ? "" : r.GetString(9);
            var isbn = r.IsDBNull(10) ? "" : r.GetString(10);
            var unit = r.IsDBNull(11) ? "Cuốn" : r.GetString(11);

            list.Add(new BookItemDto(
                id, title, author, category,
                price, unit,
                isActive ? "ĐANG BÁN" : "ĐÃ ẨN",
                desc,
                stock,
                pub,
                cover,
                isbn));
        }
        return list;
    }

    public BookEditDetailsDto? GetBookForEdit(string id)
    {
        using var conn = DbConnection.Open();
        var sql = "SELECT MaSach, TenSach, DGBan, DVTinh, MaLoai, IDNhaXuatBan, TrangThai, MoTa, CoverImagePath, ISBN, SLCo FROM Sach WHERE MaSach = @id";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        var bookId = r.IsDBNull(0) ? "" : r.GetString(0);
        var title = r.IsDBNull(1) ? "" : r.GetString(1);
        var price = r.IsDBNull(2) ? 0m : r.GetDecimal(2);
        var unit = r.IsDBNull(3) ? "Cuốn" : r.GetString(3);
        var categoryId = r.IsDBNull(4) ? "" : r.GetString(4);
        var publisherId = r.IsDBNull(5) ? (int?)null : r.GetInt32(5);
        var isActive = !r.IsDBNull(6) && r.GetBoolean(6);
        var desc = r.IsDBNull(7) ? "" : r.GetString(7);
        var coverImagePath = r.IsDBNull(8) ? "" : r.GetString(8);
        var isbn = r.IsDBNull(9) ? "" : r.GetString(9);
        var stock = r.IsDBNull(10) ? 0 : r.GetInt32(10);
        r.Close();

        var authorIds = new List<int>();
        using var cmdAuthors = new SqlCommand("SELECT IDTacGia FROM TacGiaSach WHERE MaSach = @id", conn);
        cmdAuthors.Parameters.AddWithValue("@id", id);
        using var rAuthors = cmdAuthors.ExecuteReader();
        while (rAuthors.Read()) authorIds.Add(rAuthors.GetInt32(0));

        return new BookEditDetailsDto(bookId, title, price, unit, categoryId, publisherId, authorIds, isActive, desc, coverImagePath, isbn, stock);
    }

    public void InsertBook(string id, BookManagementCommandDto cmdDto)
    {
        using var conn = DbConnection.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var sql = @"INSERT INTO Sach (MaSach, TenSach, MoTa, ISBN, SLCo, DGBan, TrangThai, DVTinh, MaLoai, IDNhaXuatBan, CoverImagePath)
                        VALUES (@id, @title, @desc, @isbn, 0, @price, @status, @unit, @catId, @pubId, @cover)";
            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@title", cmdDto.Title.Trim());
                cmd.Parameters.AddWithValue("@desc", (object?)cmdDto.Description?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@isbn", (object?)cmdDto.Isbn?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@price", cmdDto.SalePrice);
                cmd.Parameters.AddWithValue("@status", cmdDto.IsActive);
                cmd.Parameters.AddWithValue("@unit", string.IsNullOrWhiteSpace(cmdDto.Unit) ? "Cuốn" : cmdDto.Unit.Trim());
                cmd.Parameters.AddWithValue("@catId", cmdDto.CategoryId);
                cmd.Parameters.AddWithValue("@pubId", (object?)cmdDto.PublisherId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cover", (object?)cmdDto.CoverImagePath?.Trim() ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            foreach (var authorId in cmdDto.AuthorIds)
            {
                using var cmdAuthor = new SqlCommand("INSERT INTO TacGiaSach (MaSach, IDTacGia) VALUES (@bookId, @authorId)", conn, tx);
                cmdAuthor.Parameters.AddWithValue("@bookId", id);
                cmdAuthor.Parameters.AddWithValue("@authorId", authorId);
                cmdAuthor.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public void UpdateBook(string id, BookManagementCommandDto cmdDto)
    {
        using var conn = DbConnection.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var sql = @"UPDATE Sach SET TenSach = @title, MoTa = @desc, DGBan = @price, DVTinh = @unit,
                                       MaLoai = @catId, IDNhaXuatBan = @pubId, ISBN = @isbn, CoverImagePath = @cover, TrangThai = @status
                        WHERE MaSach = @id";
            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@title", cmdDto.Title.Trim());
                cmd.Parameters.AddWithValue("@desc", (object?)cmdDto.Description?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@isbn", (object?)cmdDto.Isbn?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@price", cmdDto.SalePrice);
                cmd.Parameters.AddWithValue("@unit", string.IsNullOrWhiteSpace(cmdDto.Unit) ? "Cuốn" : cmdDto.Unit.Trim());
                cmd.Parameters.AddWithValue("@catId", cmdDto.CategoryId);
                cmd.Parameters.AddWithValue("@pubId", (object?)cmdDto.PublisherId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cover", (object?)cmdDto.CoverImagePath?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", cmdDto.IsActive);
                cmd.ExecuteNonQuery();
            }

            using (var cmdDel = new SqlCommand("DELETE FROM TacGiaSach WHERE MaSach = @id", conn, tx))
            {
                cmdDel.Parameters.AddWithValue("@id", id);
                cmdDel.ExecuteNonQuery();
            }

            foreach (var authorId in cmdDto.AuthorIds)
            {
                using var cmdAuthor = new SqlCommand("INSERT INTO TacGiaSach (MaSach, IDTacGia) VALUES (@bookId, @authorId)", conn, tx);
                cmdAuthor.Parameters.AddWithValue("@bookId", id);
                cmdAuthor.Parameters.AddWithValue("@authorId", authorId);
                cmdAuthor.ExecuteNonQuery();
            }

            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }

    public void SetBookVisibility(string id, bool isActive)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("UPDATE Sach SET TrangThai = @active WHERE MaSach = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@active", isActive);
        cmd.ExecuteNonQuery();
    }

    public List<ManagerCategoryDto> GetAllCategoryDetails()
    {
        var list = new List<ManagerCategoryDto>();
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT MaLoai, TenLoai FROM LoaiSach ORDER BY TenLoai", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new ManagerCategoryDto(r.GetString(0), r.GetString(1)));
        return list;
    }

    public void InsertCategory(string id, string name)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("INSERT INTO LoaiSach (MaLoai, TenLoai) VALUES (@id, @name)", conn);
        cmd.Parameters.AddWithValue("@id", id.Trim());
        cmd.Parameters.AddWithValue("@name", name.Trim());
        cmd.ExecuteNonQuery();
    }

    public void UpdateCategory(string id, string name)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("UPDATE LoaiSach SET TenLoai = @name WHERE MaLoai = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", name.Trim());
        cmd.ExecuteNonQuery();
    }

    public void DeleteCategory(string id)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("DELETE FROM LoaiSach WHERE MaLoai = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public bool CategoryExists(string id)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM LoaiSach WHERE MaLoai = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return (int)(cmd.ExecuteScalar() ?? 0) > 0;
    }

    public bool CategoryHasBooks(string id)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM Sach WHERE MaLoai = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return (int)(cmd.ExecuteScalar() ?? 0) > 0;
    }

    public List<ManagerAuthorDto> GetAllAuthorDetails()
    {
        var list = new List<ManagerAuthorDto>();
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT IDTacGia, TenTacGia, TieuSu FROM TacGia ORDER BY TenTacGia", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new ManagerAuthorDto(r.GetInt32(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2)));
        return list;
    }

    public List<ManagerAuthorDto> GetAuthorsByBookId(string bookId)
    {
        var list = new List<ManagerAuthorDto>();
        using var conn = DbConnection.Open();
        var sql = @"
            SELECT tg.IDTacGia, tg.TenTacGia, tg.TieuSu
            FROM TacGiaSach tgs
            JOIN TacGia tg ON tgs.IDTacGia = tg.IDTacGia
            WHERE tgs.MaSach = @bookId
            ORDER BY tg.TenTacGia";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bookId", bookId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new ManagerAuthorDto(r.GetInt32(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2)));
        }
        return list;
    }

    public int InsertAuthor(string name, string? biography)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("INSERT INTO TacGia (TenTacGia, TieuSu) OUTPUT INSERTED.IDTacGia VALUES (@name, @bio)", conn);
        cmd.Parameters.AddWithValue("@name", name.Trim());
        cmd.Parameters.AddWithValue("@bio", (object?)biography?.Trim() ?? DBNull.Value);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public void UpdateAuthor(int id, string name, string? biography)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("UPDATE TacGia SET TenTacGia = @name, TieuSu = @bio WHERE IDTacGia = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", name.Trim());
        cmd.Parameters.AddWithValue("@bio", (object?)biography?.Trim() ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void DeleteAuthor(int id)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("DELETE FROM TacGia WHERE IDTacGia = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public bool AuthorHasBooks(int id)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM TacGiaSach WHERE IDTacGia = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return (int)(cmd.ExecuteScalar() ?? 0) > 0;
    }

    public List<ManagerPublisherDto> GetAllPublisherDetails()
    {
        var list = new List<ManagerPublisherDto>();
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT IDNhaXuatBan, TenNhaXuatBan, DiaChi FROM NhaXuatBan ORDER BY TenNhaXuatBan", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(new ManagerPublisherDto(r.GetInt32(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2)));
        return list;
    }

    public int InsertPublisher(string name, string? address)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("INSERT INTO NhaXuatBan (TenNhaXuatBan, DiaChi) OUTPUT INSERTED.IDNhaXuatBan VALUES (@name, @addr)", conn);
        cmd.Parameters.AddWithValue("@name", name.Trim());
        cmd.Parameters.AddWithValue("@addr", (object?)address?.Trim() ?? DBNull.Value);
        return (int)(cmd.ExecuteScalar() ?? 0);
    }

    public void UpdatePublisher(int id, string name, string? address)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("UPDATE NhaXuatBan SET TenNhaXuatBan = @name, DiaChi = @addr WHERE IDNhaXuatBan = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", name.Trim());
        cmd.Parameters.AddWithValue("@addr", (object?)address?.Trim() ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void DeletePublisher(int id)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("DELETE FROM NhaXuatBan WHERE IDNhaXuatBan = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public bool PublisherHasBooks(int id)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM Sach WHERE IDNhaXuatBan = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return (int)(cmd.ExecuteScalar() ?? 0) > 0;
    }

    public List<(string BookId, string Title, decimal Price, int Stock)> GetBooksForOrder(string[] bookIds)
    {
        var list = new List<(string, string, decimal, int)>();
        if (bookIds.Length == 0) return list;
        using var conn = DbConnection.Open();
        var ids = string.Join(",", bookIds.Select((_, i) => $"@id{i}"));
        var sql = $"SELECT MaSach, TenSach, DGBan, SLCo FROM Sach WHERE TrangThai = 1 AND MaSach IN ({ids})";
        using var cmd = new SqlCommand(sql, conn);
        for (int i = 0; i < bookIds.Length; i++) cmd.Parameters.AddWithValue($"@id{i}", bookIds[i]);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add((r.GetString(0), r.GetString(1), r.GetDecimal(2), r.GetInt32(3)));
        return list;
    }

    public (string BookId, string Title, decimal Price, int Stock)? GetBookForCart(string bookId)
    {
        using var conn = DbConnection.Open();
        using var cmd = new SqlCommand("SELECT MaSach, TenSach, DGBan, SLCo FROM Sach WHERE TrangThai = 1 AND MaSach = @id", conn);
        cmd.Parameters.AddWithValue("@id", bookId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return (r.GetString(0), r.GetString(1), r.GetDecimal(2), r.GetInt32(3));
    }
}
