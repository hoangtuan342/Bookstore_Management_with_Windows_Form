/*
   Replaces the twelve synthetic demo titles with real Vietnamese editions.
   Stable MaSach values, stock quantities, sale prices and all existing order /
   inventory line values are retained, so demo transaction links remain valid.
   Cover files are stored under Apps/Customer/Web/wwwroot/images/books.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.Sach', N'ISBN') IS NULL
    ALTER TABLE dbo.Sach ADD ISBN nvarchar(20) NULL;
GO
IF COL_LENGTH(N'dbo.Sach', N'CoverImagePath') IS NULL
    ALTER TABLE dbo.Sach ADD CoverImagePath nvarchar(260) NULL;
GO

BEGIN TRANSACTION;

INSERT dbo.NhaXuatBan (TenNhaXuatBan, DiaChi)
SELECT v.Name, N'Việt Nam'
FROM (VALUES
    (N'NXB Trẻ'),
    (N'NXB Kim Đồng'),
    (N'NXB Hội Nhà Văn'),
    (N'NXB Tổng hợp Thành phố Hồ Chí Minh'),
    (N'NXB Thế Giới'),
    (N'NXB Tri Thức')
) v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.NhaXuatBan p WHERE p.TenNhaXuatBan = v.Name);

INSERT dbo.TacGia (TenTacGia, TieuSu)
SELECT v.Name, N''
FROM (VALUES
    (N'Nguyễn Nhật Ánh'),
    (N'Tô Hoài'),
    (N'José Mauro de Vasconcelos'),
    (N'Paulo Coelho'),
    (N'Dale Carnegie'),
    (N'Rosie Nguyễn'),
    (N'James Clear'),
    (N'Yuval Noah Harari'),
    (N'J. K. Rowling'),
    (N'Antoine de Saint-Exupéry')
) v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TacGia a WHERE a.TenTacGia = v.Name);

DECLARE @Books TABLE
(
    MaSach nvarchar(30) NOT NULL PRIMARY KEY,
    TenSach nvarchar(250) NOT NULL,
    MoTa nvarchar(max) NOT NULL,
    ISBN nvarchar(20) NULL,
    CoverImagePath nvarchar(260) NOT NULL,
    MaLoai nvarchar(20) NOT NULL,
    NhaXuatBan nvarchar(180) NOT NULL,
    TacGia nvarchar(180) NOT NULL
);

INSERT @Books (MaSach, TenSach, MoTa, ISBN, CoverImagePath, MaLoai, NhaXuatBan, TacGia)
VALUES
 (N'DEMO-S001',N'Mắt biếc',N'Tình yêu đơn phương của Ngạn dành cho Hà Lan kéo dài từ những năm tháng tuổi thơ ở làng Đo Đo đến khi cả hai trưởng thành. Câu chuyện gợi lại ký ức, tình bạn và những lựa chọn không thể quay ngược.',N'9786041200333',N'/images/books/DEMO-S001.jpg',N'DEMO-VP',N'NXB Trẻ',N'Nguyễn Nhật Ánh'),
 (N'DEMO-S002',N'Cô gái đến từ hôm qua',N'Thư nhớ lại những trò nghịch ngợm với cô bạn Tiểu Li thời thơ ấu, trong khi ở hiện tại cậu lại lúng túng trước Việt An. Hai mạch thời gian đan xen tạo nên câu chuyện về tuổi học trò và tình bạn.',N'9786041004825',N'/images/books/DEMO-S002.jpg',N'DEMO-VP',N'NXB Trẻ',N'Nguyễn Nhật Ánh'),
 (N'DEMO-S003',N'Tôi thấy hoa vàng trên cỏ xanh',N'Qua lời kể của Thiều, cuốn truyện đưa người đọc về một ngôi làng miền Trung với tình anh em, tình bạn và những biến cố trong tuổi thơ của Thiều, Tường cùng những người bạn.',N'9786041218543',N'/images/books/DEMO-S003.jpg',N'DEMO-VP',N'NXB Trẻ',N'Nguyễn Nhật Ánh'),
 (N'DEMO-S004',N'Dế mèn phiêu lưu ký',N'Dế Mèn rời khỏi nơi ở quen thuộc để bước vào những chuyến phiêu lưu. Trên hành trình ấy, chú học về tình bạn, lòng dũng cảm và trách nhiệm với những sinh vật xung quanh.',N'9786042030540',N'/images/books/DEMO-S004.jpg',N'DEMO-TH',N'NXB Kim Đồng',N'Tô Hoài'),
 (N'DEMO-S005',N'Cây cam ngọt của tôi',N'Zezé là cậu bé giàu trí tưởng tượng sống trong một gia đình nghèo ở Brazil. Cây cam nhỏ trong vườn trở thành người bạn để em gửi gắm những suy nghĩ và cảm xúc của mình.',NULL,N'/images/books/DEMO-S005.jpg',N'DEMO-VP',N'NXB Hội Nhà Văn',N'José Mauro de Vasconcelos'),
 (N'DEMO-S006',N'Nhà giả kim',N'Santiago, một chàng chăn cừu trẻ, lên đường tìm kho báu sau một giấc mơ. Chuyến đi qua sa mạc đưa cậu đến với những cuộc gặp gỡ và suy ngẫm về ước mơ, lựa chọn và hành trình riêng của mỗi người.',NULL,N'/images/books/DEMO-S006.jpg',N'DEMO-VP',N'NXB Hội Nhà Văn',N'Paulo Coelho'),
 (N'DEMO-S007',N'Đắc nhân tâm',N'Cuốn sách tập hợp các nguyên tắc giao tiếp và ứng xử, khuyến khích lắng nghe, tôn trọng người đối diện và xây dựng quan hệ hợp tác trong công việc cũng như đời sống.',NULL,N'/images/books/DEMO-S007.jpg',N'DEMO-KN',N'NXB Tổng hợp Thành phố Hồ Chí Minh',N'Dale Carnegie'),
 (N'DEMO-S008',N'Tuổi trẻ đáng giá bao nhiêu?',N'Rosie Nguyễn chia sẻ trải nghiệm và gợi ý cho người trẻ về học tập, công việc và hành trình khám phá thế giới, đồng thời khuyến khích mỗi người chủ động lựa chọn cách sống phù hợp với mình.',NULL,N'/images/books/DEMO-S008.jpg',N'DEMO-KN',N'NXB Hội Nhà Văn',N'Rosie Nguyễn'),
 (N'DEMO-S009',N'Atomic Habits - Thay đổi tí hon, hiệu quả bất ngờ',N'James Clear trình bày cách những thay đổi nhỏ lặp lại mỗi ngày có thể tạo nên kết quả dài hạn. Sách gợi ý cách xây dựng thói quen tốt và điều chỉnh môi trường để duy trì hành vi mong muốn.',NULL,N'/images/books/DEMO-S009.jpg',N'DEMO-KN',N'NXB Thế Giới',N'James Clear'),
 (N'DEMO-S010',N'Sapiens: Lược sử loài người',N'Yuval Noah Harari nhìn lại lịch sử loài người qua các bước ngoặt lớn, từ sự hình thành xã hội đến những thay đổi trong nông nghiệp, khoa học và cách con người tổ chức đời sống.',N'9786044843308',N'/images/books/DEMO-S010.jpg',N'DEMO-LS',N'NXB Tri Thức',N'Yuval Noah Harari'),
 (N'DEMO-S011',N'Harry Potter và hòn đá phù thủy',N'Khi nhận được thư mời đến Hogwarts, Harry Potter khám phá ra nguồn gốc phép thuật của mình và bước vào một năm học mở ra bí mật về hòn đá phù thủy.',N'9786041219205',N'/images/books/DEMO-S011.jpg',N'DEMO-TH',N'NXB Trẻ',N'J. K. Rowling'),
 (N'DEMO-S012',N'Hoàng tử bé',N'Cuộc gặp gỡ giữa một phi công mắc kẹt ở sa mạc và cậu bé đến từ tiểu hành tinh xa xôi mở ra những câu chuyện về tình bạn, sự gắn bó và cách nhìn thế giới bằng sự hồn nhiên.',NULL,N'/images/books/DEMO-S012.jpg',N'DEMO-TH',N'NXB Hội Nhà Văn',N'Antoine de Saint-Exupéry');

UPDATE s
SET s.TenSach = b.TenSach,
    s.MoTa = b.MoTa,
    s.ISBN = b.ISBN,
    s.CoverImagePath = b.CoverImagePath,
    s.MaLoai = b.MaLoai,
    s.IDNhaXuatBan = p.IDNhaXuatBan
FROM dbo.Sach s
JOIN @Books b ON b.MaSach = s.MaSach
JOIN dbo.NhaXuatBan p ON p.TenNhaXuatBan = b.NhaXuatBan;

DELETE dbo.TacGiaSach WHERE MaSach IN (SELECT MaSach FROM @Books);

INSERT dbo.TacGiaSach (MaSach, IDTacGia)
SELECT b.MaSach, a.IDTacGia
FROM @Books b
JOIN dbo.TacGia a ON a.TenTacGia = b.TacGia;

COMMIT TRANSACTION;

SELECT s.MaSach, s.TenSach, s.ISBN, a.TenTacGia, p.TenNhaXuatBan, s.CoverImagePath
FROM dbo.Sach s
JOIN @Books b ON b.MaSach = s.MaSach
LEFT JOIN dbo.TacGiaSach ba ON ba.MaSach = s.MaSach
LEFT JOIN dbo.TacGia a ON a.IDTacGia = ba.IDTacGia
LEFT JOIN dbo.NhaXuatBan p ON p.IDNhaXuatBan = s.IDNhaXuatBan
ORDER BY s.MaSach;

