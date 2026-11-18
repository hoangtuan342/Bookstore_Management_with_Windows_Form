# Hướng dẫn ý nghĩa các file trong dự án

Tài liệu này mô tả từng file mã nguồn, project, cấu hình, tài liệu và ảnh đang có trong repository. Không liệt kê `bin/`, `obj/` và `.git/` vì đó là dữ liệu build hoặc dữ liệu nội bộ của Git, có thể được tạo lại.

## Cấu trúc tổng quát

```text
Apps/       Hai ứng dụng WinForms và GUI dùng chung
Server/     API, nghiệp vụ server và truy cập SQL Server
Shared/     DTO và quy tắc phân quyền dùng chung
Docs/       Tài liệu kiến trúc và ánh xạ dữ liệu
Ảnh.../     Ảnh yêu cầu, giao diện tham khảo và lỗi
```

## File ở thư mục gốc

| File | Ý nghĩa |
|---|---|
| `.gitignore` | Quy định file/thư mục Git bỏ qua, như đầu ra build và file môi trường cục bộ. |
| `README.md` | Giới thiệu dự án, yêu cầu cài đặt, lệnh chạy API và hai ứng dụng. |
| `TrangSach.sln` | Solution Visual Studio chứa các project của toàn hệ thống. |

## Ứng dụng khách hàng — `Apps/Customer`

### BUS

| File | Ý nghĩa |
|---|---|
| `BUS/Services/CustomerApiGateway.BUS.cs` | Gọi HTTPS API, gửi token đăng nhập, chuyển yêu cầu/response thành DTO cho GUI. |
| `BUS/Services/CustomerWebApiClient.BUS.cs` | Gọi cùng HTTPS API theo kiểu stateless cho Razor Pages; nhận JWT theo từng request để dùng cookie đăng nhập của website. |
| `BUS/TrangSach.Customer.BUS.csproj` | Cấu hình build và thư viện/tham chiếu của BUS khách hàng. |

### GUI

| File | Ý nghĩa |
|---|---|
| `GUI/Forms/CustomerForm.GUI.cs` | Form chính, điểm khởi chạy ứng dụng, khởi tạo cấu hình API và bố cục tổng thể. |
| `GUI/Forms/CustomerForm.Authentication.GUI.cs` | Sự kiện và phần hiển thị đăng nhập, đăng ký, đăng xuất. |
| `GUI/Forms/CustomerForm.Customer.GUI.cs` | Giao diện danh mục sách, tìm kiếm, giỏ hàng và thao tác mua của khách. |
| `GUI/Forms/CustomerForm.Shared.GUI.cs` | Thành phần giao diện và hàm hỗ trợ được dùng chung trong các phần của Form khách hàng. |
| `GUI/appsettings.GUI.json` | Cấu hình địa chỉ API mà ứng dụng khách hàng kết nối tới. |
| `GUI/TrangSach.Customer.GUI.csproj` | Cấu hình build WinForms khách hàng và tham chiếu tới BUS/DTO/UI dùng chung. |

### Website khách hàng — `Apps/Customer/Web`

| File/thư mục | Ý nghĩa |
|---|---|
| `Program.cs` | Cấu hình Razor Pages, HttpClient gọi API, cookie xác thực và pipeline HTTPS. |
| `TrangSach.Customer.Web.csproj` | Cấu hình build website và tham chiếu Customer BUS/DTO. |
| `appsettings.json` | Địa chỉ API mà website gọi tới. |
| `Pages/Index.cshtml` và `Pages/Index.cshtml.cs` | Trang cửa hàng, tìm kiếm/lọc danh mục và thêm sách vào giỏ. |
| `Pages/Books/Details.cshtml` và `Pages/Books/Details.cshtml.cs` | Chi tiết sách, thêm vào giỏ và mua ngay. |
| `Pages/Account/*` | Giao diện và xử lý đăng nhập, đăng ký, đăng xuất; tạo cookie xác thực cho khách hàng. |
| `Pages/Cart/*` | Xem giỏ, cập nhật số lượng, xóa sản phẩm và chuyển tới thanh toán. |
| `Pages/Checkout/*` | Nhập địa chỉ, điện thoại, voucher, phương thức thanh toán và gửi đơn hàng. |
| `Pages/Orders/*` | Xem danh sách đơn và chi tiết đơn hàng của tài khoản hiện tại. |
| `Pages/CustomerPageModel.GUI.cs` | PageModel cơ sở, lấy JWT từ principal và cung cấp xử lý lỗi chung. |
| `Pages/Shared/_Layout.cshtml` | Khung chung của website, điều hướng cửa hàng/giỏ/đơn hàng/tài khoản. |
| `wwwroot/css/site.css` | Kiểu dáng responsive cho website khách hàng. |
| `Properties/launchSettings.json` | Cấu hình chạy website cục bộ tại cổng HTTPS 7315. |

