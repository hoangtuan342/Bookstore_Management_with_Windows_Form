# Trang Sách — Windows Forms

Dự án gồm App quản lý, App khách hàng, REST API và SQL Server. Khách hàng gọi API qua HTTPS; App quản lý nội bộ dùng BUS quản lý và BUS/DAO phía server trong cùng tiến trình.

## Yêu cầu

- Windows 10/11 và .NET 10 SDK.
- SQL Server được cấu hình để API hoặc App quản lý nội bộ truy cập.
- App khách hàng cần API server đang chạy và có thể truy cập qua HTTPS.

## Chạy ứng dụng

Mở PowerShell tại thư mục gốc repository. Chạy API:

```powershell
dotnet run --project ".\Server\API\BookStore.Api\BookStore.Api.csproj" --launch-profile https
```

Mở cửa sổ PowerShell thứ hai để chạy App khách hàng:

```powershell
dotnet run --project ".\Apps\Customer\GUI\TrangSach.Customer.csproj"
```

Chạy App quản lý nội bộ:

```powershell
dotnet run --project ".\Apps\Management\GUI\TrangSach.Management.csproj"
```

Để theo dõi thay đổi mã nguồn, thay `dotnet run` bằng `dotnet watch run` cho project cần chạy. Một số thay đổi WinForms cần khởi động lại ứng dụng. Nhấn `Ctrl+C` để dừng.

API phát triển mặc định dùng `https://localhost:7215/`, cấu hình tại `Apps/Customer/GUI/appsettings.json`. Khi triển khai, cấu hình URL HTTPS của API server và chứng thư TLS hợp lệ. Chỉ server cần connection string SQL Server; không mở SQL Server trực tiếp ra Internet và không phân phối connection string database cùng App khách hàng.

## Cấu trúc dự án

- `Apps/Management/GUI`: WinForms quản lý.
- `Apps/Management/BUS`: điều phối use case và quyền riêng của App quản lý.
- `Apps/Customer/GUI`: WinForms khách hàng.
- `Apps/Customer/BUS`: client BUS gọi REST API qua HTTPS; không kết nối SQL Server.
- `Apps/Shared/GUI`: controls dùng chung và thành phần Blazor.
- `Server/API`: REST API và HTTP controllers.
- `Server/BUS`: nghiệp vụ lõi dùng chung bởi API và App quản lý.
- `Server/DAO`: truy vấn/cập nhật dữ liệu.
- `Server/Domain`, `Server/Infrastructure`: entity, EF Core, DbContext và migrations.
- `Shared/Contracts`: DTO trao đổi giữa client và server.
- `docs`: tài liệu kiến trúc và bản đồ entity.
- `TrangSach.sln`: solution tổng để mở/build toàn bộ project.

Xem [kiến trúc](docs/ARCHITECTURE.md) và [bản đồ entity theo lớp](docs/ENTITY_LAYER_MAP.md).
