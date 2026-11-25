# BẢNG KHOANH VÙNG PHẠM VI MÃ NGUỒN THEO CÁC GÓI CHẤM ĐIỂM (50% - 70% - 100%)

Tài liệu này xác định chi tiết và đầy đủ các file, thư mục mã nguồn và hàm chức năng trong dự án **Trang Sách** tương ứng với từng gói chấm điểm của Giáo viên:
- **Gói 50%:** Viết CRUD của 3 lớp (GUI – BUS – DAO)
- **Gói 70%:** Viết hàm thống kê đơn giản (Dashboard / Số liệu tổng hợp nhanh)
- **Gói 100%:** Viết hàm thống kê chi tiết, phức tạp (Báo cáo đa chiều theo kỳ/ngày, phân tích danh mục, khách hàng, nhân viên, xuất file Excel/CSV/PDF)

---

## TỔNG QUAN KIẾN TRÚC 3 LỚP (3-TIER) TRONG HỆ THỐNG

Dự án tuân thủ nghiêm ngặt mô hình 3 lớp phân tách độc lập:
1. **Lớp Giao diện (GUI):** Nhận tương tác người dùng, hiển thị dữ liệu qua DataGridView / Form controls / Razor Pages, gọi hàm của lớp BUS.
2. **Lớp Nghiệp vụ (BUS - Business Logic Layer):** Kiểm tra tính hợp lệ dữ liệu (validation), xử lý nghiệp vụ, tính toán logic và gọi lớp DAO.
3. **Lớp Truy cập Dữ liệu (DAO - Data Access Object):** Thực thi các câu lệnh SQL thuần (SELECT, INSERT, UPDATE, DELETE, GROUP BY, SUM, COUNT,...) qua ADO.NET (`Microsoft.Data.SqlClient`) với cơ sở dữ liệu SQL Server.

---

## 1. GÓI 50%: VIẾT CRUD CỦA 3 LỚP (GUI - BUS - DAO)

Gói 50% tập trung vào việc thực hiện trọn vẹn các thao tác **Thêm, Xóa, Sửa, Đọc (Create, Read, Update, Delete - CRUD)** qua cả 3 tầng ứng dụng. Dự án hỗ trợ CRUD cho nhiều thực thể cốt lõi, trong đó điển hình nhất là **Sách (Books)**, **Khách hàng (Customers)**, **Nhân viên (Employees)**, **Đơn hàng (Orders)**, và **Phiếu nhập (Receipts)**.

### 1.1. Thực thể mẫu 1: Quản lý Sách (Books CRUD)
- **Lớp GUI (Presentation Layer):**
  - [Apps/Management/GUI/Forms/BooksPageForm.GUI.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/GUI/Forms/BooksPageForm.GUI.cs): Giao diện danh sách sách, hiển thị lưới, master-detail tác giả, nút Thêm, Sửa, Ẩn/Hiện sách.
  - [Apps/Management/GUI/Forms/ManagementForm.Books.GUI.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/GUI/Forms/ManagementForm.Books.GUI.cs): Điều hướng sự kiện nút bấm trên giao diện WinForms, hiển thị hộp thoại nhập liệu và gọi xuống BUS.
  - *(Phần Web tương ứng):* [Apps/Management/Web/GUI/Pages/Admin/Books/](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/Web/GUI/Pages/Admin/Books) (`Index.cshtml`, `Create.cshtml`, `Edit.cshtml`).
- **Lớp BUS (Business Logic Layer):**
  - [Server/BUS/Services/AdminBookService.BUS.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/BUS/Services/AdminBookService.BUS.cs):
    - `CreateBook(CreateManagerBookDto dto)`: Kiểm tra ràng buộc hợp lệ của mã sách, tên sách, đơn giá, sau đó gọi DAO thêm mới.
    - `UpdateBook(string id, BookManagementCommandDto dto)`: Kiểm tra tính tồn tại và cập nhật thông tin sách qua DAO.
    - `SetBookVisibility(string id, bool active)`: Ẩn hoặc kích hoạt hiển thị sách.
  - [Server/BUS/Services/BookCatalogService.BUS.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/BUS/Services/BookCatalogService.BUS.cs): Tìm kiếm, lọc và phân trang danh mục sách.