## Ứng dụng quản lý — `Apps/Management`

### Website quản lý — `Web/GUI`

| File/thư mục | Ý nghĩa |
|---|---|
| `Web/GUI/Program.cs` | Cấu hình Razor Pages, HttpClient HTTPS, cookie xác thực và pipeline web quản lý. |
| `Web/GUI/Pages/Account/*` | Đăng nhập nhân viên, thiết lập quản lý đầu tiên trong môi trường Development, đăng xuất và từ chối quyền. |
| `Web/GUI/Pages/Admin/Index.*` | Tổng quan doanh thu, đơn hàng, sách và khách hàng. |
| `Web/GUI/Pages/Admin/Books/*` | Tìm kiếm, thêm/sửa/ẩn sách và nhập/xuất CSV. |
| `Web/GUI/Pages/Admin/Customers/*`, `Employees/*` | CRUD khách hàng, tài khoản liên quan và hồ sơ nhân viên theo quyền. |
| `Web/GUI/Pages/Admin/Orders/*` | Tạo, sửa thông tin giao hàng, đổi trạng thái/hủy và cập nhật thanh toán đơn. |
| `Web/GUI/Pages/Admin/Inventory/*`, `Suppliers/*` | Tạo/đảo phiếu nhập và CRUD nhà cung cấp. |
| `Web/GUI/Pages/Admin/References/*` | CRUD thể loại, tác giả, nhà xuất bản. |
| `Web/GUI/Pages/Admin/Promotions/*`, `Vouchers/*` | Tạo/sửa/xóa chương trình, quy tắc khuyến mãi và cập nhật voucher. |
| `Web/GUI/Pages/Admin/Reports/*`, `Audit/*` | Báo cáo theo kỳ/ngày, tải CSV và xem nhật ký hệ thống. |
| `Web/GUI/Pages/Shared/_Layout.cshtml` | Khung giao diện quản trị, menu theo vai trò nhân viên. |
| `Web/GUI/wwwroot/css/admin*.css` | Kiểu dáng responsive cho các màn quản trị. |
| `BUS/Web/ManagementWebApiClient.BUS.cs` | BUS client stateless, gắn JWT vào request API quản trị. |
| `BUS/Web/TrangSach.Management.Web.BUS.csproj` | Project BUS web độc lập, chỉ tham chiếu DTO và Security, không tham chiếu DAO. |

### BUS

| File | Ý nghĩa |
|---|---|
| `BUS/Services/ManagementApplicationFacade.BUS.cs` | Tạo các dịch vụ BUS, cung cấp mặt tiền cho GUI và cache danh sách đọc để tái sử dụng. |
| `BUS/Services/ManagementBusinessService.BUS.cs` | Kiểm tra quyền, điều phối use case quản lý tới BUS server và ghi nhật ký thao tác. |
| `BUS/Services/ManagementOperationsService.BUS.cs` | Chuyển DTO lệnh của GUI sang lệnh nghiệp vụ server và chuyển kết quả về dạng GUI dùng. |
| `BUS/Services/ManagementSessionService.BUS.cs` | Xử lý phiên đăng nhập, tài khoản quản lý và trạng thái người dùng nội bộ. |
| `BUS/Services/ManagementSearchExpression.BUS.cs` | Phân tích biểu thức tìm kiếm và lọc danh sách đã tải trong bộ nhớ. |
| `BUS/ImportExport/BookCsvFileService.BUS.cs` | Đọc/ghi CSV sách, phân tích dòng và kiểm tra dữ liệu nhập. |
| `BUS/ImportExport/BookExcelFileService.BUS.cs` | Chuyển dữ liệu sách giữa workbook Excel và các lệnh/dòng dữ liệu của ứng dụng. |
| `BUS/ImportExport/ExcelWorkbookService.BUS.cs` | Đọc và ghi file workbook `.xls`/`.xlsx` thông qua thư viện Excel. |
| `BUS/TrangSach.Management.BUS.csproj` | Cấu hình build BUS quản lý và tham chiếu tới BUS/DAO server, DTO, bảo mật. |

### GUI

