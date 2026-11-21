# Trang Sách — Hệ Thống Quản Lý & Cửa Hàng Sách Trực Tuyến

Hệ thống quản lý và bán sách đa nền tảng viết bằng **C# (.NET 10)** theo kiến trúc **3 tầng (3-tier: GUI – BUS – DAO)**, cơ sở dữ liệu **Microsoft SQL Server**.

Hệ thống gồm 5 phân hệ phần mềm hoàn chỉnh:
1. **Server REST API** (`Server/API`): Cung cấp API HTTPS cho toàn bộ ứng dụng khách hàng và website quản lý.
2. **Website Khách hàng** (`Apps/Customer/Web`): Web bán sách trực tuyến (ASP.NET Core Razor Pages), giỏ hàng, thanh toán, quản lý hồ sơ, lịch sử đơn hàng.
3. **Website Quản lý** (`Apps/Management/Web/GUI`): Web quản trị cửa hàng (ASP.NET Core Razor Pages), dashboard doanh thu, quản lý sách/kho/đơn hàng/nhân sự/khuyến mãi.
4. **Ứng dụng Quản lý WinForms** (`Apps/Management/GUI`): Phần mềm Windows Forms cho nhân viên/quản lý nội bộ, in hóa đơn, xuất nhập Excel/CSV, quản trị toàn diện.
5. **Ứng dụng Khách hàng WinForms** (`Apps/Customer/GUI`): Phần mềm Windows Forms mua hàng cho khách.

---