- **Lớp DAO (Data Access Object):**
  - [Server/DAO/Repositories/CatalogDao.DAO.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/DAO/Repositories/CatalogDao.DAO.cs):
    - `GetBooks(...)`: Thực thi SQL `SELECT ... FROM Sach` kết hợp `LEFT JOIN` với bảng Thể loại (`LoaiSach`), NXB (`NhaXuatBan`), Tác giả (`TacGiaSach`).
    - `InsertBook(...)`: Thực thi câu lệnh SQL `INSERT INTO Sach (...) VALUES (...)`.
    - `UpdateBook(...)`: Thực thi câu lệnh SQL `UPDATE Sach SET ... WHERE MaSach = @id`.
    - `SetBookActive(...)`: Thực thi câu lệnh SQL `UPDATE Sach SET TrangThai = @active WHERE MaSach = @id`.

---

### 1.2. Thực thể mẫu 2: Quản lý Khách hàng (Customers CRUD)
- **Lớp GUI:**
  - [Apps/Management/GUI/Forms/CustomersPageForm.GUI.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/GUI/Forms/CustomersPageForm.GUI.cs): Giao diện bảng danh sách khách hàng, tìm kiếm và nút thao tác CRUD.
  - [Apps/Management/GUI/Forms/ManagementForm.Records.GUI.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/GUI/Forms/ManagementForm.Records.GUI.cs): Mở dialog tạo mới, sửa thông tin (Họ tên, SĐT, Địa chỉ, Ngày sinh), xóa khách hàng.
- **Lớp BUS:**
  - [Server/BUS/Services/AdminRecordsService.BUS.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/BUS/Services/AdminRecordsService.BUS.cs):
    - `GetCustomers()`: Lấy danh sách khách hàng.
    - `CreateCustomer(SaveCustomerDto dto)`: Kiểm tra hợp lệ số điện thoại, định dạng họ tên trước khi ghi nhận.
    - `UpdateCustomer(string id, SaveCustomerDto dto)`: Cập nhật thông tin khách hàng.
    - `DeleteCustomer(string id)`: Kiểm tra ràng buộc khóa ngoại (nếu khách hàng đã có đơn hàng thì không được xóa bừa bãi).
- **Lớp DAO:**
  - [Server/DAO/Repositories/UserDao.DAO.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/DAO/Repositories/UserDao.DAO.cs):
    - `GetCustomers()`: Thực thi SQL `SELECT ... FROM KH kh LEFT JOIN TaiKhoan tk ON kh.IDTaiKhoan = tk.IDTaiKhoan`.
    - `InsertCustomer(...)`: Thực thi SQL `INSERT INTO KH (MaKH, HoTen, NgaySinh, SoDienThoai, DiaChiGiaoHang, IDTaiKhoan) VALUES (...)`.
    - `UpdateCustomer(...)`: Thực thi SQL `UPDATE KH SET HoTen = @name, NgaySinh = @birth, SoDienThoai = @phone, DiaChiGiaoHang = @addr WHERE MaKH = @id`.
    - `DeleteCustomer(...)`: Thực thi SQL `DELETE FROM KH WHERE MaKH = @id`.

---

### 1.3. Thực thể mẫu 3: Quản lý Thể loại, Tác giả, Nhà xuất bản (References CRUD)
- **Lớp GUI:**
  - [Apps/Management/GUI/Forms/AuthorsPageForm.GUI.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/GUI/Forms/AuthorsPageForm.GUI.cs): CRUD Tác giả (thêm mới, sửa tiểu sử, xóa).
  - [Apps/Management/GUI/Forms/ReferenceDataPageForm.GUI.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/GUI/Forms/ReferenceDataPageForm.GUI.cs): Quản lý Thể loại (`LoaiSach`) và Nhà xuất bản (`NhaXuatBan`).