| File | Ý nghĩa |
|---|---|
| `GUI/Forms/ManagementForm.GUI.cs` | Form khung quản lý, menu/tab, khởi tạo dịch vụ và điểm khởi chạy ứng dụng. |
| `GUI/Forms/ManagementForm.Authentication.GUI.cs` | Sự kiện đăng nhập/đăng xuất, thiết lập tài khoản quản trị đầu tiên và tải dữ liệu ban đầu. |
| `GUI/Forms/ManagementForm.Pages.GUI.cs` | Tạo các trang quản lý và gắn trang Form vào vùng tab. |
| `GUI/Forms/ManagementForm.Shared.GUI.cs` | Hàm dùng chung cho các thao tác, trạng thái và thành phần của Form quản lý. |
| `GUI/Forms/ManagementForm.Books.GUI.cs` | Sự kiện thêm/sửa/ẩn sách và nhập/xuất danh sách sách. |
| `GUI/Forms/ManagementForm.Records.GUI.cs` | Sự kiện CRUD khách hàng, nhân viên, nhà cung cấp và danh mục tham chiếu. |
| `GUI/Forms/ManagementForm.OrdersInventory.GUI.cs` | Sự kiện xử lý đơn hàng, phiếu nhập và tồn kho. |
| `GUI/Forms/ManagementForm.Promotions.GUI.cs` | Sự kiện tạo/sửa/xóa khuyến mãi và voucher. |
| `GUI/Forms/ManagementForm.Data.GUI.cs` | Tải dữ liệu bảng điều khiển/báo cáo và xử lý giao diện xuất PDF/Excel. |
| `GUI/Forms/ManagementGridPageForm.GUI.cs` | Form lưới dữ liệu dùng chung: tìm kiếm, bộ lọc, nút thao tác và sự kiện trang. |
| `GUI/Forms/ManagementLoginForm.GUI.cs` | Form đăng nhập quản lý và phát sự kiện khi người dùng yêu cầu đăng nhập. |
| `GUI/Forms/ReportDashboardForm.GUI.cs` | Form trình bày báo cáo, chọn khoảng ngày và yêu cầu xuất báo cáo. |
| `GUI/Forms/DashboardPageForm.GUI.cs` | Form trang tổng quan và bộ lọc dữ liệu tổng quan. |
| `GUI/Forms/BooksPageForm.GUI.cs` | Form danh sách sách và các bộ lọc theo nghiệp vụ sách. |
| `GUI/Forms/CustomersPageForm.GUI.cs` | Form danh sách khách hàng và bộ lọc khách hàng. |
| `GUI/Forms/EmployeesPageForm.GUI.cs` | Form danh sách nhân viên và bộ lọc trạng thái/vị trí. |
| `GUI/Forms/OrdersPageForm.GUI.cs` | Form danh sách đơn hàng và bộ lọc đơn. |
| `GUI/Forms/ReceiptsPageForm.GUI.cs` | Form danh sách phiếu nhập và bộ lọc phiếu. |
| `GUI/Forms/SuppliersPageForm.GUI.cs` | Form danh sách nhà cung cấp và bộ lọc liên hệ. |
| `GUI/Forms/ReferenceDataPageForm.GUI.cs` | Form danh mục tham chiếu như thể loại, tác giả, nhà xuất bản. |
| `GUI/Forms/PromotionsPageForm.GUI.cs` | Form danh sách khuyến mãi và bộ lọc thời gian. |
| `GUI/Forms/VouchersPageForm.GUI.cs` | Form danh sách voucher và bộ lọc trạng thái/thời hạn. |
| `GUI/Forms/AuditLogsPageForm.GUI.cs` | Form nhật ký hệ thống với lọc theo chức năng và khoảng ngày. |
| `GUI/TrangSach.Management.GUI.csproj` | Cấu hình build WinForms quản lý và tham chiếu tới BUS cùng GUI dùng chung. |

## GUI dùng chung — `Apps/Shared/GUI`

| File | Ý nghĩa |
|---|---|
| `Forms/EditDialog.GUI.cs` | Form nhập liệu nhỏ dùng lại trong hai ứng dụng WinForms. |
| `TrangSach.Shared.GUI.csproj` | Cấu hình build thư viện Form dùng chung. |

## API server — `Server/API`