## Mục lục
- [1. Yêu cầu hệ thống (Prerequisites)](#1-yêu-cầu-hệ-thống-prerequisites)
- [2. Cài đặt môi trường từ máy mới](#2-cài-đặt-môi-trường-từ-máy-mới)
- [3. Khởi tạo Cơ sở dữ liệu (Database Setup)](#3-khởi-tạo-cơ-sở-dữ-liệu-database-setup)
- [4. Cấu hình Chuỗi kết nối Database](#4-cấu-hình-chuỗi-kết-nối-database)
- [5. Cài đặt Chứng chỉ HTTPS Development](#5-cài-đặt-chứng-chỉ-https-development)
- [6. Hướng dẫn Khởi chạy Hệ thống](#6-hướng-dẫn-khởi-chạy-hệ-thống)
- [7. Tài khoản Đăng nhập & Dữ liệu Thử nghiệm](#7-tài-khoản-đăng-nhập--dữ-liệu-thử-nghiệm)
- [8. Xử lý Sự cố Thường gặp (Troubleshooting)](#8-xử-lý-sự-cố-thường-gặp-troubleshooting)
- [9. Cấu trúc Thư mục Dự án](#9-cấu-trúc-thư-mục-dự-án)

---

## 1. Yêu cầu hệ thống (Prerequisites)

- **Hệ điều hành:** Windows 10 (bản 64-bit build 19041+) hoặc Windows 11.
- **.NET SDK:** [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Bắt buộc, dự án target `net10.0` và `net10.0-windows`).
- **Cơ sở dữ liệu:** Microsoft SQL Server 2019/2022 (bản Developer, Enterprise, Standard hoặc Express) hoặc LocalDB.
- **Công cụ quản trị CSDL (khuyến nghị):** [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms) hoặc [Azure Data Studio](https://learn.microsoft.com/azure-data-studio/download-azure-data-studio).
- **Git:** [Git for Windows](https://git-scm.com/download/win).

---

## 2. Cài đặt môi trường từ máy mới

Nếu bạn vừa cài lại Windows hoặc ngồi trước một máy tính hoàn toàn mới, hãy thực hiện lần lượt các bước sau:

### Bước 2.1: Cài đặt .NET 10 SDK
1. Tải và cài đặt [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Mở cửa sổ **PowerShell** hoặc **Command Prompt** mới, kiểm tra:
   ```powershell
   dotnet --version
   ```
   *Kết quả hiển thị phiên bản bắt đầu bằng `10.0.xxx` là thành công.*

### Bước 2.2: Cài đặt SQL Server & SSMS
1. Tải và cài đặt [SQL Server 2022 Express](https://www.microsoft.com/sql-server/sql-server-downloads) (chọn kiểu cài đặt **Basic**).
2. Tải và cài đặt công cụ [SSMS (SQL Server Management Studio)](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms).
3. Đảm bảo dịch vụ SQL Server đang chạy (mặc định instance name là `localhost`, `.` hoặc `.\SQLEXPRESS`).

---

## 3. Khởi tạo Cơ sở dữ liệu (Database Setup)

Để tạo toàn bộ bảng, quan hệ và nạp dữ liệu mẫu ban đầu:

1. Mở **SQL Server Management Studio (SSMS)** và kết nối tới SQL Server của bạn.
2. Mở và thực thi lần lượt 4 file SQL trong thư mục [docs/](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/docs):

| Thứ tự | File SQL | Mô tả chức năng |
|:---:|---|---|
| **1** | [docs/InitDatabase.sql](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/docs/InitDatabase.sql) | Tạo CSDL `TrangSachDb`, 21 bảng, toàn bộ khóa chính/ngoại, chỉ mục và vai trò hệ thống. |
| **2** | [docs/SampleData.sql](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/docs/SampleData.sql) | Dữ liệu mẫu ban đầu: thể loại, NXB, tác giả, nhân viên, khách hàng, nhà cung cấp, sách, đơn hàng, phiếu nhập, khuyến mãi, voucher. |
| **3** | [docs/RealBookCatalog.sql](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/docs/RealBookCatalog.sql) | Bổ sung danh mục sách thực tế tiếng Việt, tác giả và hình ảnh bìa đầy đủ. |
| **4** | [docs/ReportChartSampleData.sql](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/docs/ReportChartSampleData.sql) | Dữ liệu mẫu đơn hàng theo thời gian thực tế để hiển thị các biểu đồ doanh thu và báo cáo. |

> [!TIP]
> Bạn có thể chạy nhanh qua PowerShell bằng lệnh `sqlcmd` (nếu đã cài đặt Command Line Utilities):
> ```powershell
> sqlcmd -S localhost -E -i "docs/InitDatabase.sql"
> sqlcmd -S localhost -E -i "docs/SampleData.sql"
> sqlcmd -S localhost -E -i "docs/RealBookCatalog.sql"
> sqlcmd -S localhost -E -i "docs/ReportChartSampleData.sql"
> ```

---

## 4. Cấu hình Chuỗi kết nối Database

Mở file mã nguồn:
[Server/DAO/Infrastructure/DbConnection.DAO.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/DAO/Infrastructure/DbConnection.DAO.cs)

Tìm đến dòng 11 và chỉnh sửa giá trị `ConnectionString` cho phù hợp với máy tính của bạn:

```csharp
// Đối với SQL Server dùng Windows Authentication (khuyên dùng cho máy cá nhân):
public const string ConnectionString = "Server=localhost;Database=TrangSachDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=15";

// Hoặc nếu bạn cài SQL Server Express:
// public const string ConnectionString = @"Server=.\SQLEXPRESS;Database=TrangSachDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=15";

// Hoặc nếu dùng tài khoản sa:
// public const string ConnectionString = "Server=localhost;Database=TrangSachDb;User Id=sa;Password=MatKhauCuaBan;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=15";
```

---

## 5. Cài đặt Chứng chỉ HTTPS Development

Vì các ứng dụng Web và GUI giao tiếp qua HTTPS (`https://localhost:7215`), máy tính cần tin cậy chứng chỉ tự ký của .NET để không bị lỗi kết nối hoặc cảnh báo bảo mật trên trình duyệt:

Mở **PowerShell** và chạy:
```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```
*(Nếu hệ thống hỏi cửa sổ cảnh báo "Do you wish to install this certificate?", bấm **Yes**).*

---

## 6. Hướng dẫn Khởi chạy Hệ thống

### Thứ tự khởi chạy chuẩn:
1. **BẮT BUỘC:** Chạy **Server REST API** trước để mở cổng dịch vụ `https://localhost:7215`.
2. Sau khi API đã chạy, bạn có thể khởi chạy bất kỳ ứng dụng nào: Website Khách hàng, Website Quản lý, WinForms Quản lý hoặc WinForms Khách hàng.

---

### Phân hệ 1: Server REST API (Bắt buộc chạy trước)
Mở cửa sổ PowerShell tại thư mục gốc của dự án:
```powershell
dotnet run --project ".\Server\API\TrangSach.API.csproj" --launch-profile https
```
- **Địa chỉ API:** `https://localhost:7215`
- **Tài liệu Swagger UI:** [https://localhost:7215/swagger](https://localhost:7215/swagger)

---

### Phân hệ 2: Website Khách hàng (Razor Pages)
Mở cửa sổ PowerShell thứ hai:
```powershell
dotnet run --project ".\Apps\Customer\Web\TrangSach.Customer.Web.csproj" --launch-profile TrangSach.Customer.Web
```
- **Địa chỉ truy cập trình duyệt:** [https://localhost:7315](https://localhost:7315)
- **Tính năng:** Xem danh mục sách, tìm kiếm sách theo từ khóa/thể loại, giỏ hàng, đặt hàng thanh toán (tự động điền SĐT & địa chỉ giao hàng), trang hồ sơ khách hàng, tra cứu lịch sử đơn hàng và hủy đơn hàng đang chờ xác nhận.

---

### Phân hệ 3: Website Quản lý (Razor Pages)
Mở cửa sổ PowerShell thứ ba:
```powershell
dotnet run --project ".\Apps\Management\Web\GUI\TrangSach.Management.Web.GUI.csproj" --launch-profile TrangSach.Management.Web
```
- **Địa chỉ truy cập trình duyệt:** [https://localhost:7415](https://localhost:7415)
- **Tính năng:** Đăng nhập phân quyền theo vai trò (Manager, Sales, Inventory, Catalog, Marketing, Accountant), Dashboard doanh thu 7 ngày, quản lý sách, khách hàng, nhân viên, đơn hàng, phiếu nhập kho, nhà cung cấp, thể loại, tác giả, NXB, khuyến mãi, voucher, báo cáo doanh thu và nhật ký hệ thống.

---

### Phân hệ 4: Ứng dụng Quản lý WinForms
Mở cửa sổ PowerShell thứ tư (hoặc chạy trực tiếp trên Windows):
```powershell
dotnet run --project ".\Apps\Management\GUI\TrangSach.Management.GUI.csproj"
```
- **Đặc điểm:** Chạy trực tiếp kết nối BUS/DAO nội bộ, giao diện WinForms hiện đại chống giật nháy, hỗ trợ in hóa đơn thanh toán, xem trước hóa đơn, master-detail nhiều tác giả cho một sách, master-detail đơn hàng và phiếu nhập kho, xuất/nhập danh mục sách qua Excel/CSV.

---

### Phân hệ 5: Ứng dụng Khách hàng WinForms
Mở cửa sổ PowerShell thứ năm:
```powershell
dotnet run --project ".\Apps\Customer\GUI\TrangSach.Customer.GUI.csproj"
```
- **Đặc điểm:** Ứng dụng mua sách dạng Desktop WinForms dành cho khách hàng, kết nối tới API server qua HTTPS.

---

## 7. Tài khoản Đăng nhập & Dữ liệu Thử nghiệm

### 7.1. Tài khoản Quản lý đầu tiên (Setup First Manager)
Nếu cơ sở dữ liệu chưa có tài khoản Quản lý nào:
- **Cách 1 (Trên Web Quản lý):** Truy cập [https://localhost:7415/Account/SetupManager](https://localhost:7415/Account/SetupManager) để tạo tài khoản Quản lý đầu tiên.
- **Cách 2 (Trên App Quản lý WinForms):** Tại màn hình đăng nhập WinForms, nhấn vào liên kết **"Tạo tài khoản quản lý đầu tiên"** ở góc dưới.
- **Cách 3 (Qua Swagger API):** Gọi endpoint `POST /api/auth/setup-manager` trên [https://localhost:7215/swagger](https://localhost:7215/swagger).

### 7.2. Tài khoản Khách hàng
- Bạn có thể nhấn vào nút **Đăng ký** trực tiếp trên Website Khách hàng [https://localhost:7315/Account/Register](https://localhost:7315/Account/Register) để tạo ngay một tài khoản mới và đăng nhập mua sách.
- Khách hàng mẫu có sẵn trong database:
  - Mã khách hàng: `KH-DEMO-01` đến `KH-DEMO-08` (Đã có sẵn địa chỉ nhận hàng và số điện thoại mẫu để thử nghiệm tính năng tự động điền form).

### 7.3. Mã Voucher giảm giá mẫu
Tại trang Giỏ hàng và Thanh toán, bạn có thể áp dụng các mã voucher mẫu sau:
- `DEMO10`: Giảm giá 10%
- `DEMO50K`: Giảm trực tiếp 50.000 ₫
- `CHAOMUNG`: Giảm giá cho khách hàng mới

---

## 8. Xử lý Sự cố Thường gặp (Troubleshooting)

### Lỗi 1: `MSB3021 / MSB3027: Unable to copy file ... because it is being used by another process`
- **Nguyên nhân:** Một tiến trình của ứng dụng (`TrangSach.API`, `TrangSach.Customer.Web` hoặc `TrangSach.Management.GUI`) đang chạy ngầm hoặc chưa thoát hẳn, khóa file `.dll` trong thư mục `bin/`.
- **Cách khắc phục:** Chạy lệnh PowerShell sau để tắt toàn bộ các tiến trình đang chạy của dự án trước khi build lại:
  ```powershell
  Stop-Process -Name "TrangSach.API", "TrangSach.Customer.Web", "TrangSach.Management.Web.GUI", "TrangSach.Management.GUI", "TrangSach.Customer.GUI" -Force -ErrorAction SilentlyContinue
  ```

### Lỗi 2: Trình duyệt báo lỗi kết nối hoặc "Your connection is not private" (Lỗi SSL/HTTPS)
- **Nguyên nhân:** Máy mới chưa tin cậy chứng chỉ HTTPS phát triển cục bộ của ASP.NET Core.
- **Cách khắc phục:**
  ```powershell
  dotnet dev-certs https --clean
  dotnet dev-certs https --trust
  ```

### Lỗi 3: `SqlException: Cannot open database "TrangSachDb" / Login failed for user`
- **Nguyên nhân:** Bạn chưa chạy script tạo database hoặc tên server / thông tin đăng nhập trong `DbConnection.DAO.cs` không khớp với SQL Server trên máy bạn.
- **Cách khắc phục:**
  1. Kiểm tra lại [Mục 3](#3-khởi-tạo-cơ-sở-dữ-liệu-database-setup) xem đã chạy file `docs/InitDatabase.sql` chưa.
  2. Mở file [Server/DAO/Infrastructure/DbConnection.DAO.cs](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/Server/DAO/Infrastructure/DbConnection.DAO.cs) và cập nhật `Server=...` đúng với tên SQL Server của bạn.

---

## 9. Cấu trúc Thư mục Dự án

```text
├── Apps/
│   ├── Customer/
│   │   ├── BUS/                 # Business service client gọi HTTPS API cho Khách hàng
│   │   ├── GUI/                 # Giao diện WinForms cho Khách hàng
│   │   └── Web/                 # Website bán sách trực tuyến (ASP.NET Core Razor Pages)
│   ├── Management/
│   │   ├── BUS/                 # Use case quản lý, xử lý Excel/CSV và gọi API
│   │   ├── GUI/                 # Giao diện WinForms Quản trị cửa hàng
│   │   └── Web/                 # Website quản trị phân quyền (ASP.NET Core Razor Pages)
│   └── Shared/GUI/              # Thành phần giao diện dùng chung (Dialog, Controls)
├── Server/
│   ├── API/                     # ASP.NET Core Web API (Controllers, Authentication, JWT)
│   ├── BUS/Services/            # Nghiệp vụ lõi (Catalog, Commerce, Inventory, Records, Reports)
│   └── DAO/                     # Data Access Object (ADO.NET, Repositories, DbConnection)
├── Shared/
│   ├── DTO/                     # Data Transfer Objects trao đổi giữa các tầng
│   └── Security/                # Định nghĩa Vai trò, Quyền hạn (StaffRoles, Permissions)
├── docs/                        # Tài liệu kiến trúc và toàn bộ script SQL
└── TrangSach.sln                # Solution tổng cho Visual Studio / VS Code / Rider
```

Xem thêm tài liệu chi tiết:
- [Kiến trúc hệ thống](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/docs/ARCHITECTURE.md)
- [Ánh xạ Entity theo từng tầng](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/docs/ENTITY_LAYER_MAP.md)
- [Hướng dẫn chi tiết từng file](file:///c:/Users/duong/Downloads/Quản%20lý%20của%20hàng%20sách%20(C%23)/docs/FILE_GUIDE.md)
