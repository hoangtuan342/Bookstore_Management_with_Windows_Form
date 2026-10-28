# Kiến trúc dự án Trang Sách

## Cấu trúc thư mục

```text
GUI/
  CustomerApp/       WinForms khách hàng; giao tiếp HTTPS với API
  ManagementApp/     WinForms quản lý; composition root gọi BUS tại chỗ
  Common/            Thành phần WinForms dùng chung
  BlazorShared/      Giao diện Blazor và HTTP clients
BUS/                 Nghiệp vụ ứng dụng: service, quy tắc và điều phối
DAO/                 Truy cập dữ liệu qua các DAO có kiểu
API/BookStore.Api/   ASP.NET Core REST API và bộ chuyển HTTP
Contracts/           DTO và hợp đồng trao đổi giữa client/server
Domain/              Entity nghiệp vụ được lưu trữ
Infrastructure/       EF Core, DbContext, migrations và cấu hình persistence
docs/                Tài liệu kiến trúc
```

`TrangSach.sln` ở thư mục gốc chứa toàn bộ project để mở hoặc build cùng nhau.

## Luồng xử lý

```text
WinForms khách hàng: GUI → CustomerApiGateway → HTTPS REST API → BUS → DAO → Infrastructure/EF Core → SQL Server
WinForms quản lý:    GUI → BUS → DAO → Infrastructure/EF Core → SQL Server
Blazor:              GUI → HTTPS REST API → BUS → DAO → Infrastructure/EF Core → SQL Server
```

Ứng dụng khách hàng là client phân tán nên không tham chiếu BUS server, DAO hay Infrastructure. `GUI/CustomerApp/Services/CustomerApiGateway` là adapter HTTP thuộc GUI: nó gửi request HTTPS và ánh xạ response thành kiểu dữ liệu cho form. Nghiệp vụ có thẩm quyền chạy tại API server qua BUS. `Contracts/BookStore.Contracts` chứa các kiểu trao đổi chung, không chứa truy cập database.

Ứng dụng quản lý hiện chạy trong môi trường nội bộ tin cậy và dùng composition root ở `GUI/ManagementApp/Services/ManagementAppServices` để gọi BUS trong cùng tiến trình. API và ManagementApp dùng chung BUS, vì vậy quy tắc nghiệp vụ vẫn nằm ngoài form/controller.

## Trách nhiệm từng lớp

- **GUI** (`GUI/CustomerApp`, `GUI/ManagementApp`, `GUI/Common`, `GUI/BlazorShared`): form, view, điều hướng, hiển thị và nhận thao tác người dùng. Không truy vấn DbContext.
- **BUS** (`BUS/Services`): kiểm tra quy tắc nghiệp vụ, điều phối ca sử dụng và gọi DAO. Không phụ thuộc WinForms, ASP.NET controllers hoặc EF Core.
- **DAO** (`DAO/Entities`): truy vấn và cập nhật dữ liệu qua phương thức DAO/unit of work; không đưa `DbContext`, `DbSet` hoặc `IQueryable` ra GUI/API.
- **API** (`API/BookStore.Api/Controllers`): adapter HTTP, xác thực/ủy quyền và chuyển request/response. Controller gọi BUS, không tự truy cập database.
- **Infrastructure** (`Infrastructure/BookStore.Infrastructure`): triển khai persistence bằng EF Core, context, mapping, migrations và seeding.
- **Domain** (`Domain/BookStore.Domain`): entity được lưu trữ và các kiểu miền cốt lõi.
- **Contracts** (`Contracts/BookStore.Contracts`): DTO dùng chung giữa client và server, không tham chiếu DAO hoặc Infrastructure.

API host có composition root trong `Program.cs`: nơi đăng ký `DbContext`, DAO và BUS vào DI, đồng thời chạy khởi tạo/migration database. Đây là wiring lúc khởi động. Các thao tác theo request như kiểm tra trạng thái tài khoản và ghi audit đều gọi BUS; middleware không truy vấn DAO trực tiếp.

## Quy tắc kết nối

Connection string SQL Server chỉ được cấu hình ở môi trường chạy API hoặc ManagementApp. Không đưa connection string database vào CustomerApp. CustomerApp lấy URL API từ `GUI/CustomerApp/appsettings.json`; môi trường triển khai phải cấu hình địa chỉ HTTPS và chứng thư TLS hợp lệ. Không mở SQL Server trực tiếp cho mạng Internet.

Xem thêm [bản đồ entity theo lớp](ENTITY_LAYER_MAP.md) và lệnh chạy trong [README](../README.md).
