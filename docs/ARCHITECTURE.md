# Kiến trúc Trang Sách

## Cấu trúc

```text
Apps/
  Customer/GUI/Forms/            WinForms khách hàng
  Customer/Web/Pages/            Razor Pages khách hàng
  Customer/Web/wwwroot/          CSS website khách hàng
  Customer/BUS/Services/         BUS client gọi HTTPS API cho WinForms và Web
  Management/GUI/Forms/          WinForms quản lý
  Management/BUS/Services/       Use case quản lý
  Management/BUS/ImportExport/   Xử lý nhập/xuất CSV/Excel
  Management/Web/GUI/Pages/      Website quản lý bằng Razor Pages
  Management/BUS/Web/            BUS client gọi API quản trị qua HTTPS
  Shared/GUI/Forms/              Form nhập liệu dùng chung
Server/
  API/                          REST API, controller và middleware
  BUS/Services/                 Nghiệp vụ dùng chung phía server
  DAO/                          Project DAO của server
    Entities/                   Các kiểu dữ liệu lưu trữ
    Repositories/               Các DAO truy vấn theo dữ liệu
    Infrastructure/             Kết nối, kiểm tra schema và dữ liệu khởi tạo

Shared/
  DTO/{Accounts,Catalog,Commerce,Inventory,People,Promotions,Reports,Audit}/ Kiểu dữ liệu trao đổi theo nghiệp vụ
  Security/                     Quy tắc quyền dùng chung
```

Mã nguồn được đặt theo lớp và ứng dụng; các file `.csproj` là cấu hình build của từng project. Tất cả file mã nguồn trong GUI kết thúc bằng `.GUI.cs`, trong BUS bằng `.BUS.cs`, trong DAO bằng `.DAO.cs`. Các khai báo `global using` được đặt trong `.csproj`, không tạo thêm file mã nguồn lệch quy ước. `TrangSach.sln` là solution tổng.

## Luồng xử lý

```text
WinForms khách hàng: Apps/Customer/GUI → Apps/Customer/BUS → HTTPS API → Server/BUS → Server/DAO → SQL Server
Web khách hàng:      Apps/Customer/Web/Pages → Apps/Customer/BUS → HTTPS API → Server/BUS → Server/DAO → SQL Server
WinForms quản lý:    Apps/Management/GUI → Apps/Management/BUS → Server/BUS → Server/DAO → SQL Server
Web quản lý:         Apps/Management/Web/GUI → Apps/Management/BUS/Web → HTTPS API → Server/BUS → Server/DAO → SQL Server
```

Customer app chỉ gọi API, không chứa chuỗi kết nối database. Ứng dụng quản lý chạy nội bộ và gọi BUS server trong cùng tiến trình. API là cổng giao tiếp HTTP(S), không phải một lớp dữ liệu của GUI–BUS–DAO.

## Trách nhiệm

- **GUI** nhận thao tác, hiển thị dữ liệu và gọi BUS.
- **GUI khách hàng** khởi tạo `CustomerApiGateway` tại `CustomerForm.Main` rồi truyền nó cho Form; không có lớp service trung gian trong thư mục GUI.
- **Web khách hàng** dùng Razor Pages làm GUI; các trang chỉ bind input, hiển thị DTO và gọi BUS client. Cookie xác thực của ASP.NET Core giữ JWT được bảo vệ; trình duyệt không nhận thông tin kết nối database.
- **Web quản lý** dùng Razor Pages làm GUI; BUS client giữ trạng thái ngoài form và gọi API HTTPS. Cookie xác thực bảo vệ JWT nhân viên, còn quyền ở từng trang lẫn từng API được kiểm tra theo vai trò.
- **BUS ứng dụng** điều phối use case của từng client. Customer BUS gọi API qua HTTPS; Management BUS gọi nghiệp vụ server trong tiến trình nội bộ.
- **Management BUS** thực hiện use case quản lý, phân tích tìm kiếm và đọc/ghi file CSV, Excel. Form quản lý xử lý sự kiện chọn file và dựng nội dung báo cáo để xuất.
- Các Form trang trong Management GUI được nhúng vào tab của Form quản lý; chúng chỉ dựng giao diện và phát sự kiện về Form quản lý, không gọi DAO hoặc tự chứa quy tắc nghiệp vụ.
- **API** chuyển HTTP request thành lời gọi BUS và trả response; controller không truy cập DAO.
- **BUS server** xác thực quyền, kiểm tra quy tắc nghiệp vụ và điều phối thao tác dữ liệu.
- **DAO** dùng `Microsoft.Data.SqlClient` để đọc/ghi SQL Server bằng lệnh tham số hóa. Dữ liệu được nạp vào `List` cục bộ; `AdoBookStoreUnitOfWork` quản lý liên kết, snapshot và transaction.
- **DTO** nằm trong `Shared/DTO`; không chứa logic truy cập database.
- **Security** nằm trong `Shared/Security` để dùng chung quy tắc phân quyền; đây là thư viện hỗ trợ, không phải lớp thứ tư.

## Database

Database và schema phải được tạo sẵn trên SQL Server. Khi khởi động, `DirectDatabase` kết nối tới database đã cấu hình và kiểm tra các bảng/cột bắt buộc; database hoặc bảng bị thiếu sẽ gây lỗi thay vì được tự tạo. `DatabaseInitializer` chỉ bổ sung dữ liệu tham chiếu còn thiếu; ứng dụng không tự tạo đơn hàng hay phiếu nhập mẫu.

Connection string được cấu hình ở API hoặc ứng dụng quản lý nội bộ. Không phân phối thông tin SQL Server cho customer app.






