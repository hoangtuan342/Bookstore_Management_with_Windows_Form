/*
   TrangSachDb sample business dataset.
   Safe to re-run: all inserted rows use stable demo keys or names.
   Existing accounts and roles are preserved.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
BEGIN TRANSACTION;

/* Categories */
INSERT dbo.LoaiSach (MaLoai, TenLoai)
SELECT v.MaLoai, v.TenLoai
FROM (VALUES
    (N'DEMO-VP', N'Văn học'),
    (N'DEMO-KN', N'Kinh doanh'),
    (N'DEMO-CN', N'Công nghệ'),
    (N'DEMO-TH', N'Thiếu nhi'),
    (N'DEMO-LS', N'Lịch sử')
) v(MaLoai, TenLoai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.LoaiSach x WHERE x.MaLoai = v.MaLoai);

/* Publishers and authors use identity keys, so resolve them by their stable names. */
INSERT dbo.NhaXuatBan (TenNhaXuatBan, DiaChi)
SELECT v.Ten, v.DiaChi
FROM (VALUES
    (N'NXB Tri Thức Mẫu', N'Hà Nội'),
    (N'NXB Văn Học Mẫu', N'Hà Nội'),
    (N'NXB Công Nghệ Mẫu', N'TP. Hồ Chí Minh'),
    (N'NXB Tuổi Thơ Mẫu', N'TP. Hồ Chí Minh')
) v(Ten, DiaChi)
WHERE NOT EXISTS (SELECT 1 FROM dbo.NhaXuatBan x WHERE x.TenNhaXuatBan = v.Ten);

INSERT dbo.TacGia (TenTacGia, TieuSu)
SELECT v.Ten, v.TieuSu
FROM (VALUES
    (N'Nguyễn Minh An', N'Tác giả văn học và tản văn.'),
    (N'Trần Thu Hà', N'Tác giả sách kỹ năng sống.'),
    (N'Lê Quốc Bảo', N'Tác giả về quản trị và kinh doanh.'),
    (N'Phạm Gia Huy', N'Tác giả sách công nghệ.'),
    (N'Võ Ngọc Mai', N'Tác giả sách thiếu nhi.'),
    (N'Đặng Thành Nam', N'Tác giả nghiên cứu lịch sử.'),
    (N'Hoàng Lan Chi', N'Tác giả và dịch giả.'),
    (N'Bùi Đức Long', N'Tác giả sách khoa học phổ thông.')
) v(Ten, TieuSu)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TacGia x WHERE x.TenTacGia = v.Ten);

/* Additional staff without login accounts; the existing manager account is untouched. */
INSERT dbo.NV (MaNV, HoLot, Ten, NS, AnhChanDung, DiaChi, TrangThaiTaiKhoan, LuongCB, IDTaiKhoan)
SELECT v.MaNV, v.HoLot, v.Ten, v.NS, NULL, v.DiaChi, N'Active', v.LuongCB, NULL
FROM (VALUES
    (N'NV-DEMO-01', N'Nguyễn Văn', N'Minh', CONVERT(date,'1990-04-12'), N'Hà Nội', CONVERT(decimal(18,2),12000000)),
    (N'NV-DEMO-02', N'Trần Thị', N'Lan', CONVERT(date,'1994-08-21'), N'TP. Hồ Chí Minh', CONVERT(decimal(18,2),11000000)),
    (N'NV-DEMO-03', N'Lê Quốc', N'Huy', CONVERT(date,'1988-01-06'), N'Đà Nẵng', CONVERT(decimal(18,2),13500000))
) v(MaNV, HoLot, Ten, NS, DiaChi, LuongCB)
WHERE NOT EXISTS (SELECT 1 FROM dbo.NV x WHERE x.MaNV = v.MaNV);