| File | Ý nghĩa |
|---|---|
| `Program.cs` | Khởi tạo web host, đăng ký dịch vụ, xác thực và middleware API. |
| `TrangSach.API.csproj` | Cấu hình build REST API và các package/tham chiếu cần thiết. |
| `Properties/launchSettings.json` | Cấu hình profile, URL và môi trường chạy khi phát triển cục bộ. |
| `TrangSach.API.http` | Các HTTP request mẫu để gọi thử endpoint API. |
| `Contracts/AuthContracts.cs` | Kiểu request/response HTTP cho đăng ký và đăng nhập. |
| `Contracts/CommerceContracts.cs` | Kiểu request/response HTTP cho giỏ hàng và thanh toán. |
| `Middleware/AuditLogMiddleware.cs` | Ghi nhật ký các request API sau khi request được xử lý. |
| `Controllers/AuthController.cs` | Endpoint HTTP xác thực, đăng ký, đăng nhập và phát token. |
| `Controllers/BooksController.cs` | Endpoint công khai để đọc danh mục và chi tiết sách. |
| `Controllers/CartController.cs` | Endpoint đọc và cập nhật giỏ hàng khách hàng. |
| `Controllers/OrdersController.cs` | Endpoint tạo và xem đơn hàng phía khách hàng. |
| `Controllers/AdminBooksController.cs` | Endpoint quản lý sách; chuyển request thành lệnh BUS sách. |
| `Controllers/AdminDashboardController.cs` | Endpoint dữ liệu tổng quan cho trang quản trị. |
| `Controllers/AdminInventoryController.cs` | Endpoint nhà cung cấp, phiếu nhập và tra cứu tồn kho. |
| `Controllers/AdminLogsController.cs` | Endpoint đọc nhật ký hệ thống quản trị. |
| `Controllers/AdminOrdersController.cs` | Endpoint quản lý và cập nhật trạng thái đơn hàng. |
| `Controllers/AdminPromotionsController.cs` | Endpoint quản lý chương trình khuyến mãi. |
| `Controllers/AdminRecordsController.cs` | Endpoint quản lý khách hàng, nhân viên và danh mục tham chiếu. |
| `Controllers/AdminReportsController.cs` | Endpoint truy xuất số liệu báo cáo quản trị. |
| `Controllers/AdminVouchersController.cs` | Endpoint quản lý trạng thái và thời hạn voucher. |

## BUS server — `Server/BUS`

| File | Ý nghĩa |
|---|---|
| `Services/AuthenticationService.BUS.cs` | Quy tắc đăng ký, đăng nhập, xác thực tài khoản và phiên người dùng. |
| `Services/BookCatalogService.BUS.cs` | Lấy và trình bày danh mục sách cho ứng dụng. |
| `Services/BookPricingService.BUS.cs` | Tính giá sách/đơn sau các quy tắc khuyến mãi. |
| `Services/CustomerCommerceService.BUS.cs` | Nghiệp vụ giỏ hàng, thanh toán và đơn mua của khách. |
| `Services/AdminBookService.BUS.cs` | Nghiệp vụ CRUD/ẩn-hiện sách và liên kết tác giả. |
| `Services/AdminDashboardService.BUS.cs` | Tổng hợp số liệu và đơn gần đây cho bảng điều khiển. |
| `Services/AdminInventoryService.BUS.cs` | Nghiệp vụ nhà cung cấp, phiếu nhập và tồn kho. |
| `Services/AdminLogsService.BUS.cs` | Đọc và ghi nhật ký thao tác hệ thống. |
| `Services/AdminOrderService.BUS.cs` | Tạo, sửa, hủy đơn và xử lý trạng thái/thanh toán/hoàn tiền. |
| `Services/AdminPromotionService.BUS.cs` | CRUD khuyến mãi và các quy tắc giảm giá liên quan. |
| `Services/AdminRecordsService.BUS.cs` | Nghiệp vụ danh mục, tác giả, nhà xuất bản, khách hàng và nhân viên. |
| `Services/AdminReportsService.BUS.cs` | Tổng hợp doanh thu, đơn hàng, sản phẩm và các số liệu báo cáo. |
| `Services/AdminVoucherService.BUS.cs` | Đọc, cập nhật và xóa voucher theo quy tắc nghiệp vụ. |
| `Controllers/ControllerExceptionHelper.cs` | Xử lý ngoại lệ chuẩn (ArgumentException, KeyNotFoundException, v.v.) và chuyển đổi mã HTTP tương ứng. |
| `Services/PasswordHashingService.BUS.cs` | Giao diện và triển khai băm/xác minh mật khẩu. |
| `TrangSach.BUS.csproj` | Cấu hình build thư viện nghiệp vụ server. |

