-- ==============================================================================
-- CƠ SỞ DỮ LIỆU: TrangSachDb
-- Script tạo toàn bộ bảng, khóa chính, khóa ngoại, chỉ mục và vai trò mặc định
-- Tương thích với Microsoft SQL Server 2017+ / Azure SQL / SQL Server Express
-- ==============================================================================

USE [master];
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'TrangSachDb')
BEGIN
    CREATE DATABASE [TrangSachDb];
END
GO

USE [TrangSachDb];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. BẢNG THỂ LOẠI SÁCH (LoaiSach)
IF OBJECT_ID(N'dbo.LoaiSach', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoaiSach (
        MaLoai NVARCHAR(20) NOT NULL,
        TenLoai NVARCHAR(120) NOT NULL,
        CONSTRAINT PK_LoaiSach PRIMARY KEY CLUSTERED (MaLoai)
    );
END
GO

-- 2. BẢNG NHÀ XUẤT BẢN (NhaXuatBan)
IF OBJECT_ID(N'dbo.NhaXuatBan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NhaXuatBan (
        IDNhaXuatBan INT IDENTITY(1,1) NOT NULL,
        TenNhaXuatBan NVARCHAR(180) NOT NULL,
        DiaChi NVARCHAR(300) NULL,
        CONSTRAINT PK_NhaXuatBan PRIMARY KEY CLUSTERED (IDNhaXuatBan)
    );
END
GO

-- 3. BẢNG TÁC GIẢ (TacGia)
IF OBJECT_ID(N'dbo.TacGia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TacGia (
        IDTacGia INT IDENTITY(1,1) NOT NULL,
        TenTacGia NVARCHAR(180) NOT NULL,
        TieuSu NVARCHAR(MAX) NULL,
        CONSTRAINT PK_TacGia PRIMARY KEY CLUSTERED (IDTacGia)
    );
END
GO

-- 4. BẢNG SÁCH (Sach)
IF OBJECT_ID(N'dbo.Sach', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sach (
        MaSach NVARCHAR(30) NOT NULL,
        TenSach NVARCHAR(200) NOT NULL,
        MoTa NVARCHAR(MAX) NULL,
        ISBN NVARCHAR(30) NULL,
        SLCo INT NOT NULL DEFAULT 0,
        DGBan DECIMAL(18,2) NOT NULL DEFAULT 0,
        TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Active',
        DVTinh NVARCHAR(30) NOT NULL DEFAULT N'Cuốn',
        MaLoai NVARCHAR(20) NOT NULL,
        IDNhaXuatBan INT NOT NULL,
        CoverImagePath NVARCHAR(300) NULL,
        CONSTRAINT PK_Sach PRIMARY KEY CLUSTERED (MaSach),
        CONSTRAINT FK_Sach_LoaiSach FOREIGN KEY (MaLoai) REFERENCES dbo.LoaiSach (MaLoai),
        CONSTRAINT FK_Sach_NhaXuatBan FOREIGN KEY (IDNhaXuatBan) REFERENCES dbo.NhaXuatBan (IDNhaXuatBan)
    );
    CREATE NONCLUSTERED INDEX IX_Sach_MaLoai ON dbo.Sach (MaLoai);
    CREATE NONCLUSTERED INDEX IX_Sach_IDNhaXuatBan ON dbo.Sach (IDNhaXuatBan);
    CREATE NONCLUSTERED INDEX IX_Sach_TenSach ON dbo.Sach (TenSach);
END
GO

-- 5. BẢNG LIÊN KẾT TÁC GIẢ - SÁCH (TacGiaSach: quan hệ N - N)
IF OBJECT_ID(N'dbo.TacGiaSach', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TacGiaSach (
        MaSach NVARCHAR(30) NOT NULL,
        IDTacGia INT NOT NULL,
        CONSTRAINT PK_TacGiaSach PRIMARY KEY CLUSTERED (MaSach, IDTacGia),
        CONSTRAINT FK_TacGiaSach_Sach FOREIGN KEY (MaSach) REFERENCES dbo.Sach (MaSach) ON DELETE CASCADE,
        CONSTRAINT FK_TacGiaSach_TacGia FOREIGN KEY (IDTacGia) REFERENCES dbo.TacGia (IDTacGia) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_TacGiaSach_IDTacGia ON dbo.TacGiaSach (IDTacGia);
END
GO

-- 6. BẢNG NHÀ CUNG CẤP (NCC)
IF OBJECT_ID(N'dbo.NCC', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NCC (
        MaNCC NVARCHAR(20) NOT NULL,
        TenNCC NVARCHAR(180) NOT NULL,
        SoDienThoai NVARCHAR(30) NULL,
        CONSTRAINT PK_NCC PRIMARY KEY CLUSTERED (MaNCC)
    );
END
GO

-- 7. BẢNG VAI TRÒ (VaiTro)
IF OBJECT_ID(N'dbo.VaiTro', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VaiTro (
        IDVaiTro INT IDENTITY(1,1) NOT NULL,
        TenVaiTro NVARCHAR(60) NOT NULL,
        CONSTRAINT PK_VaiTro PRIMARY KEY CLUSTERED (IDVaiTro),
        CONSTRAINT UQ_VaiTro_TenVaiTro UNIQUE (TenVaiTro)
    );
END
GO

-- Khởi tạo danh sách các vai trò mặc định
IF NOT EXISTS (SELECT 1 FROM dbo.VaiTro WHERE TenVaiTro = N'Manager') INSERT INTO dbo.VaiTro (TenVaiTro) VALUES (N'Manager');
IF NOT EXISTS (SELECT 1 FROM dbo.VaiTro WHERE TenVaiTro = N'Sales') INSERT INTO dbo.VaiTro (TenVaiTro) VALUES (N'Sales');
IF NOT EXISTS (SELECT 1 FROM dbo.VaiTro WHERE TenVaiTro = N'Inventory') INSERT INTO dbo.VaiTro (TenVaiTro) VALUES (N'Inventory');
IF NOT EXISTS (SELECT 1 FROM dbo.VaiTro WHERE TenVaiTro = N'Catalog') INSERT INTO dbo.VaiTro (TenVaiTro) VALUES (N'Catalog');
IF NOT EXISTS (SELECT 1 FROM dbo.VaiTro WHERE TenVaiTro = N'Marketing') INSERT INTO dbo.VaiTro (TenVaiTro) VALUES (N'Marketing');
IF NOT EXISTS (SELECT 1 FROM dbo.VaiTro WHERE TenVaiTro = N'Accountant') INSERT INTO dbo.VaiTro (TenVaiTro) VALUES (N'Accountant');
IF NOT EXISTS (SELECT 1 FROM dbo.VaiTro WHERE TenVaiTro = N'Customer') INSERT INTO dbo.VaiTro (TenVaiTro) VALUES (N'Customer');
GO

-- 8. BẢNG TÀI KHOẢN (TaiKhoan)
IF OBJECT_ID(N'dbo.TaiKhoan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaiKhoan (
        IDTaiKhoan INT IDENTITY(1,1) NOT NULL,
        TenDangNhap NVARCHAR(80) NOT NULL,
        Email NVARCHAR(254) NOT NULL,
        MatKhauHash NVARCHAR(255) NOT NULL,
        IDVaiTro INT NOT NULL,
        TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Active',
        CONSTRAINT PK_TaiKhoan PRIMARY KEY CLUSTERED (IDTaiKhoan),
        CONSTRAINT UQ_TaiKhoan_TenDangNhap UNIQUE (TenDangNhap),
        CONSTRAINT UQ_TaiKhoan_Email UNIQUE (Email),
        CONSTRAINT FK_TaiKhoan_VaiTro FOREIGN KEY (IDVaiTro) REFERENCES dbo.VaiTro (IDVaiTro)
    );
    CREATE NONCLUSTERED INDEX IX_TaiKhoan_IDVaiTro ON dbo.TaiKhoan (IDVaiTro);
END
GO

-- 9. BẢNG NHÂN VIÊN (NV)
IF OBJECT_ID(N'dbo.NV', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NV (
        MaNV NVARCHAR(20) NOT NULL,
        HoLot NVARCHAR(100) NOT NULL,
        Ten NVARCHAR(50) NOT NULL,
        NS DATE NULL,
        AnhChanDung NVARCHAR(300) NULL,
        DiaChi NVARCHAR(300) NULL,
        TrangThaiTaiKhoan NVARCHAR(30) NOT NULL DEFAULT N'Active',
        LuongCB DECIMAL(18,2) NOT NULL DEFAULT 0,
        IDTaiKhoan INT NULL,
        CONSTRAINT PK_NV PRIMARY KEY CLUSTERED (MaNV),
        CONSTRAINT FK_NV_TaiKhoan FOREIGN KEY (IDTaiKhoan) REFERENCES dbo.TaiKhoan (IDTaiKhoan),
        CONSTRAINT UQ_NV_IDTaiKhoan UNIQUE (IDTaiKhoan)
    );
END
GO

-- 10. BẢNG KHÁCH HÀNG (KH)
IF OBJECT_ID(N'dbo.KH', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KH (
        MaKH NVARCHAR(30) NOT NULL,
        HoTen NVARCHAR(180) NOT NULL,
        NgaySinh DATE NULL,
        SoDienThoai NVARCHAR(30) NULL,
        DiaChiGiaoHang NVARCHAR(300) NULL,
        IDTaiKhoan INT NULL,
        CONSTRAINT PK_KH PRIMARY KEY CLUSTERED (MaKH),
        CONSTRAINT FK_KH_TaiKhoan FOREIGN KEY (IDTaiKhoan) REFERENCES dbo.TaiKhoan (IDTaiKhoan),
        CONSTRAINT UQ_KH_IDTaiKhoan UNIQUE (IDTaiKhoan)
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.KH') AND name = 'NgaySinh')
    BEGIN
        ALTER TABLE dbo.KH ADD NgaySinh DATE NULL;
    END
END
GO

-- 11. BẢNG VOUCHER (Voucher)
IF OBJECT_ID(N'dbo.Voucher', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Voucher (
        MaVoucher NVARCHAR(40) NOT NULL,
        TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Active',
        HanSuDung DATETIME2 NULL,
        SoLanDaSuDung INT NOT NULL DEFAULT 0,
        SoLanToiDa INT NULL,
        CONSTRAINT PK_Voucher PRIMARY KEY CLUSTERED (MaVoucher)
    );
END
GO

-- 12. BẢNG CHƯƠNG TRÌNH KHUYẾN MÃI (CTKM)
IF OBJECT_ID(N'dbo.CTKM', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CTKM (
        MaCTKM NVARCHAR(30) NOT NULL,
        TenCTKM NVARCHAR(180) NOT NULL,
        BeginDate DATETIME2 NOT NULL,
        EndDate DATETIME2 NOT NULL,
        CONSTRAINT PK_CTKM PRIMARY KEY CLUSTERED (MaCTKM)
    );
END
GO

-- 13. BẢNG KHUYẾN MÃI THEO SÁCH (KMTheoSach)
IF OBJECT_ID(N'dbo.KMTheoSach', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KMTheoSach (
        MaCTKM NVARCHAR(30) NOT NULL,
        MaSach NVARCHAR(30) NOT NULL,
        TiLeGiamGia DECIMAL(5,2) NOT NULL DEFAULT 0,
        CONSTRAINT PK_KMTheoSach PRIMARY KEY CLUSTERED (MaCTKM, MaSach),
        CONSTRAINT FK_KMTheoSach_CTKM FOREIGN KEY (MaCTKM) REFERENCES dbo.CTKM (MaCTKM) ON DELETE CASCADE,
        CONSTRAINT FK_KMTheoSach_Sach FOREIGN KEY (MaSach) REFERENCES dbo.Sach (MaSach) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_KMTheoSach_MaSach ON dbo.KMTheoSach (MaSach);
END
GO

-- 14. BẢNG KHUYẾN MÃI THEO HÓA ĐƠN (KMTheoHD)
IF OBJECT_ID(N'dbo.KMTheoHD', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KMTheoHD (
        MaCTKM NVARCHAR(30) NOT NULL,
        GiaTriToiThieu DECIMAL(18,2) NOT NULL DEFAULT 0,
        TiLeGiamGia DECIMAL(5,2) NOT NULL DEFAULT 0,
        CONSTRAINT PK_KMTheoHD PRIMARY KEY CLUSTERED (MaCTKM, GiaTriToiThieu),
        CONSTRAINT FK_KMTheoHD_CTKM FOREIGN KEY (MaCTKM) REFERENCES dbo.CTKM (MaCTKM) ON DELETE CASCADE
    );
END
GO

-- 15. BẢNG KHUYẾN MÃI THEO VOUCHER (KMTheoVoucher)
IF OBJECT_ID(N'dbo.KMTheoVoucher', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KMTheoVoucher (
        MaCTKM NVARCHAR(30) NOT NULL,
        MaVoucher NVARCHAR(40) NOT NULL,
        GiaTriToiThieu DECIMAL(18,2) NOT NULL DEFAULT 0,
        TiLeGiamGia DECIMAL(5,2) NOT NULL DEFAULT 0,
        CONSTRAINT PK_KMTheoVoucher PRIMARY KEY CLUSTERED (MaCTKM, MaVoucher),
        CONSTRAINT FK_KMTheoVoucher_CTKM FOREIGN KEY (MaCTKM) REFERENCES dbo.CTKM (MaCTKM) ON DELETE CASCADE,
        CONSTRAINT FK_KMTheoVoucher_Voucher FOREIGN KEY (MaVoucher) REFERENCES dbo.Voucher (MaVoucher) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_KMTheoVoucher_MaVoucher ON dbo.KMTheoVoucher (MaVoucher);
END
GO

-- 16. BẢNG HÓA ĐƠN / ĐƠN HÀNG (HD)
IF OBJECT_ID(N'dbo.HD', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HD (
        MaHD NVARCHAR(30) NOT NULL,
        NgayLapHD DATETIME2 NOT NULL,
        TongTien DECIMAL(18,2) NOT NULL DEFAULT 0,
        TongGiamGia DECIMAL(18,2) NOT NULL DEFAULT 0,
        MaKH NVARCHAR(30) NOT NULL,
        MaNV NVARCHAR(20) NULL,
        MaVoucher NVARCHAR(40) NULL,
        DiaChiGiaoHang NVARCHAR(300) NULL,
        SoDienThoaiNhan NVARCHAR(30) NULL,
        PhuongThucThanhToan NVARCHAR(30) NOT NULL DEFAULT N'COD',
        TrangThaiThanhToan NVARCHAR(30) NOT NULL DEFAULT N'Pending',
        TrangThaiDonHang NVARCHAR(30) NOT NULL DEFAULT N'Submitted',
        GhiChu NVARCHAR(500) NULL,
        CONSTRAINT PK_HD PRIMARY KEY CLUSTERED (MaHD),
        CONSTRAINT FK_HD_KH FOREIGN KEY (MaKH) REFERENCES dbo.KH (MaKH),
        CONSTRAINT FK_HD_NV FOREIGN KEY (MaNV) REFERENCES dbo.NV (MaNV),
        CONSTRAINT FK_HD_Voucher FOREIGN KEY (MaVoucher) REFERENCES dbo.Voucher (MaVoucher)
    );
    CREATE NONCLUSTERED INDEX IX_HD_MaKH ON dbo.HD (MaKH);
    CREATE NONCLUSTERED INDEX IX_HD_MaNV ON dbo.HD (MaNV);
    CREATE NONCLUSTERED INDEX IX_HD_MaVoucher ON dbo.HD (MaVoucher);
    CREATE NONCLUSTERED INDEX IX_HD_NgayLapHD_TrangThaiDonHang ON dbo.HD (NgayLapHD, TrangThaiDonHang);
END
GO

-- 17. BẢNG CHI TIẾT HÓA ĐƠN (CTHD)
IF OBJECT_ID(N'dbo.CTHD', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CTHD (
        MaHD NVARCHAR(30) NOT NULL,
        MaSach NVARCHAR(30) NOT NULL,
        SoLuong INT NOT NULL DEFAULT 1,
        DonGia DECIMAL(18,2) NOT NULL DEFAULT 0,
        CONSTRAINT PK_CTHD PRIMARY KEY CLUSTERED (MaHD, MaSach),
        CONSTRAINT FK_CTHD_HD FOREIGN KEY (MaHD) REFERENCES dbo.HD (MaHD) ON DELETE CASCADE,
        CONSTRAINT FK_CTHD_Sach FOREIGN KEY (MaSach) REFERENCES dbo.Sach (MaSach)
    );
    CREATE NONCLUSTERED INDEX IX_CTHD_MaSach ON dbo.CTHD (MaSach);
END
GO

-- 18. BẢNG PHIẾU NHẬP HÀNG (PNH)
IF OBJECT_ID(N'dbo.PNH', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PNH (
        MaPNH NVARCHAR(30) NOT NULL,
        NgayLap DATETIME2 NOT NULL,
        TongTien DECIMAL(18,2) NOT NULL DEFAULT 0,
        MaNCC NVARCHAR(20) NOT NULL,
        MaNV NVARCHAR(20) NOT NULL,
        TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Posted',
        GhiChu NVARCHAR(500) NULL,
        CONSTRAINT PK_PNH PRIMARY KEY CLUSTERED (MaPNH),
        CONSTRAINT FK_PNH_NCC FOREIGN KEY (MaNCC) REFERENCES dbo.NCC (MaNCC),
        CONSTRAINT FK_PNH_NV FOREIGN KEY (MaNV) REFERENCES dbo.NV (MaNV)
    );
    CREATE NONCLUSTERED INDEX IX_PNH_MaNCC ON dbo.PNH (MaNCC);
    CREATE NONCLUSTERED INDEX IX_PNH_MaNV ON dbo.PNH (MaNV);
END
GO

-- 19. BẢNG CHI TIẾT PHIẾU NHẬP HÀNG (CTPNH)
IF OBJECT_ID(N'dbo.CTPNH', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CTPNH (
        MaPNH NVARCHAR(30) NOT NULL,
        MaSach NVARCHAR(30) NOT NULL,
        SoLuong INT NOT NULL DEFAULT 1,
        DonGia DECIMAL(18,2) NOT NULL DEFAULT 0,
        CONSTRAINT PK_CTPNH PRIMARY KEY CLUSTERED (MaPNH, MaSach),
        CONSTRAINT FK_CTPNH_PNH FOREIGN KEY (MaPNH) REFERENCES dbo.PNH (MaPNH) ON DELETE CASCADE,
        CONSTRAINT FK_CTPNH_Sach FOREIGN KEY (MaSach) REFERENCES dbo.Sach (MaSach)
    );
    CREATE NONCLUSTERED INDEX IX_CTPNH_MaSach ON dbo.CTPNH (MaSach);
END
GO

-- 20. BẢNG GIỎ HÀNG KHÁCH HÀNG (GioHang)
IF OBJECT_ID(N'dbo.GioHang', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GioHang (
        MaKH NVARCHAR(30) NOT NULL,
        MaSach NVARCHAR(30) NOT NULL,
        SoLuong INT NOT NULL DEFAULT 1,
        CONSTRAINT PK_GioHang PRIMARY KEY CLUSTERED (MaKH, MaSach),
        CONSTRAINT FK_GioHang_KH FOREIGN KEY (MaKH) REFERENCES dbo.KH (MaKH) ON DELETE CASCADE,
        CONSTRAINT FK_GioHang_Sach FOREIGN KEY (MaSach) REFERENCES dbo.Sach (MaSach) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_GioHang_MaSach ON dbo.GioHang (MaSach);
END
GO

-- 21. BẢNG NHẬT KÝ HỆ THỐNG (SystemAuditLog)
IF OBJECT_ID(N'dbo.SystemAuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemAuditLog (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        OccurredAt DATETIME2 NOT NULL,
        ActorAccountId INT NULL,
        ActorName NVARCHAR(100) NOT NULL,
        ActorCode NVARCHAR(30) NULL,
        ActorRole NVARCHAR(60) NOT NULL,
        Action NVARCHAR(10) NOT NULL,
        Area NVARCHAR(100) NOT NULL,
        Resource NVARCHAR(300) NOT NULL,
        StatusCode INT NOT NULL,
        IpAddress NVARCHAR(64) NULL,
        CONSTRAINT PK_SystemAuditLog PRIMARY KEY CLUSTERED (Id)
    );
    CREATE NONCLUSTERED INDEX IX_SystemAuditLog_OccurredAt ON dbo.SystemAuditLog (OccurredAt);
END
GO