/* Customers, suppliers */
INSERT dbo.KH (MaKH, HoTen, IDTaiKhoan, SoDienThoai, DiaChiGiaoHang)
SELECT v.MaKH, v.HoTen, NULL, v.SoDienThoai, v.DiaChi
FROM (VALUES
    (N'KH-DEMO-01', N'Nguyễn Minh Anh', N'0901000001', N'12 Nguyễn Trãi, Hà Nội'),
    (N'KH-DEMO-02', N'Trần Gia Bảo', N'0901000002', N'25 Lê Lợi, TP. Hồ Chí Minh'),
    (N'KH-DEMO-03', N'Lê Thu Trang', N'0901000003', N'18 Hùng Vương, Đà Nẵng'),
    (N'KH-DEMO-04', N'Phạm Quốc Khánh', N'0901000004', N'36 Hai Bà Trưng, Hà Nội'),
    (N'KH-DEMO-05', N'Võ Ngọc Hà', N'0901000005', N'42 Nguyễn Huệ, TP. Hồ Chí Minh'),
    (N'KH-DEMO-06', N'Đặng Hoàng Long', N'0901000006', N'09 Trần Phú, Hải Phòng'),
    (N'KH-DEMO-07', N'Bùi Thanh Tâm', N'0901000007', N'51 Phan Châu Trinh, Đà Nẵng'),
    (N'KH-DEMO-08', N'Hoàng Nhật Linh', N'0901000008', N'73 Cầu Giấy, Hà Nội')
) v(MaKH, HoTen, SoDienThoai, DiaChi)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KH x WHERE x.MaKH = v.MaKH);

INSERT dbo.NCC (MaNCC, TenNCC, SoDienThoai)
SELECT v.MaNCC, v.TenNCC, v.SoDienThoai
FROM (VALUES
    (N'NCC-DEMO-01', N'Phát hành Tri Thức', N'02839000001'),
    (N'NCC-DEMO-02', N'Kho sách Văn Học', N'02439000002'),
    (N'NCC-DEMO-03', N'Phân phối Công Nghệ', N'02839000003'),
    (N'NCC-DEMO-04', N'Sách và Thiết bị Giáo dục', N'02439000004')
) v(MaNCC, TenNCC, SoDienThoai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.NCC x WHERE x.MaNCC = v.MaNCC);

/* Books */
INSERT dbo.Sach (MaSach, TenSach, SLCo, DGBan, TrangThai, DVTinh, MaLoai, IDNhaXuatBan, MoTa)
SELECT v.MaSach, v.TenSach, v.SLCo, v.DGBan, 1, N'Cuốn', v.MaLoai, n.IDNhaXuatBan, v.MoTa
FROM (VALUES
    (N'DEMO-S001', N'Những ngày nhiều nắng', 28, CONVERT(decimal(18,2),180000), N'DEMO-VP', N'NXB Văn Học Mẫu', N'Tập truyện về gia đình, tình bạn và những lựa chọn trưởng thành.'),
    (N'DEMO-S002', N'Đi tìm bình yên', 34, CONVERT(decimal(18,2),150000), N'DEMO-VP', N'NXB Văn Học Mẫu', N'Tản văn nhẹ nhàng về cách sống cân bằng và trân trọng hiện tại.'),
    (N'DEMO-S003', N'Tư duy người dẫn đầu', 22, CONVERT(decimal(18,2),220000), N'DEMO-KN', N'NXB Tri Thức Mẫu', N'Phương pháp xây dựng đội ngũ, ra quyết định và phát triển tổ chức.'),
    (N'DEMO-S004', N'Khởi nghiệp từ điều nhỏ nhất', 17, CONVERT(decimal(18,2),195000), N'DEMO-KN', N'NXB Tri Thức Mẫu', N'Hướng dẫn lập kế hoạch, thử nghiệm ý tưởng và quản lý dòng tiền.'),
    (N'DEMO-S005', N'Lập trình thực hành', 26, CONVERT(decimal(18,2),260000), N'DEMO-CN', N'NXB Công Nghệ Mẫu', N'Nhập môn tư duy lập trình qua bài tập và dự án nhỏ.'),
    (N'DEMO-S006', N'Dữ liệu và cuộc sống số', 13, CONVERT(decimal(18,2),240000), N'DEMO-CN', N'NXB Công Nghệ Mẫu', N'Giải thích dữ liệu, quyền riêng tư và ứng dụng trí tuệ nhân tạo.'),
    (N'DEMO-S007', N'Khu vườn của Mít', 42, CONVERT(decimal(18,2),95000), N'DEMO-TH', N'NXB Tuổi Thơ Mẫu', N'Câu chuyện tranh giúp trẻ khám phá thiên nhiên và tình bạn.'),
    (N'DEMO-S008', N'Bí mật dưới đại dương', 31, CONVERT(decimal(18,2),125000), N'DEMO-TH', N'NXB Tuổi Thơ Mẫu', N'Cuộc phiêu lưu khám phá biển sâu dành cho độc giả nhỏ tuổi.'),
    (N'DEMO-S009', N'Dòng chảy lịch sử Việt', 19, CONVERT(decimal(18,2),210000), N'DEMO-LS', N'NXB Tri Thức Mẫu', N'Tổng quan các giai đoạn lịch sử qua tư liệu và câu chuyện.'),
    (N'DEMO-S010', N'Chuyện xưa kể lại', 16, CONVERT(decimal(18,2),175000), N'DEMO-LS', N'NXB Văn Học Mẫu', N'Tuyển chọn những câu chuyện lịch sử dành cho gia đình.'),
    (N'DEMO-S011', N'Thói quen tạo nên khác biệt', 24, CONVERT(decimal(18,2),135000), N'DEMO-KN', N'NXB Tri Thức Mẫu', N'Các bước nhỏ để xây dựng thói quen học tập và làm việc bền vững.'),
    (N'DEMO-S012', N'Bầu trời của những ước mơ', 37, CONVERT(decimal(18,2),110000), N'DEMO-TH', N'NXB Tuổi Thơ Mẫu', N'Truyện thiếu nhi về lòng dũng cảm, sự sẻ chia và ước mơ.')
) v(MaSach, TenSach, SLCo, DGBan, MaLoai, TenNhaXuatBan, MoTa)
JOIN dbo.NhaXuatBan n ON n.TenNhaXuatBan = v.TenNhaXuatBan
WHERE NOT EXISTS (SELECT 1 FROM dbo.Sach x WHERE x.MaSach = v.MaSach);

