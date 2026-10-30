# Kiến trúc dự án Trang Sách

## Cấu trúc thư mục

```text
Apps/
  Management/
    GUI/                 WinForms quản lý
    BUS/                 Điều phối use case và quyền riêng của ứng dụng quản lý
  Customer/
    GUI/                 WinForms khách hàng
    BUS/                 Client BUS gọi API qua HTTPS
  Shared/GUI/            Thành phần giao diện dùng chung
Server/
  API/                   ASP.NET Core REST API và bộ chuyển HTTP
  BUS/                   Nghiệp vụ lõi dùng chung
  DAO/                   Truy cập dữ liệu qua các DAO có kiểu
  Domain/                Entity nghiệp vụ được lưu trữ
  Infrastructure/        EF Core, DbContext, migrations và persistence
Shared/Contracts/        DTO trao đổi giữa client và server
docs/                    Tài liệu kiến trúc
```

`TrangSach.sln` ở thư mục gốc chứa toàn bộ project để mở hoặc build cùng nhau.

## Luồng xử lý

```text
WinForms khách hàng: Apps/Customer/GUI → Apps/Customer/BUS → HTTPS API → Server/BUS → Server/DAO → Server/Infrastructure → SQL Server
WinForms quản lý:    Apps/Management/GUI → Apps/Management/BUS → Server/BUS và Server/DAO → Server/Infrastructure → SQL Server
Blazor:              Apps/Shared/GUI → HTTPS API → Server/BUS → Server/DAO → Server/Infrastructure → SQL Server
```

App khách hàng là client phân tán, không tham chiếu BUS server, DAO hay Infrastructure. `Apps/Customer/BUS/CustomerApiGateway` gửi request HTTPS và ánh xạ response cho GUI; nó không truy cập database. Nghiệp vụ có thẩm quyền chạy tại API server qua `Server/BUS`. `Shared/Contracts` chứa kiểu dữ liệu trao đổi, không chứa truy cập database.

`Apps/Management/BUS` chứa điều phối use case và quyền riêng của ứng dụng quản lý. BUS lõi và các dịch vụ nghiệp vụ dùng chung với API nằm tại `Server/BUS`. App quản lý chạy nội bộ, gọi các project server trong cùng tiến trình; composition root nằm ở `Apps/Management/GUI/Services/ManagementAppServices`.

## Trách nhiệm từng lớp

- **GUI** (`Apps/*/GUI`): form, view, điều hướng, hiển thị và nhận thao tác người dùng; không truy vấn DbContext.
- **BUS phía App** (`Apps/Management/BUS`, `Apps/Customer/BUS`): điều phối use case riêng của App. Customer BUS gọi HTTPS API, không truy cập DAO.
- **BUS lõi** (`Server/BUS/Services`): quy tắc và nghiệp vụ có thẩm quyền, dùng chung bởi API và ứng dụng quản lý.
- **DAO** (`Server/DAO/Entities`): truy vấn/cập nhật dữ liệu qua DAO; không đưa `DbContext`, `DbSet` hoặc `IQueryable` ra GUI/API.
- **API** (`Server/API/BookStore.Api/Controllers`): adapter HTTP, xác thực/ủy quyền và chuyển request/response; controller gọi BUS, không tự truy cập database.
- **Infrastructure** (`Server/Infrastructure/BookStore.Infrastructure`): EF Core, context, mapping, migrations và seeding.
- **Domain** (`Server/Domain/BookStore.Domain`): entity và kiểu miền cốt lõi.
- **Contracts** (`Shared/Contracts/BookStore.Contracts`): DTO dùng chung giữa client/server, không tham chiếu DAO hoặc Infrastructure.

API host đăng ký DbContext, DAO và BUS vào DI trong `Program.cs`; middleware không truy vấn DAO trực tiếp.

## Quy tắc kết nối

Connection string SQL Server chỉ cấu hình ở môi trường chạy API hoặc ManagementApp. Không đưa connection string database vào CustomerApp. CustomerApp lấy URL API từ `Apps/Customer/GUI/appsettings.json`; môi trường triển khai cần HTTPS và chứng thư TLS hợp lệ. Không mở SQL Server trực tiếp ra Internet.

Xem thêm [bản đồ entity theo lớp](ENTITY_LAYER_MAP.md) và hướng dẫn chạy trong [README](../README.md).