- **Lớp BUS:**
  - [Server/BUS/Services/AdminRecordsService.BUS.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/BUS/Services/AdminRecordsService.BUS.cs): Các hàm `CreateAuthor`, `UpdateAuthor`, `DeleteAuthor`, `CreateCategory`, `CreatePublisher`,...
- **Lớp DAO:**
  - [Server/DAO/Repositories/CatalogDao.DAO.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/DAO/Repositories/CatalogDao.DAO.cs): Các hàm thao tác bảng `TacGia`, `LoaiSach`, `NhaXuatBan`.

---

## 2. GÓI 70%: VIẾT HÀM THỐNG KÊ ĐƠN GIẢN (DASHBOARD & CHỈ SỐ NHANH)

Gói 70% tập trung vào việc tổng hợp số liệu nhanh, các hàm aggregation cơ bản (SUM, COUNT, GROUP BY đơn giản) để trả về các thẻ chỉ số KPI tổng quan (Dashboard overview) và danh sách đơn hàng gần đây.

### 2.1. Lớp GUI (Hiển thị thẻ chỉ số thống kê tổng quan):
- [Apps/Management/GUI/Forms/DashboardPageForm.GUI.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/GUI/Forms/DashboardPageForm.GUI.cs):
  - Vẽ và hiển thị các Card thống kê tổng quan:
    - **Doanh thu tháng này** (Doanh thu các đơn hàng hoàn tất trong tháng).
    - **Số đơn hàng hôm nay** (Tổng số đơn phát sinh trong ngày).
    - **Số đơn đang chờ xử lý** (Đơn hàng ở trạng thái `Pending` / `Submitted`).
    - **Tổng số sách đang kinh doanh** và **Sách sắp hết hàng** (Tồn kho $\le 5$).
    - **Tổng số khách hàng đăng ký**.
  - Bảng danh sách hiển thị các **Đơn hàng mới nhất** vừa phát sinh.
- *(Phần Web tương ứng):* [Apps/Management/Web/GUI/Pages/Admin/Index.cshtml](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/Web/GUI/Pages/Admin/Index.cshtml).

### 2.2. Lớp BUS (Tổng hợp số liệu Dashboard):
- [Server/BUS/Services/AdminDashboardService.BUS.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/BUS/Services/AdminDashboardService.BUS.cs):
  - Hàm `GetSummary()`:
    - Xác định mốc thời gian đầu ngày và đầu tháng (`today`, `monthStart`).
    - Gọi DAO lấy `revenue`, `ordersToday`, `pendingOrders`, `activeBooks`, `lowStockBooks`, `customerCount` và `recentOrders`.
    - Đóng gói toàn bộ vào DTO `ManagerDashboardDto`.

### 2.3. Lớp DAO (Các câu lệnh SQL Aggregation đơn giản):
- [Server/DAO/Repositories/ReportDao.DAO.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/DAO/Repositories/ReportDao.DAO.cs):
  - `GetMonthRevenue(DateTime monthStart)`:
    ```sql
    SELECT ISNULL(SUM(TongTien), 0) FROM HD WHERE TrangThaiDonHang = 'Completed' AND NgayLapHD >= @d
    ```
  - `CountOrdersToday(DateTime todayUtc)`:
    ```sql
    SELECT COUNT(1) FROM HD WHERE NgayLapHD >= @d
    ```
  - `CountOrdersByStatus(string status)`:
    ```sql
    SELECT COUNT(1) FROM HD WHERE TrangThaiDonHang = @s
    ```
  - `CountActiveBooks()`:
    ```sql
    SELECT COUNT(1) FROM Sach WHERE TrangThai = 1
    ```
  - `CountLowStockBooks(int threshold)`:
    ```sql
    SELECT COUNT(1) FROM Sach WHERE TrangThai = 1 AND SLCo <= @t
    ```
  - `CountCustomers()`:
    ```sql
    SELECT COUNT(1) FROM KH
    ```
  - `GetRecentOrders(int take)`:
    ```sql
    SELECT TOP (@take) h.MaHD, h.NgayLapHD, h.TrangThaiDonHang, h.TongTien, ISNULL(kh.HoTen, N'Khách vãng lai')
    FROM HD h LEFT JOIN KH kh ON h.MaKH = kh.MaKH ORDER BY h.NgayLapHD DESC
    ```