/* Book-author links */
INSERT dbo.TacGiaSach (MaSach, IDTacGia)
SELECT v.MaSach, a.IDTacGia
FROM (VALUES
    (N'DEMO-S001', N'Nguyễn Minh An'), (N'DEMO-S002', N'Trần Thu Hà'),
    (N'DEMO-S003', N'Lê Quốc Bảo'), (N'DEMO-S004', N'Lê Quốc Bảo'),
    (N'DEMO-S005', N'Phạm Gia Huy'), (N'DEMO-S006', N'Phạm Gia Huy'),
    (N'DEMO-S007', N'Võ Ngọc Mai'), (N'DEMO-S008', N'Võ Ngọc Mai'),
    (N'DEMO-S009', N'Đặng Thành Nam'), (N'DEMO-S010', N'Đặng Thành Nam'),
    (N'DEMO-S011', N'Trần Thu Hà'), (N'DEMO-S012', N'Hoàng Lan Chi'),
    (N'DEMO-S005', N'Bùi Đức Long'), (N'DEMO-S006', N'Bùi Đức Long')
) v(MaSach, TenTacGia)
JOIN dbo.TacGia a ON a.TenTacGia = v.TenTacGia
WHERE NOT EXISTS (SELECT 1 FROM dbo.TacGiaSach x WHERE x.MaSach = v.MaSach AND x.IDTacGia = a.IDTacGia);

/* Vouchers and promotion rules */
INSERT dbo.Voucher (MaVoucher, TrangThai, HanSuDung, SoLanDaSuDung, SoLanToiDa)
SELECT v.MaVoucher, v.TrangThai, v.HanSuDung, v.SoLanDaSuDung, v.SoLanToiDa
FROM (VALUES
    (N'DEMO10', N'Active', DATEADD(day,45,SYSDATETIME()), 8, 100),
    (N'DEMO15', N'Active', DATEADD(day,20,SYSDATETIME()), 3, 50),
    (N'DEMO-USED', N'Used', DATEADD(day,-10,SYSDATETIME()), 20, 20),
    (N'DEMO-EXPIRED', N'Expired', DATEADD(day,-5,SYSDATETIME()), 12, 100)
) v(MaVoucher, TrangThai, HanSuDung, SoLanDaSuDung, SoLanToiDa)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Voucher x WHERE x.MaVoucher = v.MaVoucher);

