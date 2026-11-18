/*
   Idempotent report-chart sample data for TrangSachDb.
   Adds 3 demo employees, 24 posted receipts with 48 lines,
   and 50 demo orders with 100 lines across several years and recent days.
   Existing business rows and accounts are not modified; book stock is adjusted
   only for newly inserted receipt/order lines.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT dbo.NV (MaNV, HoLot, Ten, NS, AnhChanDung, DiaChi, TrangThaiTaiKhoan, LuongCB, IDTaiKhoan)
    SELECT v.MaNV, v.HoLot, v.Ten, v.NS, NULL, v.DiaChi, N'Active', v.LuongCB, NULL
    FROM (VALUES
        (N'NV-DEMO-04', N'Phạm Minh', N'Khoa', CONVERT(date,'1992-05-17'), N'Hà Nội', CONVERT(decimal(18,2),12500000)),
        (N'NV-DEMO-05', N'Đỗ Thu', N'Vy', CONVERT(date,'1995-10-09'), N'Đà Nẵng', CONVERT(decimal(18,2),11800000)),
        (N'NV-DEMO-06', N'Vũ Hoàng', N'Phúc', CONVERT(date,'1991-02-24'), N'TP. Hồ Chí Minh', CONVERT(decimal(18,2),13200000))
    ) v(MaNV, HoLot, Ten, NS, DiaChi, LuongCB)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.NV x WHERE x.MaNV = v.MaNV);

    DECLARE @ReceiptSamples TABLE
    (
        Id nvarchar(30) NOT NULL PRIMARY KEY,
        CreatedAt datetime2 NOT NULL,
        EmployeeId nvarchar(20) NOT NULL,
        SupplierId nvarchar(20) NOT NULL,
        Book1 nvarchar(30) NOT NULL,
        Quantity1 int NOT NULL,
        Book2 nvarchar(30) NOT NULL,
        Quantity2 int NOT NULL
    );

    ;WITH N AS
    (
        SELECT TOP (24) CONVERT(int, ROW_NUMBER() OVER (ORDER BY (SELECT NULL))) AS n
        FROM sys.all_objects
    )
    INSERT @ReceiptSamples (Id, CreatedAt, EmployeeId, SupplierId, Book1, Quantity1, Book2, Quantity2)
    SELECT CONCAT(N'DEMO-CHART-PN-', RIGHT(CONCAT('000', n), 3)),
           DATEADD(month, (n - 1) * 2, CONVERT(datetime2, '2022-12-15')),
           CONCAT(N'NV-DEMO-0', CASE n % 6 WHEN 0 THEN 1 WHEN 1 THEN 2 WHEN 2 THEN 3 WHEN 3 THEN 4 WHEN 4 THEN 5 ELSE 6 END),
           CONCAT(N'NCC-DEMO-0', ((n - 1) % 4) + 1),
           CONCAT(N'DEMO-S', RIGHT(CONCAT('000', ((n - 1) % 12) + 1), 3)),
           8 + (n % 5),
           CONCAT(N'DEMO-S', RIGHT(CONCAT('000', ((n + 4) % 12) + 1), 3)),
           5 + ((n + 1) % 6)
    FROM N;

    INSERT dbo.PNH (MaPNH, NgayLap, MaNV, MaNCC, TongTien, TrangThai)
    SELECT r.Id, r.CreatedAt, r.EmployeeId, r.SupplierId,
           SUM(CAST(b.DGBan * CONVERT(decimal(5,2), 0.45) AS decimal(18,2)) * q.Quantity), N'Posted'
    FROM @ReceiptSamples r
    CROSS APPLY (VALUES (r.Book1, r.Quantity1), (r.Book2, r.Quantity2)) q(BookId, Quantity)
    JOIN dbo.Sach b ON b.MaSach = q.BookId
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PNH x WHERE x.MaPNH = r.Id)
    GROUP BY r.Id, r.CreatedAt, r.EmployeeId, r.SupplierId;

    DECLARE @NewPurchases TABLE (BookId nvarchar(30) NOT NULL, Quantity int NOT NULL);
    INSERT dbo.CTPNH (MaPNH, MaSach, SL, DG, ThanhTien)
    OUTPUT inserted.MaSach, inserted.SL INTO @NewPurchases (BookId, Quantity)
    SELECT r.Id, q.BookId, q.Quantity, cost.UnitCost, cost.UnitCost * q.Quantity
    FROM @ReceiptSamples r
    CROSS APPLY (VALUES (r.Book1, r.Quantity1), (r.Book2, r.Quantity2)) q(BookId, Quantity)
    JOIN dbo.Sach b ON b.MaSach = q.BookId
    CROSS APPLY (SELECT CAST(b.DGBan * CONVERT(decimal(5,2), 0.45) AS decimal(18,2)) AS UnitCost) cost
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CTPNH x WHERE x.MaPNH = r.Id AND x.MaSach = q.BookId);

    ;WITH Purchased AS
    (
        SELECT BookId, SUM(Quantity) AS Quantity FROM @NewPurchases GROUP BY BookId
    )
    UPDATE b SET b.SLCo = b.SLCo + p.Quantity
    FROM dbo.Sach b JOIN Purchased p ON p.BookId = b.MaSach;

    DECLARE @OrderSamples TABLE
    (
        Id nvarchar(30) NOT NULL PRIMARY KEY,
        CreatedAt datetime2 NOT NULL,
        EmployeeId nvarchar(20) NOT NULL,
        CustomerId nvarchar(20) NOT NULL,
        OrderStatus nvarchar(30) NOT NULL,
        PaymentStatus nvarchar(30) NOT NULL,
        PaymentMethod nvarchar(30) NOT NULL,
        Book1 nvarchar(30) NOT NULL,
        Quantity1 int NOT NULL,
        Book2 nvarchar(30) NOT NULL,
        Quantity2 int NOT NULL
    );

    ;WITH N AS
    (
        SELECT TOP (50) CONVERT(int, ROW_NUMBER() OVER (ORDER BY (SELECT NULL))) AS n
        FROM sys.all_objects
    ), S AS
    (
        SELECT n,
               CASE WHEN n <= 36
                    THEN DATEADD(month, n - 1, CONVERT(datetime2, '2022-12-15'))
                    ELSE DATEADD(hour, 10, CONVERT(datetime2, DATEADD(day, n - 50, CONVERT(date, GETDATE())))) END AS CreatedAt,
               CONCAT(N'NV-DEMO-0', ((n - 1) % 6) + 1) AS EmployeeId,
               CONCAT(N'KH-DEMO-0', ((n - 1) % 8) + 1) AS CustomerId,
               CASE WHEN n > 36 AND n IN (41, 46) THEN N'Pending'
                    WHEN n > 36 AND n = 44 THEN N'Shipping'
                    WHEN n <= 36 AND n % 12 = 0 THEN N'Cancelled'
                    WHEN n <= 36 AND n % 12 = 1 AND n <> 1 THEN N'Pending'
                    WHEN n <= 36 AND n % 12 = 2 THEN N'Shipping'
                    WHEN n <= 36 AND n % 12 = 3 THEN N'Confirmed'
                    ELSE N'Completed' END AS OrderStatus,
               CONCAT(N'DEMO-S', RIGHT(CONCAT('000', ((n - 1) % 12) + 1), 3)) AS Book1,
               1 + (n % 3) AS Quantity1,
               CONCAT(N'DEMO-S', RIGHT(CONCAT('000', ((n + 4) % 12) + 1), 3)) AS Book2,
               1 + ((n + 1) % 2) AS Quantity2
        FROM N
    )
    INSERT @OrderSamples (Id, CreatedAt, EmployeeId, CustomerId, OrderStatus, PaymentStatus, PaymentMethod, Book1, Quantity1, Book2, Quantity2)
    SELECT CONCAT(N'DEMO-CHART-HD-', RIGHT(CONCAT('000', n), 3)), CreatedAt, EmployeeId, CustomerId, OrderStatus,
           CASE OrderStatus WHEN N'Completed' THEN N'Paid' WHEN N'Shipping' THEN N'Paid'
                            WHEN N'Confirmed' THEN N'AwaitingPayment' WHEN N'Cancelled' THEN N'Cancelled' ELSE N'Unpaid' END,
           CASE WHEN n % 2 = 0 THEN N'BankTransfer' ELSE N'CashOnDelivery' END,
           Book1, Quantity1, Book2, Quantity2
    FROM S;

    INSERT dbo.HD (MaHD, NgayLapHD, MaNV, MaKH, TongTien, TrangThaiDonHang, TrangThaiThanhToan,
                   PhuongThucThanhToan, DiaChiGiaoHang, SDTNhanHang, MaVoucher, TongGiamGia)
    SELECT o.Id, o.CreatedAt, o.EmployeeId, o.CustomerId,
           b1.DGBan * o.Quantity1 + b2.DGBan * o.Quantity2,
           o.OrderStatus, o.PaymentStatus, o.PaymentMethod, c.DiaChiGiaoHang, c.SoDienThoai, NULL, 0
    FROM @OrderSamples o
    JOIN dbo.Sach b1 ON b1.MaSach = o.Book1
    JOIN dbo.Sach b2 ON b2.MaSach = o.Book2
    JOIN dbo.KH c ON c.MaKH = o.CustomerId
    WHERE NOT EXISTS (SELECT 1 FROM dbo.HD x WHERE x.MaHD = o.Id);

    UPDATE h SET h.NgayLapHD = o.CreatedAt, h.TrangThaiDonHang = o.OrderStatus, h.TrangThaiThanhToan = o.PaymentStatus
    FROM dbo.HD h JOIN @OrderSamples o ON o.Id = h.MaHD;

    DECLARE @NewSales TABLE (OrderId nvarchar(30) NOT NULL, BookId nvarchar(30) NOT NULL, Quantity int NOT NULL);
    INSERT dbo.CTHD (MaHD, MaSach, SL, DG, ThanhTien)
    OUTPUT inserted.MaHD, inserted.MaSach, inserted.SL INTO @NewSales (OrderId, BookId, Quantity)
    SELECT o.Id, q.BookId, q.Quantity, b.DGBan, b.DGBan * q.Quantity
    FROM @OrderSamples o
    CROSS APPLY (VALUES (o.Book1, o.Quantity1), (o.Book2, o.Quantity2)) q(BookId, Quantity)
    JOIN dbo.Sach b ON b.MaSach = q.BookId
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CTHD x WHERE x.MaHD = o.Id AND x.MaSach = q.BookId);

    ;WITH Sold AS
    (
        SELECT ns.BookId, SUM(ns.Quantity) AS Quantity
        FROM @NewSales ns JOIN @OrderSamples o ON o.Id = ns.OrderId
        WHERE o.OrderStatus = N'Completed'
        GROUP BY ns.BookId
    )
    UPDATE b SET b.SLCo = CASE WHEN b.SLCo >= s.Quantity THEN b.SLCo - s.Quantity ELSE 0 END
    FROM dbo.Sach b JOIN Sold s ON s.BookId = b.MaSach;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT 'ChartDemoEmployees' AS Dataset, COUNT(*) AS Rows FROM dbo.NV WHERE MaNV LIKE N'NV-DEMO-0%'
UNION ALL SELECT 'ChartDemoOrders', COUNT(*) FROM dbo.HD WHERE MaHD LIKE N'DEMO-CHART-HD-%'
UNION ALL SELECT 'ChartDemoOrderLines', COUNT(*) FROM dbo.CTHD WHERE MaHD LIKE N'DEMO-CHART-HD-%'
UNION ALL SELECT 'ChartDemoReceipts', COUNT(*) FROM dbo.PNH WHERE MaPNH LIKE N'DEMO-CHART-PN-%'
UNION ALL SELECT 'ChartDemoReceiptLines', COUNT(*) FROM dbo.CTPNH WHERE MaPNH LIKE N'DEMO-CHART-PN-%';

SELECT YEAR(NgayLapHD) AS Nam, COUNT(*) AS TongDon,
       SUM(CASE WHEN TrangThaiDonHang = N'Completed' THEN 1 ELSE 0 END) AS DonHoanTat
FROM dbo.HD
WHERE MaHD LIKE N'DEMO-CHART-HD-%'
GROUP BY YEAR(NgayLapHD)
ORDER BY Nam;

SELECT YEAR(NgayLap) AS Nam, COUNT(*) AS SoPhieuNhap
FROM dbo.PNH
WHERE MaPNH LIKE N'DEMO-CHART-PN-%' AND TrangThai = N'Posted'
GROUP BY YEAR(NgayLap)
ORDER BY Nam;