---

## 3. GÓI 100%: VIẾT HÀM THỐNG KÊ CHI TIẾT, PHỨC TẠP (REPORTING & ANALYTICS)

Gói 100% bao gồm toàn bộ hệ thống báo cáo chuyên sâu, thống kê đa chiều theo thời gian tùy biến, phân tích tỷ trọng doanh thu/lợi nhuận, hiệu suất bán hàng của nhân viên, phân tích sản phẩm bán chạy, khách hàng tiêu biểu và xuất báo cáo ra các định dạng chuẩn (Excel, CSV, PDF).

### 3.1. Lớp GUI (Trung tâm Báo cáo Doanh thu & Thống kê Chuyên sâu):
- [Apps/Management/GUI/Forms/ReportDashboardForm.GUI.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/GUI/Forms/ReportDashboardForm.GUI.cs):
  - Form báo cáo đồ sộ nhất dự án (~42.000 dòng code UI hoàn chỉnh).
  - Cho phép người dùng lọc báo cáo động theo khoảng ngày (`FromDate` đến `ToDate`) hoặc theo mốc định sẵn: 7 ngày, 30 ngày, Quý này, Năm nay.
  - Vẽ biểu đồ cột doanh thu theo ngày/tháng/năm, so sánh tốc độ tăng trưởng với kỳ trước (Growth rate %).
  - Hiển thị danh sách thống kê chi tiết trên nhiều Grid:
    - **Doanh thu theo ngày** (`RevenueByDay`)
    - **Doanh thu theo tháng / quý / năm** (`MonthlyRevenue`, `QuarterlyRevenue`, `YearlyRevenue`)
    - **Top 10 Sách bán chạy nhất** (`BestSellers` - Số lượng bán & Tổng doanh thu)
    - **Cơ cấu doanh thu theo Thể loại sách** (`CategorySales` - Tỷ trọng đóng góp %)
    - **Khách hàng tiềm năng / Doanh số theo khách hàng** (`CustomerSales`)
    - **Hiệu suất bán hàng theo từng Nhân viên** (`EmployeeSales`)
    - **Định giá và cơ cấu tồn kho** (`InventorySummary`)
  - Tích hợp các nút xuất báo cáo: **Xuất Excel (.xlsx)**, **Xuất CSV (.csv)**, **In báo cáo / Xem trước PDF**.
- [Apps/Management/GUI/Forms/RevenueTrendDetailForm.GUI.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/GUI/Forms/RevenueTrendDetailForm.GUI.cs): Form soi chi tiết xu hướng tăng trưởng doanh thu theo từng mốc thời gian.
- *(Phần Web tương ứng):* [Apps/Management/Web/GUI/Pages/Admin/Reports/](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/Web/GUI/Pages/Admin/Reports) (`Index.cshtml` và `Export.cshtml`).

