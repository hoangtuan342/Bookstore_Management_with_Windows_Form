# Trang Sách — Windows Forms

Dự án gồm ứng dụng khách hàng và ứng dụng quản lý viết bằng WinForms, REST API và SQL Server. Khách hàng truy cập dữ liệu qua HTTPS API; ứng dụng quản lý nội bộ gọi BUS và DAO trực tiếp trong cùng tiến trình.

## Yêu cầu

- Windows 10/11 và .NET 10 SDK.
- SQL Server được cấu hình để API hoặc ứng dụng quản lý nội bộ truy cập.
- Ứng dụng khách hàng cần API server đang chạy và có thể truy cập qua HTTPS.

## Chạy ứng dụng

Mở PowerShell tại thư mục gốc repository. Chạy API:

```powershell
dotnet run --project ".\API\BookStore.Api\BookStore.Api.csproj" --launch-profile https
```

Mở cửa sổ PowerShell thứ hai để chạy ứng dụng khách hàng:

```powershell
dotnet run --project ".\GUI\CustomerApp\TrangSach.Customer.csproj"
```

Chạy ứng dụng quản lý nội bộ:

```powershell
dotnet run --project ".\GUI\ManagementApp\TrangSach.Management.csproj"
```

Để theo dõi thay đổi mã nguồn khi phát triển, thay `dotnet run` bằng `dotnet watch run` cho project cần chạy. Những thay đổi WinForms hoặc thay đổi không hỗ trợ hot reload có thể yêu cầu khởi động lại ứng dụng. Nhấn `Ctrl+C` trong cửa sổ PowerShell để dừng.

API phát triển mặc định dùng `https://localhost:7215/`, cấu hình tại `GUI/CustomerApp/appsettings.json`. Khi triển khai, cấu hình URL HTTPS của API server và chứng thư TLS hợp lệ. Chỉ máy chủ API cần connection string SQL Server; không mở SQL Server trực tiếp ra Internet và không phân phối connection string database cùng ứng dụng khách hàng.

## Cấu trúc dự án

- `GUI/CustomerApp`: WinForms khách hàng, form, view và `CustomerApiGateway`.
- `GUI/ManagementApp`: WinForms quản lý và composition root gọi BUS.
- `GUI/Common`: control và tiện ích WinForms dùng chung.
- `GUI/BlazorShared`: giao diện Blazor và HTTP clients dùng API.
- `BUS`: service và quy tắc nghiệp vụ.
- `DAO`: truy cập dữ liệu qua DAO.
- `API/BookStore.Api`: REST API host và HTTP controllers.
- `Contracts/BookStore.Contracts`: DTO chia sẻ giữa client và server.
- `Domain/BookStore.Domain`: entity nghiệp vụ.
- `Infrastructure/BookStore.Infrastructure`: EF Core, migrations và database context.
- `docs`: tài liệu kiến trúc và bản đồ entity.
- `TrangSach.sln`: solution tổng để mở/build toàn bộ project.

Xem [kiến trúc](docs/ARCHITECTURE.md) và [bản đồ entity theo lớp](docs/ENTITY_LAYER_MAP.md).