INSERT dbo.CTKM (MaCTKM, TenCTKM, BeginDate, EndDate)
SELECT v.MaCTKM, v.TenCTKM, v.BeginDate, v.EndDate
FROM (VALUES
    (N'DEMO-KM-SACH', N'Tuần lễ sách hay', DATEADD(day,-14,SYSDATETIME()), DATEADD(day,20,SYSDATETIME())),
    (N'DEMO-KM-DON', N'Ưu đãi đơn hàng', DATEADD(day,-25,SYSDATETIME()), DATEADD(day,30,SYSDATETIME())),
    (N'DEMO-KM-VOUCHER', N'Mã giảm giá thành viên', DATEADD(day,-10,SYSDATETIME()), DATEADD(day,45,SYSDATETIME())),
    (N'DEMO-KM-CU', N'Chương trình tháng trước', DATEADD(day,-100,SYSDATETIME()), DATEADD(day,-70,SYSDATETIME())),
    (N'DEMO-KM-SAP', N'Ưu đãi sắp diễn ra', DATEADD(day,8,SYSDATETIME()), DATEADD(day,25,SYSDATETIME()))
) v(MaCTKM, TenCTKM, BeginDate, EndDate)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CTKM x WHERE x.MaCTKM = v.MaCTKM);

INSERT dbo.KMTheoSach (MaCTKM, MaSach, PhanTramGiam)
SELECT N'DEMO-KM-SACH', v.MaSach, v.PhanTram
FROM (VALUES (N'DEMO-S001',15.0),(N'DEMO-S003',10.0),(N'DEMO-S007',12.5),(N'DEMO-S011',8.0)) v(MaSach, PhanTram)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KMTheoSach x WHERE x.MaCTKM=N'DEMO-KM-SACH' AND x.MaSach=v.MaSach);

INSERT dbo.KMTheoHD (MaCTKM, MucTienHD, PhanTramGiam)
SELECT v.MaCTKM, v.MucTien, v.PhanTram
FROM (VALUES (N'DEMO-KM-DON',300000.0,5.0),(N'DEMO-KM-CU',200000.0,7.5),(N'DEMO-KM-SAP',500000.0,10.0)) v(MaCTKM, MucTien, PhanTram)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KMTheoHD x WHERE x.MaCTKM=v.MaCTKM AND x.MucTienHD=v.MucTien);

INSERT dbo.KMTheoVoucher (MaCTKM, MaVoucher, PhanTramGiam)
SELECT v.MaCTKM, v.MaVoucher, v.PhanTram
FROM (VALUES (N'DEMO-KM-VOUCHER',N'DEMO10',10.0),(N'DEMO-KM-VOUCHER',N'DEMO15',15.0)) v(MaCTKM, MaVoucher, PhanTram)
WHERE NOT EXISTS (SELECT 1 FROM dbo.KMTheoVoucher x WHERE x.MaCTKM=v.MaCTKM AND x.MaVoucher=v.MaVoucher);

/* Purchase receipts: posted records power purchase and inventory reports; one cancelled receipt tests status filtering. */
INSERT dbo.PNH (MaPNH, NgayLap, MaNV, MaNCC, TongTien, TrangThai)
SELECT v.MaPNH, DATEADD(day,v.DaysAgo,SYSDATETIME()), v.MaNV, v.MaNCC, v.TongTien, v.TrangThai
FROM (VALUES
    (N'DEMO-PN-001',-330,N'NV-DEMO-02',N'NCC-DEMO-01',CONVERT(decimal(18,2),1650000),N'Posted'),
    (N'DEMO-PN-002',-190,N'NV-DEMO-02',N'NCC-DEMO-02',CONVERT(decimal(18,2),1680000),N'Posted'),
    (N'DEMO-PN-003',-95,N'NV-DEMO-02',N'NCC-DEMO-03',CONVERT(decimal(18,2),2600000),N'Posted'),
    (N'DEMO-PN-004',-55,N'NV-DEMO-02',N'NCC-DEMO-04',CONVERT(decimal(18,2),1320000),N'Posted'),
    (N'DEMO-PN-005',-25,N'NV-DEMO-02',N'NCC-DEMO-01',CONVERT(decimal(18,2),1875000),N'Posted'),
    (N'DEMO-PN-006',-12,N'NV-DEMO-02',N'NCC-DEMO-02',CONVERT(decimal(18,2),1350000),N'Posted'),
    (N'DEMO-PN-007',-4,N'NV-DEMO-02',N'NCC-DEMO-03',CONVERT(decimal(18,2),1940000),N'Posted'),
    (N'DEMO-PN-008',-2,N'NV-DEMO-02',N'NCC-DEMO-04',CONVERT(decimal(18,2),1700000),N'Cancelled')
) v(MaPNH, DaysAgo, MaNV, MaNCC, TongTien, TrangThai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.PNH x WHERE x.MaPNH=v.MaPNH);

