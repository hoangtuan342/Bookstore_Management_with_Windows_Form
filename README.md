# Trang Sách — Windows Forms

Hai ứng dụng Windows Forms (.NET 10) kết nối trực tiếp tới SQL Server. Repo này tự chứa entities, migrations, dữ liệu mẫu và lớp nghiệp vụ cần thiết trong `Vendor/src`; không phụ thuộc mã nguồn dự án Web/Mobile và không cần chạy API riêng.

## Yêu cầu

- Windows 10/11 và .NET 10 SDK.
- SQL Server đang chạy; tài khoản Windows chạy ứng dụng có quyền tạo/cập nhật schema và đọc/ghi database.
- Một instance SQL Server có thể truy cập từ máy đang chạy ứng dụng.

## Lấy mã nguồn trên GitHub

Chỉ cần clone repository Windows Forms:

```powershell
git clone https://github.com/hoangtuan342/Bookstore_Management_with_Windows_Form.git "Quản lý của hàng sách (C#)"
cd "Quản lý của hàng sách (C#)"
```

Sau đó mở PowerShell tại thư mục Windows Forms và chạy các lệnh ở phần bên dưới. Nếu SQL Server trên máy dùng cấu hình khác, mở ứng dụng, chọn **Cơ sở dữ liệu**, nhập connection string rồi khởi động lại. Mã nguồn Web/Mobile không cần được cài đặt hoặc clone.

## Chạy ứng dụng

```powershell
dotnet run --project ".\CustomerApp\TrangSach.Customer.csproj"
dotnet run --project ".\ManagementApp\TrangSach.Management.csproj"
```

Mỗi lệnh mở ứng dụng riêng. Mặc định ứng dụng thử `(localdb)\MSSQLLocalDB` với database `TrangSachDb`. Nút **Cơ sở dữ liệu** cho phép lưu connection string riêng tại `%LOCALAPPDATA%\TrangSach\database-connection.txt`; khởi động lại ứng dụng để áp dụng. Khi mở, WinForms dùng migrations và dữ liệu khởi tạo nằm trong repo này.

## Phạm vi

- `CustomerApp`: đăng nhập/đăng ký khách, xem sách, giỏ hàng, checkout, theo dõi đơn.
- `ManagementApp`: đăng nhập nhân viên, dashboard, CRUD sách/khách hàng/nhân viên/đơn hàng/nhập hàng/nhà cung cấp/danh mục/khuyến mãi, báo cáo và nhật ký theo vai trò.
- Hai ứng dụng WinForms đọc/ghi cùng database `TrangSachDb` theo connection string đã cấu hình. Chỉ database được chia sẻ với Web/Mobile; hai phần mềm có source riêng.
- Mã OOP: interface `IPaymentOption` được lớp trừu tượng `CheckoutPaymentOption` triển khai; các kiểu thanh toán cụ thể ghi đè hành vi và được xử lý qua interface. Có `BookCsvFileService`, danh sách đối tượng, đọc/ghi CSV, cùng thuộc tính và phương thức `static`.

Không lưu mật khẩu SQL hoặc thông tin đăng nhập trong thư mục dự án. Quyền SQL của từng Windows user cần được cấp trên SQL Server.
