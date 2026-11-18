# Trang Sách

Dự án gồm ứng dụng WinForms quản lý, website quản lý, ứng dụng WinForms khách hàng, website khách hàng, REST API và SQL Server. Các giao diện web/khách hàng gọi API qua HTTPS; ứng dụng WinForms quản lý nội bộ gọi BUS và DAO trong cùng tiến trình.

## Yêu cầu

- Windows 10/11 và .NET 10 SDK.
- SQL Server đã có sẵn database và schema; API/ứng dụng quản lý chỉ kết nối và kiểm tra cấu trúc cần thiết.
- Ứng dụng khách hàng cần kết nối được tới API server qua HTTPS.

## Chạy ứng dụng

Mở PowerShell tại thư mục repository. Chạy API:

```powershell
dotnet run --project ".\Server\API\TrangSach.API.csproj" --launch-profile https
```

Mở cửa sổ PowerShell thứ hai để chạy ứng dụng khách hàng:

```powershell
dotnet run --project ".\Apps\Customer\GUI\TrangSach.Customer.GUI.csproj"
```

Chạy website khách hàng trong một cửa sổ PowerShell khác:

```powershell
dotnet run --project ".\Apps\Customer\Web\TrangSach.Customer.Web.csproj" --launch-profile TrangSach.Customer.Web
```

Website mở tại `https://localhost:7315`. API phải chạy tại `https://localhost:7215` trước; thay địa chỉ này trong `Apps/Customer/Web/appsettings.json` nếu API được triển khai ở máy chủ khác. Website có danh mục/tìm kiếm sách, chi tiết sản phẩm, đăng ký/đăng nhập, giỏ hàng, thanh toán, hồ sơ cá nhân, xem chi tiết và hủy đơn đang chờ/đã xác nhận. Website giữ token API trong cookie xác thực bảo vệ bằng ASP.NET Core; trình duyệt không truy cập SQL Server.

Chạy ứng dụng quản lý:

```powershell
dotnet run --project ".\Apps\Management\GUI\TrangSach.Management.GUI.csproj"
```

Chạy website quản lý trong một cửa sổ PowerShell khác:

```powershell
dotnet run --project ".\Apps\Management\Web\GUI\TrangSach.Management.Web.GUI.csproj" --launch-profile TrangSach.Management.Web
```

Website quản lý mở tại `https://localhost:7415`; API phải chạy tại `https://localhost:7215`. Website dùng tài khoản nhân viên và ẩn/khóa chức năng theo vai trò như app WinForms. API tiếp tục kiểm tra quyền ở từng endpoint. Website có trang tổng quan và biểu đồ doanh thu 7 ngày, hồ sơ nhân viên, sách (CRUD/ẩn/tìm kiếm nâng cao/nhập xuất CSV và Excel), khách hàng, nhân viên, đơn nhiều mặt hàng, phiếu nhập nhiều sách có thể sửa/đảo, nhà cung cấp, danh mục/tác giả/NXB, khuyến mãi, voucher, báo cáo theo khoảng ngày và xuất CSV/Excel/PDF, cùng nhật ký thao tác. Các thay đổi dữ liệu đều gọi API hiện có; trình duyệt không kết nối SQL Server trực tiếp.

Có thể thay `dotnet run` bằng `dotnet watch run` để theo dõi thay đổi mã nguồn. Một số thay đổi WinForms cần khởi động lại ứng dụng. Nhấn `Ctrl+C` để dừng.

## Cấu trúc

```text
Apps/
  Customer/GUI/Forms/         Các Form khách hàng
  Customer/Web/Pages/         Các trang website khách hàng (Razor Pages)
  Customer/Web/wwwroot/       CSS giao diện website khách hàng
  Customer/BUS/Services/      BUS gọi HTTPS API cho WinForms và website
  Management/GUI/Forms/       Các Form quản lý
  Management/BUS/Services/    Use case quản lý
  Management/BUS/ImportExport/ Nhập/xuất CSV và Excel
  Management/Web/GUI/Pages/   Các trang website quản lý (Razor Pages)
  Management/BUS/Web/         BUS client gọi API quản trị qua HTTPS
  Shared/GUI/Forms/           Form WinForms dùng chung
Server/
  API/                        REST API và HTTP controllers
  BUS/Services/               Nghiệp vụ phía server
  DAO/                        Project DAO của server
    Entities/                 Các kiểu dữ liệu lưu trữ
    Repositories/             Các DAO truy vấn theo dữ liệu
    Infrastructure/           Kết nối, kiểm tra schema và dữ liệu khởi tạo

Shared/
  DTO/{Accounts,Catalog,Commerce,Inventory,People,Promotions,Reports,Audit}/ Kiểu dữ liệu trao đổi theo nghiệp vụ
  Security/                   Quy tắc quyền dùng chung
docs/                         Tài liệu kiến trúc
TrangSach.sln                 Solution tổng
```

`GUI` gọi `BUS`; `BUS` thực hiện nghiệp vụ và gọi `DAO`; `DAO` dùng ADO.NET (`Microsoft.Data.SqlClient`) để đọc/ghi SQL Server. Management BUS xử lý use case nhập/xuất sách; Form quản lý nhận sự kiện chọn file và dựng báo cáo. API là cổng HTTPS tới BUS server, không phải một lớp dữ liệu khác. Ứng dụng khách hàng không tham chiếu trực tiếp DAO.

Tên file mã nguồn trong các thư mục lớp thống nhất theo hậu tố `.GUI.cs`, `.BUS.cs` hoặc `.DAO.cs`.

Xem [kiến trúc](docs/ARCHITECTURE.md) và [bản đồ entity](docs/ENTITY_LAYER_MAP.md).