## DAO và dữ liệu — `Server/DAO`

| File | Ý nghĩa |
|---|---|
| `Infrastructure/AdoBookStoreUnitOfWork.DAO.cs` | Quản lý kết nối/giao dịch ADO.NET và các thao tác lưu dữ liệu theo transaction. |
| `Repositories/AdoQueryExtensions.DAO.cs` | Các hàm LINQ dạng async trên danh sách Entity đã nạp trong bộ nhớ. |
| `Repositories/BookStoreDao.DAO.cs` | Điểm truy cập nhóm DAO, tạo unit of work và các repository theo bảng. |
| `Repositories/EntityDao.DAO.cs` | Lớp CRUD nền dùng chung cho DAO từng entity. |
| `Infrastructure/DirectDatabase.DAO.cs` | Đọc/lưu cấu hình kết nối, kết nối SQL Server và kiểm tra schema đã có. |
| `Infrastructure/DatabaseInitializer.DAO.cs` | Bổ sung dữ liệu tham chiếu còn thiếu vào database đã có schema. |
| `Entities/CatalogEntities.DAO.cs` | Entity lưu trữ sách, thể loại, tác giả, nhà xuất bản và quan hệ tác giả-sách. |
| `Entities/IdentityEntities.DAO.cs` | Entity tài khoản, vai trò và nhân viên. |
| `Entities/OrderEntities.DAO.cs` | Entity đơn hàng, dòng đơn, phiếu nhập và dòng phiếu nhập. |
| `Entities/PromotionEntities.DAO.cs` | Entity khuyến mãi và các liên kết khuyến mãi với sản phẩm/đơn. |
| `Entities/SystemAuditLog.DAO.cs` | Entity một bản ghi nhật ký hệ thống. |
| `Repositories/BookDao.DAO.cs` | Truy vấn/lưu sách và dữ liệu sách liên quan. |
| `Repositories/BookAuthorDao.DAO.cs` | Truy vấn/lưu liên kết sách–tác giả. |
| `Repositories/BookCategoryDao.DAO.cs` | Truy vấn/lưu thể loại sách. |
| `Repositories/AuthorDao.DAO.cs` | Truy vấn/lưu tác giả. |
| `Repositories/PublisherDao.DAO.cs` | Truy vấn/lưu nhà xuất bản. |
| `Repositories/CustomerDao.DAO.cs` | Truy vấn/lưu hồ sơ khách hàng. |
| `Repositories/EmployeeDao.DAO.cs` | Truy vấn/lưu hồ sơ nhân viên. |
| `Repositories/UserAccountDao.DAO.cs` | Truy vấn/lưu tài khoản đăng nhập. |
| `Repositories/UserRoleDao.DAO.cs` | Truy vấn/lưu vai trò tài khoản. |
| `Repositories/SalesOrderDao.DAO.cs` | Truy vấn/lưu đơn hàng. |
| `Repositories/SalesOrderLineDao.DAO.cs` | Truy vấn/lưu từng dòng sách của đơn hàng. |
| `Repositories/CartItemDao.DAO.cs` | Truy vấn/lưu từng mặt hàng trong giỏ. |
| `Repositories/PurchaseReceiptDao.DAO.cs` | Truy vấn/lưu phiếu nhập. |
| `Repositories/PurchaseReceiptLineDao.DAO.cs` | Truy vấn/lưu chi tiết phiếu nhập. |
| `Repositories/SupplierDao.DAO.cs` | Truy vấn/lưu nhà cung cấp. |
| `Repositories/PromotionDao.DAO.cs` | Truy vấn/lưu chương trình khuyến mãi. |
| `Repositories/BookPromotionDao.DAO.cs` | Truy vấn/lưu quy tắc khuyến mãi áp dụng theo sách. |
| `Repositories/OrderPromotionDao.DAO.cs` | Truy vấn/lưu quy tắc khuyến mãi áp dụng theo đơn. |
| `Repositories/PromotionVoucherDao.DAO.cs` | Truy vấn/lưu liên kết khuyến mãi với voucher. |
| `Repositories/VoucherDao.DAO.cs` | Truy vấn/lưu voucher và thông tin sử dụng. |
| `Repositories/SystemAuditLogDao.DAO.cs` | Truy vấn/lưu các bản ghi nhật ký. |
| `TrangSach.DAO.csproj` | Cấu hình build lớp truy cập dữ liệu và thư viện ADO.NET. |

## DTO — `Shared/DTO`