### 3.2. Lớp BUS (Xử lý Tính toán Nghiệp vụ Thống kê Đa chiều):
- [Server/BUS/Services/AdminReportsService.BUS.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/BUS/Services/AdminReportsService.BUS.cs):
  - Hàm `Get(DateOnly fromDate, DateOnly toDate)`:
    - Tính toán số ngày trong kỳ (hỗ trợ tối đa 10 năm).
    - Quy đổi múi giờ sang UTC để truy vấn dữ liệu chính xác từ database.
    - Tính toán kỳ trước (`previousFromUtc`) để xác định tỷ lệ tăng trưởng doanh thu so với cùng kỳ.
    - Tính toán **Giá vốn ước tính (Cost of Goods Sold)** dựa trên giá nhập trung bình của các phiếu nhập hàng:
      `estimatedCost = orderLines.Sum(line => averageCosts.GetValueOrDefault(line.BookId) * line.Qty)`
    - Tính **Lợi nhuận gộp (Gross Profit)**: `Doanh thu - Giá vốn`.
    - Sử dụng LINQ (`GroupBy`, `Select`, `OrderByDescending`) để gom nhóm và tổng hợp:
      - Thống kê doanh thu theo từng ngày liên tục (kể cả những ngày không có đơn hàng vẫn hiển thị mốc 0đ).
      - Thống kê Top sách bán chạy nhất (`BestSellerDto`).
      - Phân tích cơ cấu doanh số theo Thể loại (`CategorySalesDto`).
      - Xếp hạng đóng góp của khách hàng (`CustomerSalesDto`).
      - Tổng hợp doanh thu theo từng Tháng, Quý, Năm (`MonthlyRevenueDto`, `QuarterlyRevenueDto`, `YearlyRevenueDto`).
      - Đánh giá giá trị tồn kho theo từng thể loại (`CategoryInventorySummaryDto`).
    - Đóng gói toàn bộ vào DTO toàn diện `ManagerReportDto`.
- **Dịch vụ Xuất File Báo cáo (Import / Export BUS):**
  - [Apps/Management/BUS/ImportExport/ExcelWorkbookService.BUS.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/BUS/ImportExport/ExcelWorkbookService.BUS.cs): Dựng bảng tính Excel chuyên nghiệp nhiều Sheet (Tổng quan, Doanh thu ngày, Sách bán chạy, Khách hàng, Thể loại, Tồn kho).
  - [Apps/Management/BUS/ImportExport/BookExcelFileService.BUS.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/BUS/ImportExport/BookExcelFileService.BUS.cs): Đọc / ghi file Excel OpenXML chuẩn.
  - [Apps/Management/BUS/ImportExport/BookCsvFileService.BUS.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Apps/Management/BUS/ImportExport/BookCsvFileService.BUS.cs): Xuất báo cáo sang CSV phân tách theo từng mục tương ứng với Excel.

### 3.3. Lớp DAO (Truy vấn Phức tạp với JOIN nhiều bảng và tính toán Aggregation):
- [Server/DAO/Repositories/ReportDao.DAO.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/DAO/Repositories/ReportDao.DAO.cs):
  - `GetAllOrdersInPeriod(DateTime fromUtc, DateTime untilUtc)`: Truy vấn toàn bộ đơn hàng trong kỳ và tính tổng số lượng món sách bằng Subquery:
    ```sql
    SELECT h.MaHD, h.NgayLapHD, h.TrangThaiDonHang, h.TongTien,
           ISNULL((SELECT SUM(SL) FROM CTHD c WHERE c.MaHD = h.MaHD), 0)
    FROM HD h
    WHERE h.NgayLapHD >= @from AND h.NgayLapHD < @until
    ```
  - `GetCompletedOrdersInPeriod(DateTime fromUtc, DateTime untilUtc)`: Kết hợp bảng `HD` với `KH` (Khách hàng) và `NV` (Nhân viên bán hàng) để lấy chi tiết đơn hàng đã hoàn tất:
    ```sql
    SELECT h.MaHD, h.NgayLapHD, ISNULL(h.MaKH,''), ISNULL(kh.HoTen, N'Khách vãng lai'),
           h.MaNV, ISNULL(nv.HoLot + ' ' + nv.Ten, N''), h.TongTien, h.TongGiamGia
    FROM HD h
    LEFT JOIN KH kh ON h.MaKH = kh.MaKH
    LEFT JOIN NV nv ON h.MaNV = nv.MaNV
    WHERE h.TrangThaiDonHang = 'Completed' AND h.NgayLapHD >= @from AND h.NgayLapHD < @until
    ```
  - `GetOrderLinesForPeriod(DateTime fromUtc, DateTime untilUtc)`: Thực hiện **JOIN 4 bảng** (`CTHD`, `HD`, `Sach`, `LoaiSach`) để bóc tách từng chi tiết dòng hàng của các hóa đơn thành công:
    ```sql
    SELECT c.MaHD, c.MaSach, ISNULL(s.TenSach, c.MaSach),
           ISNULL(ls.TenLoai, N'Chưa phân loại'), c.SL, c.DG, c.ThanhTien
    FROM CTHD c
    JOIN HD h ON c.MaHD = h.MaHD
    LEFT JOIN Sach s ON c.MaSach = s.MaSach
    LEFT JOIN LoaiSach ls ON s.MaLoai = ls.MaLoai
    WHERE h.TrangThaiDonHang = 'Completed' AND h.NgayLapHD >= @from AND h.NgayLapHD < @until
    ```
  - `GetAveragePurchaseCosts()`: Tính giá nhập bình quân của từng cuốn sách từ các phiếu nhập hàng hoàn tất:
    ```sql
    SELECT c.MaSach, AVG(c.DG)
    FROM CTPNH c
    JOIN PNH p ON c.MaPNH = p.MaPNH
    WHERE p.TrangThai = 'Completed'
    GROUP BY c.MaSach
    ```
  - `GetPurchaseSpendInPeriod(DateTime fromUtc, DateTime untilUtc)`: Tổng chi phí nhập hàng trong kỳ từ các phiếu nhập:
    ```sql
    SELECT ISNULL(SUM(c.ThanhTien), 0)
    FROM CTPNH c
    JOIN PNH p ON c.MaPNH = p.MaPNH
    WHERE p.TrangThai = 'Completed' AND p.NgayLap >= @from AND p.NgayLap < @until
    ```
  - `GetActiveInventory()`: Lấy số lượng tồn và giá bán của toàn bộ sách đang kinh doanh.

