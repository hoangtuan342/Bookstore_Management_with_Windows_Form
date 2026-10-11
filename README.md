# Trang Sách — Windows Forms

Hai ứng dụng Windows Forms độc lập (.NET 10) cho cùng hệ thống quản lý cửa hàng sách. WinForms dùng chung các entity, migrations, dữ liệu mẫu và lớp nghiệp vụ database của dự án gốc, đồng thời kết nối trực tiếp SQL Server qua `BookStoreDbContext`. Không cần chạy tiến trình API riêng để mở ứng dụng WinForms. Web/Mobile vẫn sử dụng API như trước.

## Yêu cầu

- Windows 10/11 và .NET 10 SDK.
- SQL Server đang chạy; tài khoản Windows chạy ứng dụng có quyền tạo/cập nhật schema và đọc/ghi database.
- Dự án gốc được đặt cạnh thư mục này với tên `Quản lý cửa hàng sách`.

## Lấy mã nguồn trên GitHub

Hai dự án nằm trong hai repository riêng. Hãy clone chúng cạnh nhau trong cùng một thư mục:

```powershell
git clone https://github.com/hoangtuan342/BookStore_Management.git "Quản lý cửa hàng sách"
git clone https://github.com/hoangtuan342/Bookstore_Management_with_Windows_Form.git "Quản lý của hàng sách (C#)"
cd "Quản lý của hàng sách (C#)"
```

Sau đó mở PowerShell tại thư mục Windows Forms và chạy các lệnh ở phần bên dưới. Nếu SQL Server trên máy dùng cấu hình khác, mở ứng dụng, chọn **Cơ sở dữ liệu**, nhập connection string rồi khởi động lại.

## Chạy ứng dụng

```powershell
dotnet run --project ".\CustomerApp\TrangSach.Customer.csproj"
dotnet run --project ".\ManagementApp\TrangSach.Management.csproj"
```

Mỗi lệnh mở ứng dụng riêng. Mặc định connection string được đọc từ `src/BookStore.Api/appsettings.json` của dự án gốc. Nút **Cơ sở dữ liệu** cho phép lưu connection string riêng tại `%LOCALAPPDATA%\TrangSach\database-connection.txt`; khởi động lại ứng dụng để áp dụng. Khi mở, WinForms dùng cùng `BookStoreDbInitializer` để áp dụng migrations và dữ liệu khởi tạo.

## Phạm vi

- `CustomerApp`: đăng nhập/đăng ký khách, xem sách, giỏ hàng, checkout, theo dõi đơn.
- `ManagementApp`: đăng nhập nhân viên, dashboard, CRUD sách/khách hàng/nhân viên/đơn hàng/nhập hàng/nhà cung cấp/danh mục/khuyến mãi, báo cáo và nhật ký theo vai trò.
- Cả hai ứng dụng đọc/ghi cùng database `TrangSachDb` theo connection string đã cấu hình.
- Mã OOP: interface `IPaymentOption` được lớp trừu tượng `CheckoutPaymentOption` triển khai; các kiểu thanh toán cụ thể ghi đè hành vi và được xử lý qua interface. Có `BookCsvFileService`, danh sách đối tượng, đọc/ghi CSV, cùng thuộc tính và phương thức `static`.

Không lưu mật khẩu SQL hoặc thông tin đăng nhập trong thư mục dự án. Quyền SQL của từng Windows user cần được cấp trên SQL Server.