Các file dưới đây định nghĩa dữ liệu truyền giữa GUI, BUS và API; chúng không tự truy cập database.

| File | Ý nghĩa |
|---|---|
| `Accounts/AccountProfileDto.cs` | Thông tin hồ sơ tài khoản đang đăng nhập. |
| `Accounts/AuthenticatedAccountDto.cs` | Tài khoản đã xác thực cùng vai trò và mã khách hàng nếu có. |
| `Audit/AuditLogCommandDto.cs` | Dữ liệu lệnh ghi một sự kiện audit. |
| `Catalog/BestSellerDto.cs` | Số lượng và doanh thu của một sách bán chạy. |
| `Catalog/BookEditDetailsDto.cs` | Dữ liệu chi tiết sách dùng khi mở màn hình chỉnh sửa. |
| `Catalog/BookItemDto.cs` | Một dòng sách trong danh mục và lưới hiển thị. |
| `Catalog/BookManagementCommandDto.cs` | Lệnh tạo/cập nhật sách gửi tới nghiệp vụ. |
| `Catalog/BookVisibilityCommandDto.cs` | Lệnh bật/tắt hiển thị sách. |
| `Inventory/CategoryInventorySummaryDto.cs` | Số lượng sách, tồn kho và giá trị tồn theo thể loại. |
| `Catalog/CategoryOptionDto.cs` | Lựa chọn thể loại cho bộ chọn trên giao diện. |
| `Reports/CategorySalesDto.cs` | Số lượng và doanh thu bán theo thể loại. |
| `Reports/ChartSegmentDto.cs` | Một phân đoạn dữ liệu dùng để dựng biểu đồ. |
| `Commerce/CheckoutDetailsDto.cs` | Địa chỉ, số điện thoại, hình thức trả tiền và voucher khi thanh toán. |
| `Commerce/CreateAdminOrderDto.cs` | Lệnh tạo đơn từ màn hình quản lý. |
| `Commerce/CreateAdminOrderLineDto.cs` | Một dòng sách trong lệnh tạo đơn quản lý. |
| `Catalog/CreateManagerBookDto.cs` | Dữ liệu sách gửi từ GUI quản lý. |
| `Promotions/CreatePromotionDto.cs` | Dữ liệu tạo chương trình và quy tắc khuyến mãi. |
| `Inventory/CreateReceiptDto.cs` | Lệnh tạo phiếu nhập gồm nhà cung cấp và các dòng hàng. |
| `Inventory/CreateReceiptLineDto.cs` | Một dòng sách/số lượng/giá vốn của phiếu nhập. |
| `Inventory/CreateSupplierDto.cs` | Dữ liệu tạo hoặc sửa nhà cung cấp. |
| `Reports/CustomerSalesDto.cs` | Số đơn, số lượng và doanh thu theo khách hàng. |
| `Reports/EmployeeYearSalesDto.cs` | Doanh thu và số đơn theo nhân viên, theo năm. |
| `Catalog/IdNameOptionDto.cs` | Cặp mã–tên dùng cho danh sách chọn. |
| `Accounts/LoginCommandDto.cs` | Tên đăng nhập/email và mật khẩu gửi khi đăng nhập. |
| `Audit/ManagerAuditLogDto.cs` | Dòng nhật ký hiển thị trong màn hình quản trị. |
| `Catalog/ManagerAuthorDto.cs` | Thông tin tác giả dùng cho quản lý. |
| `Catalog/ManagerBookDetailsDto.cs` | Chi tiết sách quản lý, gồm tác giả và trạng thái. |
| `Catalog/ManagerCategoryDto.cs` | Thể loại dùng cho màn hình quản lý. |
| `People/ManagerCustomerDto.cs` | Hồ sơ khách hàng và trạng thái tài khoản. |
| `Reports/ManagerDashboardDto.cs` | Tổng doanh thu, đơn hàng và số liệu tổng quan. |
| `People/ManagerEmployeeDto.cs` | Hồ sơ nhân viên cùng thông tin tài khoản/vai trò. |
| `Inventory/ManagerLookupsDto.cs` | Các danh sách tham chiếu cần cho màn hình quản lý. |
| `Commerce/ManagerOrderLineDto.cs` | Một dòng chi tiết đơn hàng quản lý. |
| `Commerce/ManagerOrderRowDto.cs` | Một đơn hàng trong lưới quản lý. |
| `Promotions/ManagerPromotionDto.cs` | Thông tin chương trình khuyến mãi và các quy tắc. |
| `Catalog/ManagerPublisherDto.cs` | Nhà xuất bản dùng cho quản lý. |
| `Inventory/ManagerReceiptDto.cs` | Phiếu nhập cùng các dòng chi tiết. |
| `Inventory/ManagerReceiptLineDto.cs` | Một dòng chi tiết phiếu nhập. |
| `Commerce/ManagerRecentOrderDto.cs` | Đơn hàng gần đây trên dashboard. |
| `Reports/ManagerReportDto.cs` | Bộ số liệu báo cáo quản lý theo khoảng thời gian. |
| `Promotions/ManagerVoucherDto.cs` | Voucher và thống kê sử dụng cho quản lý. |
| `Reports/MonthlyRevenueDto.cs` | Doanh thu, số đơn, tiền nhập và lãi gộp theo tháng. |
| `Reports/ProductQuarterSalesDto.cs` | Doanh số sách theo quý. |
| `Promotions/PromotionBookRuleDto.cs` | Tỷ lệ giảm giá cho một sách trong khuyến mãi. |
| `Promotions/PromotionOrderRuleDto.cs` | Mức giảm theo giá trị đơn tối thiểu. |
| `Promotions/PromotionVoucherRuleDto.cs` | Quy tắc giảm giá gắn với voucher. |
| `Reports/QuarterlyRevenueDto.cs` | Doanh thu và chi phí theo quý. |
| `Inventory/ReceiptCommandDto.cs` | Lệnh nghiệp vụ nhập hàng chuẩn hóa. |
| `Inventory/ReceiptCreatedDto.cs` | Mã, tổng tiền và thời điểm tạo phiếu nhập. |
| `Inventory/ReceiptLineCommandDto.cs` | Dòng hàng trong lệnh nhập kho. |
| `Accounts/RegisterAccountCommandDto.cs` | Dữ liệu đăng ký tài khoản khách hàng. |
| `Reports/RevenueByDayDto.cs` | Doanh thu, chi nhập và số đơn theo ngày. |
| `Catalog/SaveAuthorDto.cs` | Dữ liệu lưu tác giả. |
| `Catalog/SaveCategoryDto.cs` | Dữ liệu lưu thể loại. |
| `People/SaveCustomerDto.cs` | Dữ liệu lưu hồ sơ khách hàng/tài khoản. |
| `People/SaveEmployeeDto.cs` | Dữ liệu lưu hồ sơ nhân viên/tài khoản/vai trò. |
| `Catalog/SavePublisherDto.cs` | Dữ liệu lưu nhà xuất bản. |
| `People/StaffRoleOptionDto.cs` | Tên và nhãn hiển thị của vai trò nhân viên. |
| `Accounts/StoreApiResultDto.cs` | Kết quả thao tác API cho ứng dụng khách hàng. |
| `Accounts/StoreAuthResultDto.cs` | Token, thời hạn và thông tin người dùng sau đăng nhập. |
| `Commerce/StoreCartLineDto.cs` | Một dòng sản phẩm trong giỏ khách hàng. |
| `Commerce/StoreOrderDto.cs` | Đơn hàng của khách, gồm trạng thái và chi tiết. |
| `Commerce/StoreOrderLineDto.cs` | Một dòng sách trong đơn hàng khách. |
| `Accounts/StoreProfileDto.cs` | Hồ sơ tài khoản khách hàng hiển thị trên ứng dụng. |
| `Inventory/SupplierCommandDto.cs` | Lệnh chuẩn hóa tạo/cập nhật nhà cung cấp. |
| `Inventory/SupplierOptionDto.cs` | Nhà cung cấp dùng trong danh sách chọn. |
| `Commerce/UpdateAdminOrderDto.cs` | Dữ liệu cập nhật thông tin đơn quản lý. |
| `Promotions/UpdatePromotionDetailsDto.cs` | Lệnh sửa tên và thời gian khuyến mãi. |
| `Promotions/UpdatePromotionRulesDto.cs` | Bộ quy tắc sách/đơn/voucher cần thay cho khuyến mãi. |
| `Promotions/UpdateVoucherDto.cs` | Dữ liệu GUI quản lý cập nhật voucher. |
| `Promotions/VoucherSummaryDto.cs` | Thông tin tổng hợp voucher trả từ nghiệp vụ. |
| `Promotions/VoucherUpdateCommandDto.cs` | Lệnh chuẩn hóa cập nhật trạng thái/hạn/số lượt voucher. |
| `Reports/YearlyRevenueDto.cs` | Doanh thu, số đơn, tiền nhập và lãi gộp theo năm. |
| `TrangSach.Shared.DTO.csproj` | Cấu hình build thư viện DTO dùng chung. |