INSERT dbo.CTPNH (MaPNH, MaSach, SL, DG, ThanhTien)
SELECT v.MaPNH, v.MaSach, v.SL, v.DG, v.SL*v.DG
FROM (VALUES
    (N'DEMO-PN-001',N'DEMO-S001',10,90000.0),(N'DEMO-PN-001',N'DEMO-S002',10,75000.0),
    (N'DEMO-PN-002',N'DEMO-S003',8,110000.0),(N'DEMO-PN-002',N'DEMO-S004',8,100000.0),
    (N'DEMO-PN-003',N'DEMO-S005',10,130000.0),(N'DEMO-PN-003',N'DEMO-S006',10,130000.0),
    (N'DEMO-PN-004',N'DEMO-S007',12,55000.0),(N'DEMO-PN-004',N'DEMO-S008',12,55000.0),
    (N'DEMO-PN-005',N'DEMO-S009',5,125000.0),(N'DEMO-PN-005',N'DEMO-S010',5,125000.0),(N'DEMO-PN-005',N'DEMO-S011',5,125000.0),
    (N'DEMO-PN-006',N'DEMO-S012',10,60000.0),(N'DEMO-PN-006',N'DEMO-S001',5,90000.0),(N'DEMO-PN-006',N'DEMO-S002',4,75000.0),
    (N'DEMO-PN-007',N'DEMO-S003',6,110000.0),(N'DEMO-PN-007',N'DEMO-S005',6,130000.0),(N'DEMO-PN-007',N'DEMO-S009',4,125000.0),
    (N'DEMO-PN-008',N'DEMO-S007',10,55000.0),(N'DEMO-PN-008',N'DEMO-S012',10,60000.0),(N'DEMO-PN-008',N'DEMO-S008',10,55000.0)
) v(MaPNH, MaSach, SL, DG)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CTPNH x WHERE x.MaPNH=v.MaPNH AND x.MaSach=v.MaSach);

