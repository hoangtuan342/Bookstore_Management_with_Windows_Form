# Entity coverage by GUI, BUS and DAO

Each persisted entity and DAO is defined directly in `Server/DAO`. BUS services enforce workflow rules and call those DAOs. API controllers adapt HTTP requests to BUS commands. Customer WinForms uses `Apps/Customer/BUS/CustomerApiGateway` to call the API; management WinForms uses its App BUS and server BUS in-process. Detail and join entities are handled inside the owning workflow to preserve stock, totals, voucher usage and audit history.

| Database entity | GUI workflow | BUS service/workflow | DAO set/table |
|---|---|---|---|
| `BookCategory` | Management → Danh mục tham chiếu | `AdminRecordsService` | `BookCategoryDao` → `Categories` / `LoaiSach` |
| `Book` | Management → Sách; Customer → catalog | `AdminBookService`, `BookCatalogService` | `BookDao` → `Books` / `Sach` |
| `Publisher` | Management → Danh mục tham chiếu | `AdminRecordsService` | `PublisherDao` → `Publishers` / `NhaXuatBan` |
| `Author` | Management → Danh mục tham chiếu; book author selection | `AdminRecordsService`, `AdminBookService` | `AuthorDao` → `Authors` / `TacGia` |
| `BookAuthor` | Author selection while editing a book | `AdminBookService` | `BookAuthorDao` → `BookAuthors` / `TacGiaSach` |
| `UserRole` | Employee role selection | `AuthenticationService`, `AdminRecordsService` | `UserRoleDao` → `Roles` / `VaiTro` |
| `UserAccount` | Customer/employee sign-in, profile and account edits | `AuthenticationService`, `AdminRecordsService` | `UserAccountDao` → `Accounts` / `TaiKhoan` |
| `Employee` | Management → Nhân viên | `AdminRecordsService`, `AdminOrderService`, `AdminInventoryService` | `EmployeeDao` → `Employees` / `NV` |
| `Customer` | Management → Khách hàng; registration/profile | `AuthenticationService`, `AdminRecordsService`, `CustomerCommerceService` | `CustomerDao` → `Customers` / `KH` |
| `Supplier` | Management → Nhà cung cấp and receipt creation | `AdminInventoryService` | `SupplierDao` → `Suppliers` / `NCC` |
| `PurchaseReceipt` | Management → Phiếu nhập | `AdminInventoryService` | `PurchaseReceiptDao` → `PurchaseReceipts` / `PNH` |
| `PurchaseReceiptLine` | Receipt line editor / receipt detail | `AdminInventoryService` | `PurchaseReceiptLineDao` → `PurchaseReceiptLines` / `CTPNH` |
| `SalesOrder` | Customer → Đơn của tôi; Management → Đơn hàng | `CustomerCommerceService`, `AdminOrderService` | `SalesOrderDao` → `Orders` / `HD` |
| `SalesOrderLine` | Order details within customer/admin order | `CustomerCommerceService`, `AdminOrderService` | `SalesOrderLineDao` → `OrderLines` / `CTHD` |
| `CartItem` | Customer → Giỏ hàng | `CustomerCommerceService` | `CartItemDao` → `CartItems` / `GioHang` |
| `Promotion` | Management → Khuyến mãi | `AdminPromotionService` | `PromotionDao` → `Promotions` / `CTKM` |
| `BookPromotion` | Book discount rules inside promotion editor | `AdminPromotionService`, `BookPricingService` | `BookPromotionDao` → `BookPromotions` / `KMTheoSach` |
| `OrderPromotion` | Order discount rules inside promotion editor | `AdminPromotionService`, `BookPricingService` | `OrderPromotionDao` → `OrderPromotions` / `KMTheoHD` |
| `Voucher` | Management → Voucher; checkout code | `AdminVoucherService`, `AdminPromotionService`, `CustomerCommerceService` | `VoucherDao` → `Vouchers` / `Voucher` |
| `PromotionVoucher` | Voucher rule inside promotion editor | `AdminPromotionService` | `PromotionVoucherDao` → `PromotionVouchers` / `KMTheoVoucher` |
| `SystemAuditLog` | Management → Nhật ký hệ thống | `AdminLogsService`, `ManagementBusinessService`, API audit middleware | `SystemAuditLogDao` → `SystemAuditLogs` / `SystemAuditLog` |

## Layer boundaries

- GUI projects: `Apps/Customer/GUI`, `Apps/Management/GUI`, and shared WinForms forms in `Apps/Shared/GUI`.
- App BUS: `Apps/Management/BUS` coordinates internal management use cases; `Apps/Customer/BUS` contains the HTTPS API client facade.
- Server BUS: reusable business services in `Server/BUS`, used by the API and management app.
- DAO/persistence: typed DAOs, persisted entities, ADO.NET unit of work, table mappings, connection validation and data seeding are in `Server/DAO`.
- Shared DTOs and permission rules: `Shared/DTO` and `Shared/Security`; neither contains database access.
- API: `Server/API/Controllers` and `Server/API/Contracts` are HTTP adapters. Controllers call BUS services; persistence remains behind DAO methods.

Customer WinForms references its client BUS and shared DTOs; it has no project reference to server BUS or DAO. Management WinForms composes its App BUS and server BUS/DAO in-process for internal use. The API host uses the same server BUS rules and typed DAO operations.