## Quy tắc bảo mật — `Shared/Security`

| File | Ý nghĩa |
|---|---|
| `StaffPermissions.cs` | Định nghĩa tên vai trò, quyền truy cập khu vực và nhãn vai trò hiển thị. |
| `TrangSach.Shared.Security.csproj` | Cấu hình build thư viện quy tắc bảo mật dùng chung. |

## Tài liệu dự án — `docs`

| File | Ý nghĩa |
|---|---|
| `docs/ARCHITECTURE.md` | Mô tả các project/lớp và luồng GUI → BUS → DAO/API. |
| `docs/ENTITY_LAYER_MAP.md` | Ánh xạ entity database tới GUI, BUS và DAO xử lý. |
| `docs/FILE_GUIDE.md` | Tài liệu đang đọc: giải thích vai trò từng file trong repository. |

## Ảnh tham khảo và yêu cầu

Các ảnh là tư liệu đầu vào cho thiết kế hoặc yêu cầu; không được biên dịch thành mã nguồn.

### `Phong cách thiết kế` — ảnh tham khảo phong cách giao diện

- `02c7eac07e04fe5aa71511.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `1207df004bc4cb9a92d54.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `20e1dae44e20ce7e973117.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `236dc96a5daeddf084bf10.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `3af5d1f24536c5689c2715.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `452ca62a32eeb2b0ebff3.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `5ec911cc850805565c1924.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `6c65056191a511fb48b42.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `74b281b71573952dcc6225.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `75358a331ef79ea9c7e613.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `7aecabeb3f2fbf71e63e5.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `7fde80db141f9441cd0e19.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `8fcbf3ce670ae754be1b27.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `974f8348178c97d2ce9d1.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `98366933fdf77da924e626.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `a6d9cddc5918d946800920.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `a79be79e735af304aa4b21.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `ade351e5c521457f1c308.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `b0390a3f9efb1ea547ea9.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `b3a301a59561153f4c707.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `b6f4f4f16035e06bb92423.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `ba0188061cc29c9cc5d36.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `bee840eed42a54740d3b16.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `c23a753ce1f861a638e912.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `cb0e300ba4cf24917dde22.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `ec935f95cb514b0f124018.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.
- `f31a0a1f9edb1e8547ca14.jpg` — ảnh tham khảo phong cách thiết kế phần mềm.