/* Orders cover multiple periods and lifecycle/payment states for reports and filters. */
INSERT dbo.HD (MaHD, NgayLapHD, MaNV, MaKH, TongTien, TrangThaiDonHang, TrangThaiThanhToan, PhuongThucThanhToan, DiaChiGiaoHang, SDTNhanHang, MaVoucher, TongGiamGia)
SELECT v.MaHD, DATEADD(day,v.DaysAgo,SYSDATETIME()), v.MaNV, v.MaKH, v.TongTien, v.TrangThaiDonHang, v.TrangThaiThanhToan, v.PhuongThucThanhToan, v.DiaChi, v.SoDienThoai, v.MaVoucher, v.GiamGia
FROM (VALUES
    (N'DEMO-HD-001',-340,N'NV-DEMO-01',N'KH-DEMO-01',CONVERT(decimal(18,2),450000),N'Completed',N'Paid',N'CashOnDelivery',N'12 Nguyễn Trãi, Hà Nội',N'0901000001',NULL,CONVERT(decimal(18,2),30000)),
    (N'DEMO-HD-002',-210,N'NV-DEMO-01',N'KH-DEMO-02',CONVERT(decimal(18,2),418000),N'Completed',N'Paid',N'BankTransfer',N'25 Lê Lợi, TP. Hồ Chí Minh',N'0901000002',N'DEMO10',CONVERT(decimal(18,2),22000)),
    (N'DEMO-HD-003',-150,N'NV-DEMO-03',N'KH-DEMO-03',CONVERT(decimal(18,2),450000),N'Completed',N'Paid',N'CashOnDelivery',N'18 Hùng Vương, Đà Nẵng',N'0901000003',NULL,CONVERT(decimal(18,2),0)),
    (N'DEMO-HD-004',-95,N'NV-DEMO-01',N'KH-DEMO-04',CONVERT(decimal(18,2),527000),N'Completed',N'Paid',N'BankTransfer',N'36 Hai Bà Trưng, Hà Nội',N'0901000004',N'DEMO10',CONVERT(decimal(18,2),33000)),
    (N'DEMO-HD-005',-62,N'NV-DEMO-03',N'KH-DEMO-05',CONVERT(decimal(18,2),410000),N'Completed',N'Paid',N'CashOnDelivery',N'42 Nguyễn Huệ, TP. Hồ Chí Minh',N'0901000005',NULL,CONVERT(decimal(18,2),0)),
    (N'DEMO-HD-006',-31,N'NV-DEMO-01',N'KH-DEMO-06',CONVERT(decimal(18,2),520000),N'Completed',N'Paid',N'BankTransfer',N'09 Trần Phú, Hải Phòng',N'0901000006',N'DEMO15',CONVERT(decimal(18,2),30000)),
    (N'DEMO-HD-007',-20,N'NV-DEMO-03',N'KH-DEMO-07',CONVERT(decimal(18,2),220000),N'Shipping',N'Unpaid',N'CashOnDelivery',N'51 Phan Châu Trinh, Đà Nẵng',N'0901000007',NULL,CONVERT(decimal(18,2),0)),
    (N'DEMO-HD-008',-10,N'NV-DEMO-01',N'KH-DEMO-08',CONVERT(decimal(18,2),491000),N'Completed',N'Paid',N'BankTransfer',N'73 Cầu Giấy, Hà Nội',N'0901000008',N'DEMO10',CONVERT(decimal(18,2),39000)),
    (N'DEMO-HD-009',-6,N'NV-DEMO-03',N'KH-DEMO-01',CONVERT(decimal(18,2),320000),N'Pending',N'Unpaid',N'CashOnDelivery',N'12 Nguyễn Trãi, Hà Nội',N'0901000001',NULL,CONVERT(decimal(18,2),0)),
    (N'DEMO-HD-010',-3,N'NV-DEMO-01',N'KH-DEMO-02',CONVERT(decimal(18,2),481000),N'Confirmed',N'AwaitingPayment',N'BankTransfer',N'25 Lê Lợi, TP. Hồ Chí Minh',N'0901000002',N'DEMO10',CONVERT(decimal(18,2),44000)),
    (N'DEMO-HD-011',-2,N'NV-DEMO-03',N'KH-DEMO-04',CONVERT(decimal(18,2),180000),N'Cancelled',N'Cancelled',N'CashOnDelivery',N'36 Hai Bà Trưng, Hà Nội',N'0901000004',NULL,CONVERT(decimal(18,2),0)),
    (N'DEMO-HD-012',-1,N'NV-DEMO-01',N'KH-DEMO-05',CONVERT(decimal(18,2),220000),N'Completed',N'Paid',N'CashOnDelivery',N'42 Nguyễn Huệ, TP. Hồ Chí Minh',N'0901000005',NULL,CONVERT(decimal(18,2),0)),
    (N'DEMO-HD-013',-1,N'NV-DEMO-03',N'KH-DEMO-06',CONVERT(decimal(18,2),370000),N'Pending',N'Unpaid',N'CashOnDelivery',N'09 Trần Phú, Hải Phòng',N'0901000006',NULL,CONVERT(decimal(18,2),0))
) v(MaHD, DaysAgo, MaNV, MaKH, TongTien, TrangThaiDonHang, TrangThaiThanhToan, PhuongThucThanhToan, DiaChi, SoDienThoai, MaVoucher, GiamGia)
WHERE NOT EXISTS (SELECT 1 FROM dbo.HD x WHERE x.MaHD=v.MaHD);