---

## 4. BẢNG TỔNG HỢP TRA CỨU NHANH THEO GÓI CHẤM

| Gói chấm | Mức độ yêu cầu | Lớp GUI | Lớp BUS | Lớp DAO |
|:---:|---|---|---|---|
| **50%** | **CRUD 3 lớp** | - `BooksPageForm.GUI.cs`<br>- `CustomersPageForm.GUI.cs`<br>- `AuthorsPageForm.GUI.cs`<br>- `ManagementForm.Books.GUI.cs`<br>- `ManagementForm.Records.GUI.cs` | - `AdminBookService.BUS.cs`<br>- `AdminRecordsService.BUS.cs`<br>- `BookCatalogService.BUS.cs` | - `CatalogDao.DAO.cs`<br>- `UserDao.DAO.cs` |
| **70%** | **Thống kê đơn giản**<br>*(Dashboard & Thẻ chỉ số)* | - `DashboardPageForm.GUI.cs`<br>- `Admin/Index.cshtml` (Web) | - `AdminDashboardService.BUS.cs` | - `ReportDao.DAO.cs`<br>*(Các hàm: `GetMonthRevenue`, `CountOrdersToday`, `CountOrdersByStatus`, `CountActiveBooks`, `CountLowStockBooks`, `CountCustomers`, `GetRecentOrders`)* |
| **100%** | **Thống kê chi tiết, phức tạp**<br>*(Báo cáo đa chiều, biểu đồ, lợi nhuận, xuất Excel/CSV)* | - `ReportDashboardForm.GUI.cs`<br>- `RevenueTrendDetailForm.GUI.cs`<br>- `Admin/Reports/Index.cshtml` (Web) | - `AdminReportsService.BUS.cs`<br>- `ExcelWorkbookService.BUS.cs`<br>- `BookExcelFileService.BUS.cs`<br>- `BookCsvFileService.BUS.cs` | - `ReportDao.DAO.cs`<br>*(Các hàm: `GetOrderLinesForPeriod`, `GetCompletedOrdersInPeriod`, `GetAllOrdersInPeriod`, `GetAveragePurchaseCosts`, `GetPurchaseSpendInPeriod`, `GetActiveInventory`)* |