### `Ảnh giao diện mẫu` — ảnh giao diện doanh nghiệp tham khảo

- `IMG20261020082453.jpg` — ảnh mẫu giao diện quản lý.
- `IMG20261020082948.jpg` — ảnh mẫu giao diện quản lý.
- `IMG20261020082953.jpg` — ảnh mẫu giao diện quản lý.
- `IMG20261020083034.jpg` — ảnh mẫu giao diện quản lý.
- `IMG20261020083142.jpg` — ảnh mẫu giao diện quản lý.
- `IMG20261020083430.jpg` — ảnh mẫu giao diện quản lý.
- `IMG20261020084229.jpg` — ảnh mẫu giao diện quản lý.

### `Ảnh yêu cầu đồ án` — ảnh yêu cầu chức năng/kiến trúc

- `2aOboR8t5KO01da5dUmV0AQt3t1MyOU961igELFQ.jpg` — ảnh chứa yêu cầu đồ án.
- `2aOboR8t5KVPmvXjljybXsCnE4cCZIkBSi7i2KG0.jpg` — ảnh chứa yêu cầu đồ án.

### `Ảnh các lỗi C#` — ảnh lỗi giao diện được chụp

- `Screenshot 2026-10-27 215452.png` — ảnh chụp lỗi/hiện tượng giao diện cần đối chiếu.
- `Screenshot 2026-10-27 220018.png` — ảnh chụp lỗi/hiện tượng giao diện cần đối chiếu.
- `Screenshot 2026-11-03 153002.png` — ảnh chụp lỗi/hiện tượng giao diện cần đối chiếu.

`Ảnh ví dụ` hiện không có file.

## Phạm vi và lưu ý

Tài liệu này bao quát các file hiện có trong repository, trừ thư mục `.git` và đầu ra build `bin/obj`. Ảnh được mô tả theo thư mục và tên file; nội dung chi tiết của từng ảnh cần mở ảnh tương ứng để đọc.