INSERT dbo.CTHD (MaHD, MaSach, SL, DG, ThanhTien)
SELECT v.MaHD, v.MaSach, v.SL, v.DG, v.SL*v.DG
FROM (VALUES
    (N'DEMO-HD-001',N'DEMO-S001',1,180000.0),(N'DEMO-HD-001',N'DEMO-S002',2,150000.0),
    (N'DEMO-HD-002',N'DEMO-S003',2,220000.0),
    (N'DEMO-HD-003',N'DEMO-S005',1,260000.0),(N'DEMO-HD-003',N'DEMO-S007',2,95000.0),
    (N'DEMO-HD-004',N'DEMO-S006',1,240000.0),(N'DEMO-HD-004',N'DEMO-S009',1,210000.0),(N'DEMO-HD-004',N'DEMO-S012',1,110000.0),
    (N'DEMO-HD-005',N'DEMO-S008',1,125000.0),(N'DEMO-HD-005',N'DEMO-S011',1,135000.0),(N'DEMO-HD-005',N'DEMO-S002',1,150000.0),
    (N'DEMO-HD-006',N'DEMO-S004',1,195000.0),(N'DEMO-HD-006',N'DEMO-S005',1,260000.0),(N'DEMO-HD-006',N'DEMO-S007',1,95000.0),
    (N'DEMO-HD-007',N'DEMO-S003',1,220000.0),
    (N'DEMO-HD-008',N'DEMO-S001',1,180000.0),(N'DEMO-HD-008',N'DEMO-S006',1,240000.0),(N'DEMO-HD-008',N'DEMO-S012',1,110000.0),
    (N'DEMO-HD-009',N'DEMO-S004',1,195000.0),(N'DEMO-HD-009',N'DEMO-S008',1,125000.0),
    (N'DEMO-HD-010',N'DEMO-S003',1,220000.0),(N'DEMO-HD-010',N'DEMO-S009',1,210000.0),(N'DEMO-HD-010',N'DEMO-S007',1,95000.0),
    (N'DEMO-HD-011',N'DEMO-S001',1,180000.0),
    (N'DEMO-HD-012',N'DEMO-S003',1,220000.0),
    (N'DEMO-HD-013',N'DEMO-S005',1,260000.0),(N'DEMO-HD-013',N'DEMO-S012',1,110000.0)
) v(MaHD, MaSach, SL, DG)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CTHD x WHERE x.MaHD=v.MaHD AND x.MaSach=v.MaSach);

/* Sample cart contents */
INSERT dbo.GioHang (MaKH, MaSach, SoLuong)
SELECT v.MaKH, v.MaSach, v.SoLuong
FROM (VALUES
    (N'KH-DEMO-01',N'DEMO-S005',1),(N'KH-DEMO-01',N'DEMO-S011',2),
    (N'KH-DEMO-03',N'DEMO-S008',1),(N'KH-DEMO-05',N'DEMO-S001',1),
    (N'KH-DEMO-07',N'DEMO-S012',3),(N'KH-DEMO-08',N'DEMO-S003',1)
) v(MaKH, MaSach, SoLuong)
WHERE NOT EXISTS (SELECT 1 FROM dbo.GioHang x WHERE x.MaKH=v.MaKH AND x.MaSach=v.MaSach);

/* Audit activity for the management log screen */
INSERT dbo.SystemAuditLog (OccurredAt, ActorAccountId, ActorName, ActorRole, Action, Area, Resource, StatusCode, IpAddress, ActorCode)
SELECT DATEADD(minute,-v.MinutesAgo,SYSDATETIME()), 1003, N'testadmin', N'Manager', v.Action, v.Area, v.Resource, v.StatusCode, N'127.0.0.1', N'NV09D53E52C4'
FROM (VALUES
    (1440,N'Create',N'Books',N'DEMO-S001',201),(1320,N'Create',N'Customers',N'KH-DEMO-01',201),
    (1200,N'Create',N'Inventory',N'DEMO-PN-006',201),(960,N'Update',N'Promotions',N'DEMO-KM-SACH',200),
    (720,N'Create',N'Orders',N'DEMO-HD-010',201),(480,N'Update',N'Orders',N'DEMO-HD-007',200),
    (240,N'Create',N'Orders',N'DEMO-HD-012',201),(60,N'Update',N'Vouchers',N'DEMO10',200)
) v(MinutesAgo, Action, Area, Resource, StatusCode)
WHERE NOT EXISTS (SELECT 1 FROM dbo.SystemAuditLog x WHERE x.ActorAccountId=1003 AND x.Resource=v.Resource AND x.Action=v.Action AND x.OccurredAt >= DATEADD(day,-2,SYSDATETIME()));

COMMIT TRANSACTION;

/* Summary for the operator */
SELECT 'Sach' AS Bang, COUNT(*) AS SoDong FROM dbo.Sach
UNION ALL SELECT 'KH', COUNT(*) FROM dbo.KH
UNION ALL SELECT 'NCC', COUNT(*) FROM dbo.NCC
UNION ALL SELECT 'PNH', COUNT(*) FROM dbo.PNH
UNION ALL SELECT 'HD', COUNT(*) FROM dbo.HD
UNION ALL SELECT 'CTKM', COUNT(*) FROM dbo.CTKM
UNION ALL SELECT 'Voucher', COUNT(*) FROM dbo.Voucher
UNION ALL SELECT 'GioHang', COUNT(*) FROM dbo.GioHang
UNION ALL SELECT 'SystemAuditLog', COUNT(*) FROM dbo.SystemAuditLog;
