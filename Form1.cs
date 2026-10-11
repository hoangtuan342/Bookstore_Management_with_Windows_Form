using BookStore.Shared.Models;
using BookStore.Shared.Security;
using BookStore.Shared.Services;
using System.Globalization;
using System.Windows.Forms;

namespace TrangSach.WinForms;

public partial class Form1 : Form
{
    private readonly AppServices _services;
    private readonly BookCsvFileService _bookFiles = new();
    private readonly bool _managementMode;
    private readonly TextBox _userName = new();
    private readonly TextBox _password = new();
    private readonly Label _status = new();
    private readonly TabControl _tabs = new();
    private DataGridView _storeGrid = new();
    private CustomerCatalogView? _customerCatalogView;
    private DataGridView _cartGrid = new();
    private DataGridView _myOrdersGrid = new();
    private DataGridView _dashboardGrid = new();
    private DataGridView _booksGrid = new();
    private DataGridView _customersGrid = new();
    private DataGridView _employeesGrid = new();
    private DataGridView _ordersGrid = new();
    private DataGridView _receiptsGrid = new();
    private ReportDashboardView? _reportView;
    private bool _reportsLoaded;
    private DataGridView _logsGrid = new();
    private DataGridView _referenceGrid = new();
    private DataGridView _suppliersGrid = new();
    private DataGridView _promotionsGrid = new();
    private DataGridView _vouchersGrid = new();
    private ComboBox _referenceType = new();
    private TextBox _shippingAddress = new();
    private TextBox _receiverPhone = new();
    private TextBox _voucherCode = new();
    private ComboBox _paymentChoice = new();
    private string? _role;

    public Form1(bool managementMode)
    {
        InitializeComponent();
        _managementMode = managementMode;
        _services = new AppServices();
        Text = managementMode ? "Trang Sách | Quản lý" : "Trang Sách | Cửa hàng khách hàng";
        MinimumSize = new Size(1040, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BuildShell();
        Shown += async (_, _) => await SafeAsync(async () =>
        {
            await _services.InitializeDatabaseAsync();
            _status.Text = (_managementMode ? "Đăng nhập nhân viên để mở khu vực quản lý" : "Bạn đang ở khu vực khách hàng") + " · SQL Server: " + DirectDatabase.ConnectionCaption;
            if (!_managementMode) await LoadStoreBooksAsync();
        });
        FormClosed += (_, _) => _services.Dispose();
    }

    private void BuildShell()
    {
        var brandColor = _managementMode ? Color.FromArgb(25, 45, 68) : Color.FromArgb(53, 86, 68);
        var header = new Panel { Dock = DockStyle.Top, Height = 122, Padding = new Padding(18, 12, 18, 8), BackColor = brandColor };
        var title = new Label { Text = _managementMode ? "TRANG SÁCH · KHU VỰC QUẢN LÝ" : "TRANG SÁCH · CỬA HÀNG KHÁCH HÀNG", ForeColor = Color.White, Font = new Font("Segoe UI", 18, FontStyle.Bold), AutoSize = true, Location = new Point(18, 10) };
        header.Controls.Add(title);
        _status.Text = (_managementMode ? "Đăng nhập nhân viên để mở khu vực quản lý" : "Bạn đang ở khu vực khách hàng") + " · SQL Server: " + DirectDatabase.ConnectionCaption;
        _status.ForeColor = Color.Gainsboro;
        _status.AutoSize = true;
        _status.Location = new Point(20, 48);
        header.Controls.Add(_status);

        _userName.PlaceholderText = "Tên đăng nhập hoặc email";
        _userName.SetBounds(18, 78, 230, 28);
        _password.PlaceholderText = "Mật khẩu";
        _password.UseSystemPasswordChar = true;
        _password.SetBounds(258, 78, 180, 28);
        var login = HeaderButton(_managementMode ? "Đăng nhập quản lý" : "Đăng nhập", 448, 77, _managementMode ? 145 : 105);
        login.Click += async (_, _) => await SafeAsync(LoginAsync);
        var register = HeaderButton("Đăng ký khách", 560, 77, 120);
        register.Click += async (_, _) => await SafeAsync(RegisterCustomerAsync);
        var logout = HeaderButton("Đăng xuất", 687, 77, 95);
        logout.Click += async (_, _) => await SafeAsync(LogoutAsync);
        var dbSettings = HeaderButton("Cơ sở dữ liệu", 789, 77, 120);
        dbSettings.Click += (_, _) => ConfigureDatabaseConnection();
        register.Visible = !_managementMode;
        header.Controls.AddRange([_userName, _password, login, register, logout, dbSettings]);

        _tabs.Dock = DockStyle.Fill;
        _tabs.Padding = new Point(16, 5);
        if (!_managementMode)
        {
            _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            _tabs.ItemSize = new Size(132, 36);
            _tabs.SizeMode = TabSizeMode.Fixed;
            _tabs.DrawItem += (_, e) =>
            {
                var selected = e.Index == _tabs.SelectedIndex;
                using var back = new SolidBrush(selected ? Color.FromArgb(53, 86, 68) : Color.FromArgb(251, 250, 247));
                using var fore = new SolidBrush(selected ? Color.White : Color.FromArgb(82, 91, 79));
                e.Graphics.FillRectangle(back, e.Bounds);
                TextRenderer.DrawText(e.Graphics, _tabs.TabPages[e.Index].Text, new Font("Segoe UI", 9, selected ? FontStyle.Bold : FontStyle.Regular), e.Bounds, selected ? Color.White : Color.FromArgb(82, 91, 79), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }
        _tabs.SelectedIndexChanged += async (_, _) =>
        {
            if (_tabs.SelectedTab?.Text == "Báo cáo" && _reportView is not null && !_reportsLoaded)
                await SafeAsync(() => LoadReportAsync(30));
        };
        Controls.Add(_tabs);
        Controls.Add(header);
        if (!_managementMode) BuildGuestPages();
    }

    private void BuildGuestPages()
    {
        if (_managementMode) return;
        _tabs.TabPages.Clear();
        var shop = new TabPage("Cửa hàng") { BackColor = Color.FromArgb(251, 250, 247) };
        _customerCatalogView = new CustomerCatalogView { Dock = DockStyle.Fill };
        _customerCatalogView.AddToCartRequested += async book => await SafeAsync(async () =>
        {
            var cart = await _services.Store.GetCartAsync();
            var quantity = cart.FirstOrDefault(x => x.BookId == book.Id)?.Quantity ?? 0;
            await _services.Store.SetCartQuantityAsync(book.Id, quantity + 1);
            _status.Text = $"Đã thêm ‘{book.Title}’ vào giỏ hàng.";
            await LoadCartAsync();
        });
        shop.Controls.Add(_customerCatalogView);
        _tabs.TabPages.Add(shop);
        var cart = CreateGridPage("Giỏ hàng", out _cartGrid);
        StyleCustomerGrid(_cartGrid);
        var cartTools = ToolBar(cart);
        _shippingAddress.Width = 220; _shippingAddress.PlaceholderText = "Địa chỉ giao hàng";
        _receiverPhone.Width = 145; _receiverPhone.PlaceholderText = "Số điện thoại";
        _voucherCode.Width = 120; _voucherCode.PlaceholderText = "Mã voucher";
        _paymentChoice.DropDownStyle = ComboBoxStyle.DropDownList;
        _paymentChoice.Width = 205;
        _paymentChoice.Items.Clear();
        _paymentChoice.Items.AddRange([new CashOnDeliveryOption(), new BankTransferOption()]);
        _paymentChoice.SelectedIndex = 0;
        cartTools.Controls.Add(_shippingAddress); cartTools.Controls.Add(_receiverPhone);
        cartTools.Controls.Add(_voucherCode); cartTools.Controls.Add(_paymentChoice);
        cartTools.Controls.Add(ActionButton("Tải giỏ", async () => await LoadCartAsync()));
        cartTools.Controls.Add(ActionButton("Đặt hàng", async () => await CheckoutAsync()));
        _tabs.TabPages.Add(cart);
        var orders = CreateGridPage("Đơn của tôi", out _myOrdersGrid);
        StyleCustomerGrid(_myOrdersGrid);
        ToolBar(orders).Controls.Add(ActionButton("Tải đơn hàng", async () => await LoadMyOrdersAsync()));
        _tabs.TabPages.Add(orders);
    }

    private void BuildStaffPages()
    {
        if (_role is null || _role == StaffRoles.Customer) return;
        _reportView = null;
        _reportsLoaded = false;
        if (StaffPermissions.CanAccess(_role, "dashboard"))
        {
            var page = CreateGridPage("Tổng quan", out _dashboardGrid);
            ToolBar(page).Controls.Add(ActionButton("Làm mới", async () => await LoadDashboardAsync()));
            _tabs.TabPages.Add(page);
        }
        if (StaffPermissions.CanAccess(_role, "books"))
        {
            var page = CreateGridPage("Sách", out _booksGrid); var bar = ToolBar(page);
            bar.Controls.Add(ActionButton("Tải", async () => await LoadBooksAsync()));
            bar.Controls.Add(ActionButton("Thêm", async () => await EditBookAsync(null)));
            bar.Controls.Add(ActionButton("Sửa", async () => await EditSelectedBookAsync()));
            bar.Controls.Add(ActionButton("Ẩn sách", async () => await HideSelectedBookAsync()));
            bar.Controls.Add(ActionButton("Nhập CSV", async () => await ImportBooksAsync()));
            bar.Controls.Add(ActionButton("Xuất CSV", async () => await ExportBooksAsync(_booksGrid)));
            _tabs.TabPages.Add(page);
        }
        if (StaffPermissions.CanAccess(_role, "customers"))
        {
            var page = CreateGridPage("Khách hàng", out _customersGrid); var bar = ToolBar(page);
            bar.Controls.Add(ActionButton("Tải", async () => await LoadCustomersAsync()));
            bar.Controls.Add(ActionButton("Thêm", async () => await EditCustomerAsync(null)));
            bar.Controls.Add(ActionButton("Sửa", async () => await EditSelectedCustomerAsync()));
            bar.Controls.Add(ActionButton("Xóa", async () => await DeleteSelectedCustomerAsync()));
            _tabs.TabPages.Add(page);
        }
        if (_role == StaffRoles.Manager)
        {
            var page = CreateGridPage("Nhân viên", out _employeesGrid); var bar = ToolBar(page);
            bar.Controls.Add(ActionButton("Tải", async () => await LoadEmployeesAsync()));
            bar.Controls.Add(ActionButton("Thêm", async () => await EditEmployeeAsync(null)));
            bar.Controls.Add(ActionButton("Sửa", async () => await EditSelectedEmployeeAsync()));
            bar.Controls.Add(ActionButton("Xóa", async () => await DeleteSelectedEmployeeAsync()));
            _tabs.TabPages.Add(page);
        }
        if (StaffPermissions.CanAccess(_role, "orders"))
        {
            var page = CreateGridPage("Đơn hàng", out _ordersGrid); var bar = ToolBar(page);
            bar.Controls.Add(ActionButton("Tải", async () => await LoadOrdersAsync()));
            bar.Controls.Add(ActionButton("Đổi trạng thái", async () => await ChangeOrderStatusAsync()));
            bar.Controls.Add(ActionButton("Tạo đơn", async () => await CreateOrderAsync()));
            bar.Controls.Add(ActionButton("Sửa thông tin", async () => await EditSelectedOrderAsync()));
            bar.Controls.Add(ActionButton("Hủy đơn", async () => await CancelSelectedOrderAsync()));
            _tabs.TabPages.Add(page);
        }
        if (StaffPermissions.CanAccess(_role, "inventory"))
        {
            var page = CreateGridPage("Phiếu nhập", out _receiptsGrid); var bar = ToolBar(page);
            bar.Controls.Add(ActionButton("Tải", async () => await LoadReceiptsAsync()));
            bar.Controls.Add(ActionButton("Tạo phiếu nhập", async () => await CreateReceiptAsync()));
            bar.Controls.Add(ActionButton("Đảo phiếu", async () => await CancelSelectedReceiptAsync()));
            _tabs.TabPages.Add(page);
        }
        if (StaffPermissions.CanAccess(_role, "reports"))
        {
            var page = new TabPage("Báo cáo");
            _reportView = new ReportDashboardView { Dock = DockStyle.Fill };
            _reportView.PeriodSelected += days => { _ = SafeAsync(() => LoadReportAsync(days)); };
            page.Controls.Add(_reportView);
            _tabs.TabPages.Add(page);
        }
        if (StaffPermissions.CanAccess(_role, "records"))
        {
            var page = CreateGridPage("Danh mục tham chiếu", out _referenceGrid); var bar = ToolBar(page);
            _referenceType.DropDownStyle = ComboBoxStyle.DropDownList; _referenceType.Items.Clear(); _referenceType.Items.AddRange(["Danh mục", "Tác giả", "Nhà xuất bản"]); _referenceType.SelectedIndex = 0; _referenceType.Width = 150;
            bar.Controls.Add(_referenceType); bar.Controls.Add(ActionButton("Tải", async () => await LoadReferencesAsync()));
            bar.Controls.Add(ActionButton("Thêm", async () => await SaveReferenceAsync(false))); bar.Controls.Add(ActionButton("Sửa", async () => await SaveReferenceAsync(true)));
            bar.Controls.Add(ActionButton("Xóa", async () => await DeleteReferenceAsync())); _tabs.TabPages.Add(page);
        }
        if (StaffPermissions.CanAccess(_role, "inventory"))
        {
            var page = CreateGridPage("Nhà cung cấp", out _suppliersGrid); var bar = ToolBar(page);
            bar.Controls.Add(ActionButton("Tải", async () => await LoadSuppliersAsync())); bar.Controls.Add(ActionButton("Thêm", async () => await SaveSupplierAsync(null)));
            bar.Controls.Add(ActionButton("Sửa", async () => await EditSelectedSupplierAsync())); bar.Controls.Add(ActionButton("Xóa", async () => await DeleteSelectedSupplierAsync())); _tabs.TabPages.Add(page);
        }
        if (StaffPermissions.CanAccess(_role, "promotions"))
        {
            var page = CreateGridPage("Khuyến mãi", out _promotionsGrid); var bar = ToolBar(page);
            bar.Controls.Add(ActionButton("Tải", async () => await LoadPromotionsAsync())); bar.Controls.Add(ActionButton("Tạo", async () => await CreatePromotionAsync()));
            bar.Controls.Add(ActionButton("Sửa tên/thời gian", async () => await EditPromotionAsync())); bar.Controls.Add(ActionButton("Xóa", async () => await DeletePromotionAsync())); _tabs.TabPages.Add(page);
            var voucherPage = CreateGridPage("Voucher", out _vouchersGrid); var voucherBar = ToolBar(voucherPage);
            voucherBar.Controls.Add(ActionButton("Tải", async () => await LoadVouchersAsync())); voucherBar.Controls.Add(ActionButton("Cập nhật", async () => await EditVoucherAsync()));
            voucherBar.Controls.Add(ActionButton("Xóa", async () => await DeleteVoucherAsync())); _tabs.TabPages.Add(voucherPage);
        }
        if (_role == StaffRoles.Manager)
        {
            var page = CreateGridPage("Nhật ký hệ thống", out _logsGrid);
            ToolBar(page).Controls.Add(ActionButton("Tải nhật ký", async () => await LoadLogsAsync()));
            _tabs.TabPages.Add(page);
        }
    }

    private void ConfigureDatabaseConnection()
    {
        var current = _services.DatabaseConnectionString;
        var values = EditDialog.Show(this, "Kết nối SQL Server", [("connection", "Connection string SQL Server", current)]);
        if (values is null) return;
        try
        {
            _ = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(values["connection"]);
            DirectDatabase.SaveConnectionString(values["connection"]);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "Connection string không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        MessageBox.Show(this, "Đã lưu cấu hình database. Hãy khởi động lại ứng dụng để áp dụng.", "Cấu hình đã lưu", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(_userName.Text) || string.IsNullOrWhiteSpace(_password.Text))
            throw new InvalidOperationException("Nhập tên đăng nhập/email và mật khẩu.");
        var result = _managementMode
            ? await _services.Store.AdminLoginAsync(_userName.Text.Trim(), _password.Text)
            : await _services.Store.LoginAsync(_userName.Text.Trim(), _password.Text);
        if (!result.Succeeded) throw new InvalidOperationException(result.Message);
        _role = result.Role;
        if (_managementMode && _role == StaffRoles.Customer)
        {
            await _services.Store.SignOutAsync();
            _role = null;
            throw new InvalidOperationException("Tài khoản này không có quyền truy cập khu vực quản lý. Hãy dùng tài khoản nhân viên.");
        }
        if (!_managementMode && _role != StaffRoles.Customer)
        {
            await _services.Store.SignOutAsync();
            _role = null;
            throw new InvalidOperationException("Tài khoản nhân viên hãy đăng nhập trong khu vực Quản lý.");
        }
        _status.Text = $"Đã đăng nhập: {_userName.Text.Trim()} · {StaffPermissions.DisplayName(_role ?? "Customer")} · SQL Server: {DirectDatabase.ConnectionCaption}";
        if (!_managementMode)
        {
            BuildGuestPages(); await LoadStoreBooksAsync();
        }
        else
        {
            _tabs.TabPages.Clear(); BuildStaffPages();
            if (StaffPermissions.CanAccess(_role, "dashboard")) await LoadDashboardAsync();
            if (StaffPermissions.CanAccess(_role, "books")) await LoadBooksAsync();
            if (StaffPermissions.CanAccess(_role, "customers")) await LoadCustomersAsync();
            if (StaffPermissions.CanAccess(_role, "orders")) await LoadOrdersAsync();
        }
    }

    private async Task RegisterCustomerAsync()
    {
        if (_managementMode) throw new InvalidOperationException("Đăng ký khách hàng chỉ có trong khu vực cửa hàng.");
        var values = EditDialog.Show(this, "Đăng ký tài khoản khách hàng", [
            ("user", "Tên đăng nhập", ""), ("email", "Email", ""), ("password", "Mật khẩu", ""),
            ("name", "Họ tên", ""), ("phone", "Số điện thoại", "")]);
        if (values is null) return;
        var result = await _services.Store.RegisterAsync(values["user"], values["email"], values["password"], values["name"], values["phone"]);
        if (!result.Succeeded) throw new InvalidOperationException(result.Message);
        _role = result.Role;
        _userName.Text = values["user"];
        _status.Text = "Đã tạo tài khoản và đăng nhập · SQL Server: " + DirectDatabase.ConnectionCaption;
        BuildGuestPages();
        await LoadStoreBooksAsync();
    }

    private async Task LogoutAsync()
    {
        await _services.Store.SignOutAsync();
        _role = null; _status.Text = "Đã đăng xuất · SQL Server: " + DirectDatabase.ConnectionCaption;
        _tabs.TabPages.Clear(); BuildGuestPages();
    }

    private async Task LoadStoreBooksAsync()
    {
        var books = await _services.Catalog.GetBooksAsync();
        if (_customerCatalogView is not null) _customerCatalogView.SetBooks(books);
        else _storeGrid.DataSource = books.ToList();
    }
    private async Task LoadCartAsync() => _cartGrid.DataSource = (await _services.Store.GetCartAsync()).ToList();
    private async Task LoadMyOrdersAsync() => _myOrdersGrid.DataSource = (await _services.Store.GetMyOrdersAsync()).ToList();

    private async Task AddSelectedToCartAsync()
    {
        var item = Selected<BookItem>(_storeGrid) ?? throw new InvalidOperationException("Chọn một quyển sách trước.");
        await _services.Store.SetCartQuantityAsync(item.Id, 1);
        await LoadCartAsync();
        MessageBox.Show(this, $"Đã thêm ‘{item.Title}’ vào giỏ.", "Trang Sách", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task CheckoutAsync()
    {
        var payment = _paymentChoice.SelectedItem as IPaymentOption ?? new CashOnDeliveryOption();
        var order = await _services.Store.CheckoutAsync(new CheckoutDetails(_shippingAddress.Text.Trim(), _receiverPhone.Text.Trim(), payment.ApiCode,
            string.IsNullOrWhiteSpace(_voucherCode.Text) ? null : _voucherCode.Text.Trim()));
        await LoadCartAsync();
        MessageBox.Show(this, $"Đặt hàng thành công: {order.Id}\n{payment.CustomerInstruction}", "Đặt hàng", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task LoadDashboardAsync()
    {
        var value = await _services.Manager.GetDashboardAsync();
        _dashboardGrid.DataSource = value.RecentOrders.ToList();
        _status.Text = $"Doanh thu tháng: {MoneyFormatter.ToVnd(value.RevenueThisMonth)} · Đơn hôm nay: {value.OrdersToday} · Chờ xử lý: {value.PendingOrders} · Sách sắp hết: {value.LowStockBooks}";
    }

    private async Task LoadBooksAsync() => _booksGrid.DataSource = (await _services.Manager.GetBooksAsync()).ToList();
    private async Task LoadCustomersAsync() => _customersGrid.DataSource = (await _services.Manager.GetCustomersAsync()).ToList();
    private async Task LoadEmployeesAsync() => _employeesGrid.DataSource = (await _services.Manager.GetEmployeesAsync()).ToList();
    private async Task LoadOrdersAsync() => _ordersGrid.DataSource = (await _services.Manager.GetOrdersAsync()).ToList();
    private async Task LoadReceiptsAsync() => _receiptsGrid.DataSource = (await _services.Manager.GetReceiptsAsync()).ToList();
    private async Task LoadSuppliersAsync() => _suppliersGrid.DataSource = (await _services.Manager.GetLookupsAsync()).Suppliers.ToList();
    private async Task LoadPromotionsAsync() => _promotionsGrid.DataSource = (await _services.Manager.GetPromotionsAsync()).ToList();
    private async Task LoadVouchersAsync() => _vouchersGrid.DataSource = (await _services.Manager.GetVouchersAsync()).ToList();
    private async Task LoadLogsAsync() => _logsGrid.DataSource = (await _services.Manager.GetAuditLogsAsync()).ToList();

    private async Task LoadReportAsync(int days)
    {
        if (_reportView is null) return;
        _reportView.SetLoading(days);
        var report = await _services.Manager.GetReportAsync(days);
        _reportView.ShowReport(report);
        _reportsLoaded = true;
    }

    private async Task EditBookAsync(string? id)
    {
        ManagerBookDetails? old = id is null ? null : await _services.Manager.GetBookDetailsAsync(id);
        var lookups = await _services.Manager.GetLookupsAsync();
        var values = EditDialog.Show(this, id is null ? "Thêm sách" : "Sửa sách", [
            ("title", "Tên sách", old?.Title ?? ""), ("price", "Giá bán", old?.SalePrice.ToString(CultureInfo.InvariantCulture) ?? "0"),
            ("unit", "Đơn vị", old?.Unit ?? "Quyển"), ("category", "Mã danh mục (" + string.Join(", ", lookups.Categories.Select(x => x.Id)) + ")", old?.CategoryId ?? lookups.Categories.FirstOrDefault()?.Id ?? ""),
            ("publisher", "Mã NXB (để trống nếu không có)", old?.PublisherId?.ToString() ?? ""),
            ("authors", "Mã tác giả, cách nhau dấu phẩy", string.Join(",", old?.AuthorIds ?? []))]);
        if (values is null) return;
        if (!decimal.TryParse(values["price"], NumberStyles.Number, CultureInfo.InvariantCulture, out var price)) throw new InvalidOperationException("Giá bán phải là số, nhập theo dạng 85000.");
        var authorIds = values["authors"].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToArray();
        int? publisherId = int.TryParse(values["publisher"], out var pub) ? pub : null;
        var book = new CreateManagerBook(values["title"], price, values["unit"], values["category"], publisherId, authorIds);
        if (id is null) await _services.Manager.CreateBookAsync(book); else await _services.Manager.UpdateBookAsync(id, book);
        await LoadBooksAsync();
    }

    private Task EditSelectedBookAsync()
    {
        var item = Selected<BookItem>(_booksGrid) ?? throw new InvalidOperationException("Chọn sách cần sửa.");
        return EditBookAsync(item.Id);
    }

    private async Task HideSelectedBookAsync()
    {
        var item = Selected<BookItem>(_booksGrid) ?? throw new InvalidOperationException("Chọn sách cần ẩn.");
        if (MessageBox.Show(this, $"Ẩn sách ‘{item.Title}’ khỏi cửa hàng?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        await _services.Manager.HideBookAsync(item.Id); await LoadBooksAsync();
    }

    private async Task EditCustomerAsync(ManagerCustomer? old)
    {
        var values = EditDialog.Show(this, old is null ? "Thêm khách hàng" : "Sửa khách hàng", [
            ("id", "Mã khách (trống để tự sinh)", old?.Id ?? ""), ("name", "Họ tên", old?.FullName ?? ""),
            ("phone", "Điện thoại", old?.PhoneNumber ?? ""), ("address", "Địa chỉ", old?.Address ?? ""),
            ("user", "Tên đăng nhập (tạo/cập nhật tài khoản)", old?.UserName ?? ""), ("email", "Email", old?.Email ?? ""),
            ("password", "Mật khẩu mới (bỏ trống nếu không đổi)", ""), ("status", "Trạng thái tài khoản", old?.AccountStatus ?? "Active")]);
        if (values is null) return;
        await _services.Manager.SaveCustomerAsync(old?.Id, new SaveCustomer(EmptyToNull(values["id"]), values["name"], EmptyToNull(values["phone"]), EmptyToNull(values["address"]), EmptyToNull(values["user"]), EmptyToNull(values["email"]), EmptyToNull(values["password"]), values["status"]));
        await LoadCustomersAsync();
    }

    private Task EditSelectedCustomerAsync()
    {
        var item = Selected<ManagerCustomer>(_customersGrid) ?? throw new InvalidOperationException("Chọn khách hàng cần sửa.");
        return EditCustomerAsync(item);
    }

    private async Task DeleteSelectedCustomerAsync()
    {
        var item = Selected<ManagerCustomer>(_customersGrid) ?? throw new InvalidOperationException("Chọn khách hàng cần xóa.");
        if (Confirm($"Xóa khách hàng {item.FullName}?")) { await _services.Manager.DeleteCustomerAsync(item.Id); await LoadCustomersAsync(); }
    }

    private async Task EditEmployeeAsync(ManagerEmployee? old)
    {
        var roles = await _services.Manager.GetEmployeeRolesAsync();
        var values = EditDialog.Show(this, old is null ? "Thêm nhân viên" : "Sửa nhân viên", [
            ("id", "Mã nhân viên (trống để tự sinh)", old?.Id ?? ""), ("last", "Họ", old?.LastName ?? ""), ("first", "Tên", old?.FirstName ?? ""),
            ("birth", "Ngày sinh (yyyy-MM-dd)", old?.BirthDate?.ToString("yyyy-MM-dd") ?? ""), ("address", "Địa chỉ", old?.Address ?? ""),
            ("salary", "Lương cơ bản", old?.BaseSalary.ToString(CultureInfo.InvariantCulture) ?? "0"), ("status", "Trạng thái nhân viên", old?.AccountStatus ?? "Active"),
            ("user", "Tên đăng nhập", old?.UserName ?? ""), ("email", "Email", old?.Email ?? ""), ("password", "Mật khẩu (bắt buộc khi tạo tài khoản)", ""),
            ("role", "Chức danh (" + string.Join(", ", roles.Select(x => x.Name)) + ")", old?.RoleName ?? "Sales"), ("loginStatus", "Trạng thái tài khoản", old?.LoginStatus ?? "Active")]);
        if (values is null) return;
        DateOnly? birth = DateOnly.TryParse(values["birth"], out var date) ? date : null;
        if (!decimal.TryParse(values["salary"], NumberStyles.Number, CultureInfo.InvariantCulture, out var salary)) throw new InvalidOperationException("Lương phải là số.");
        await _services.Manager.SaveEmployeeAsync(old?.Id, new SaveEmployee(EmptyToNull(values["id"]), values["last"], values["first"], birth, null, EmptyToNull(values["address"]), values["status"], salary, EmptyToNull(values["user"]), EmptyToNull(values["email"]), EmptyToNull(values["password"]), values["loginStatus"], values["role"]));
        await LoadEmployeesAsync();
    }

    private Task EditSelectedEmployeeAsync()
    {
        var item = Selected<ManagerEmployee>(_employeesGrid) ?? throw new InvalidOperationException("Chọn nhân viên cần sửa.");
        return EditEmployeeAsync(item);
    }

    private async Task DeleteSelectedEmployeeAsync()
    {
        var item = Selected<ManagerEmployee>(_employeesGrid) ?? throw new InvalidOperationException("Chọn nhân viên cần xóa.");
        if (Confirm($"Xóa nhân viên {item.LastName} {item.FirstName}?")) { await _services.Manager.DeleteEmployeeAsync(item.Id); await LoadEmployeesAsync(); }
    }

    private async Task ChangeOrderStatusAsync()
    {
        var item = Selected<ManagerOrderRow>(_ordersGrid) ?? throw new InvalidOperationException("Chọn đơn hàng.");
        var values = EditDialog.Show(this, "Cập nhật trạng thái", [("status", "Pending / Confirmed / Shipping / Completed / Cancelled", item.OrderStatus)]);
        if (values is null) return;
        await _services.Manager.UpdateOrderStatusAsync(item.Id, values["status"]); await LoadOrdersAsync();
    }

    private async Task CancelSelectedOrderAsync()
    {
        var item = Selected<ManagerOrderRow>(_ordersGrid) ?? throw new InvalidOperationException("Chọn đơn hàng cần hủy.");
        if (Confirm($"Hủy đơn {item.Id}?")) { await _services.Manager.DeleteOrderAsync(item.Id); await LoadOrdersAsync(); }
    }

    private async Task CancelSelectedReceiptAsync()
    {
        var item = Selected<ManagerReceipt>(_receiptsGrid) ?? throw new InvalidOperationException("Chọn phiếu nhập.");
        if (Confirm($"Đảo phiếu nhập {item.Id}?")) { await _services.Manager.CancelReceiptAsync(item.Id); await LoadReceiptsAsync(); }
    }

    private async Task LoadReferencesAsync()
    {
        _referenceGrid.DataSource = _referenceType.SelectedIndex switch
        {
            0 => (object)(await _services.Manager.GetCategoriesAsync()).ToList(),
            1 => (await _services.Manager.GetAuthorsAsync()).ToList(),
            _ => (await _services.Manager.GetPublishersAsync()).ToList()
        };
    }

    private async Task SaveReferenceAsync(bool edit)
    {
        switch (_referenceType.SelectedIndex)
        {
            case 0:
            {
                var old = edit ? Selected<ManagerCategory>(_referenceGrid) : null;
                if (edit && old is null) throw new InvalidOperationException("Chọn danh mục cần sửa.");
                var values = EditDialog.Show(this, "Danh mục", [("id", "Mã danh mục", old?.Id ?? ""), ("name", "Tên danh mục", old?.Name ?? "")]);
                if (values is null) return;
                await _services.Manager.SaveCategoryAsync(old?.Id, new SaveCategory(values["id"], values["name"])); break;
            }
            case 1:
            {
                var old = edit ? Selected<ManagerAuthor>(_referenceGrid) : null;
                if (edit && old is null) throw new InvalidOperationException("Chọn tác giả cần sửa.");
                var values = EditDialog.Show(this, "Tác giả", [("name", "Tên tác giả", old?.Name ?? ""), ("bio", "Tiểu sử", old?.Biography ?? "")]);
                if (values is null) return;
                await _services.Manager.SaveAuthorAsync(old?.Id, new SaveAuthor(values["name"], EmptyToNull(values["bio"]))); break;
            }
            default:
            {
                var old = edit ? Selected<ManagerPublisher>(_referenceGrid) : null;
                if (edit && old is null) throw new InvalidOperationException("Chọn nhà xuất bản cần sửa.");
                var values = EditDialog.Show(this, "Nhà xuất bản", [("name", "Tên nhà xuất bản", old?.Name ?? ""), ("address", "Địa chỉ", old?.Address ?? "")]);
                if (values is null) return;
                await _services.Manager.SavePublisherAsync(old?.Id, new SavePublisher(values["name"], EmptyToNull(values["address"]))); break;
            }
        }
        await LoadReferencesAsync();
    }

    private async Task DeleteReferenceAsync()
    {
        switch (_referenceType.SelectedIndex)
        {
            case 0:
                if (Selected<ManagerCategory>(_referenceGrid) is not { } category) throw new InvalidOperationException("Chọn danh mục.");
                if (Confirm($"Xóa danh mục {category.Name}?")) await _services.Manager.DeleteCategoryAsync(category.Id); break;
            case 1:
                if (Selected<ManagerAuthor>(_referenceGrid) is not { } author) throw new InvalidOperationException("Chọn tác giả.");
                if (Confirm($"Xóa tác giả {author.Name}?")) await _services.Manager.DeleteAuthorAsync(author.Id); break;
            default:
                if (Selected<ManagerPublisher>(_referenceGrid) is not { } publisher) throw new InvalidOperationException("Chọn nhà xuất bản.");
                if (Confirm($"Xóa nhà xuất bản {publisher.Name}?")) await _services.Manager.DeletePublisherAsync(publisher.Id); break;
        }
        await LoadReferencesAsync();
    }

    private async Task SaveSupplierAsync(SupplierOption? old)
    {
        var values = EditDialog.Show(this, old is null ? "Thêm nhà cung cấp" : "Sửa nhà cung cấp", [("name", "Tên", old?.Name ?? ""), ("phone", "Điện thoại", old?.PhoneNumber ?? "")]);
        if (values is null) return;
        if (old is null) await _services.Manager.CreateSupplierAsync(new CreateSupplier(values["name"], EmptyToNull(values["phone"])));
        else await _services.Manager.UpdateSupplierAsync(old.Id, new CreateSupplier(values["name"], EmptyToNull(values["phone"])));
        await LoadSuppliersAsync();
    }

    private Task EditSelectedSupplierAsync() => SaveSupplierAsync(Selected<SupplierOption>(_suppliersGrid) ?? throw new InvalidOperationException("Chọn nhà cung cấp."));

    private async Task DeleteSelectedSupplierAsync()
    {
        var item = Selected<SupplierOption>(_suppliersGrid) ?? throw new InvalidOperationException("Chọn nhà cung cấp.");
        if (Confirm($"Xóa nhà cung cấp {item.Name}?")) { await _services.Manager.DeleteSupplierAsync(item.Id); await LoadSuppliersAsync(); }
    }

    private async Task CreateReceiptAsync()
    {
        var lookups = await _services.Manager.GetLookupsAsync();
        var values = EditDialog.Show(this, "Tạo phiếu nhập một mặt hàng", [
            ("supplier", "Mã nhà cung cấp (" + string.Join(", ", lookups.Suppliers.Select(x => x.Id)) + ")", lookups.Suppliers.FirstOrDefault()?.Id ?? ""),
            ("book", "Mã sách", ""), ("quantity", "Số lượng", "1"), ("cost", "Giá nhập mỗi quyển", "0")]);
        if (values is null) return;
        if (!int.TryParse(values["quantity"], out var quantity) || quantity <= 0 || !decimal.TryParse(values["cost"], NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) || cost < 0)
            throw new InvalidOperationException("Số lượng phải dương và giá nhập phải là số không âm.");
        await _services.Manager.CreateReceiptAsync(new CreateReceipt(values["supplier"], [new CreateReceiptLine(values["book"], quantity, cost)]));
        await LoadReceiptsAsync();
    }

    private async Task CreateOrderAsync()
    {
        var values = EditDialog.Show(this, "Tạo đơn hàng", [("customer", "Mã khách hàng", ""), ("book", "Mã sách", ""),
            ("quantity", "Số lượng", "1"), ("address", "Địa chỉ giao", ""), ("phone", "Điện thoại nhận", ""), ("payment", "CashOnDelivery / BankTransfer", "CashOnDelivery")]);
        if (values is null) return;
        if (!int.TryParse(values["quantity"], out var quantity) || quantity <= 0) throw new InvalidOperationException("Số lượng phải là số dương.");
        await _services.Manager.CreateOrderAsync(new CreateAdminOrder(values["customer"], values["address"], values["phone"], values["payment"], [new CreateAdminOrderLine(values["book"], quantity)]));
        await LoadOrdersAsync();
    }

    private async Task EditSelectedOrderAsync()
    {
        var order = Selected<ManagerOrderRow>(_ordersGrid) ?? throw new InvalidOperationException("Chọn đơn hàng cần sửa.");
        var values = EditDialog.Show(this, "Sửa đơn hàng", [("address", "Địa chỉ giao", order.ShippingAddress), ("phone", "Điện thoại", order.ReceiverPhone), ("payment", "CashOnDelivery / BankTransfer", order.PaymentMethod)]);
        if (values is null) return;
        await _services.Manager.UpdateOrderAsync(order.Id, new UpdateAdminOrder(values["address"], values["phone"], values["payment"])); await LoadOrdersAsync();
    }

    private async Task CreatePromotionAsync()
    {
        var values = EditDialog.Show(this, "Tạo khuyến mãi", [("name", "Tên chương trình", ""),
            ("start", "Bắt đầu (yyyy-MM-dd)", DateTime.Today.ToString("yyyy-MM-dd")), ("end", "Kết thúc (yyyy-MM-dd)", DateTime.Today.AddDays(7).ToString("yyyy-MM-dd")),
            ("book", "Mã sách (bỏ trống nếu không giảm sách)", ""), ("bookRate", "% giảm sách", ""),
            ("minimum", "Đơn tối thiểu", ""), ("orderRate", "% giảm đơn", ""), ("voucher", "Mã voucher (bỏ trống nếu không tạo)", ""),
            ("voucherRate", "% giảm voucher", ""), ("maxUses", "Số lượt tối đa", "")]);
        if (values is null) return;
        if (!DateTime.TryParse(values["start"], out var start) || !DateTime.TryParse(values["end"], out var end) || end < start) throw new InvalidOperationException("Ngày khuyến mãi không hợp lệ.");
        decimal? dec(string key) => decimal.TryParse(values[key], NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;
        int? integer(string key) => int.TryParse(values[key], out var n) ? n : null;
        await _services.Manager.CreatePromotionAsync(new CreatePromotion(values["name"], start, end, EmptyToNull(values["book"]), dec("bookRate"), dec("minimum"), dec("orderRate"), EmptyToNull(values["voucher"]), dec("voucherRate"), integer("maxUses")));
        await LoadPromotionsAsync();
    }

    private async Task EditPromotionAsync()
    {
        var old = Selected<ManagerPromotion>(_promotionsGrid) ?? throw new InvalidOperationException("Chọn khuyến mãi.");
        var values = EditDialog.Show(this, "Sửa khuyến mãi", [("name", "Tên", old.Name), ("start", "Bắt đầu", old.StartsAt.ToString("yyyy-MM-dd")), ("end", "Kết thúc", old.EndsAt.ToString("yyyy-MM-dd"))]);
        if (values is null) return;
        if (!DateTime.TryParse(values["start"], out var start) || !DateTime.TryParse(values["end"], out var end)) throw new InvalidOperationException("Ngày không hợp lệ.");
        await _services.Manager.UpdatePromotionAsync(old.Id, new UpdatePromotionDetails(values["name"], start, end)); await LoadPromotionsAsync();
    }

    private async Task DeletePromotionAsync()
    {
        var item = Selected<ManagerPromotion>(_promotionsGrid) ?? throw new InvalidOperationException("Chọn khuyến mãi.");
        if (Confirm($"Xóa chương trình {item.Name}?")) { await _services.Manager.DeletePromotionAsync(item.Id); await LoadPromotionsAsync(); }
    }

    private async Task EditVoucherAsync()
    {
        var old = Selected<ManagerVoucher>(_vouchersGrid) ?? throw new InvalidOperationException("Chọn voucher.");
        var values = EditDialog.Show(this, "Cập nhật voucher", [("status", "Active / Inactive", old.Status), ("expires", "Hạn dùng (yyyy-MM-dd, trống = không hạn)", old.ExpiresAt?.ToString("yyyy-MM-dd") ?? ""), ("max", "Số lượt tối đa (trống = không giới hạn)", old.MaxUsageCount?.ToString() ?? "")]);
        if (values is null) return;
        DateTime? expires = DateTime.TryParse(values["expires"], out var date) ? date : null;
        int? max = int.TryParse(values["max"], out var count) ? count : null;
        await _services.Manager.UpdateVoucherAsync(old.Id, new UpdateVoucher(values["status"], expires, max)); await LoadVouchersAsync();
    }

    private async Task DeleteVoucherAsync()
    {
        var item = Selected<ManagerVoucher>(_vouchersGrid) ?? throw new InvalidOperationException("Chọn voucher.");
        if (Confirm($"Xóa voucher {item.Id}?")) { await _services.Manager.DeleteVoucherAsync(item.Id); await LoadVouchersAsync(); }
    }
    private async Task ExportBooksAsync(DataGridView grid)
    {
        var books = grid.DataSource as IEnumerable<BookItem>;
        if (books is null) throw new InvalidOperationException("Tải danh sách sách trước khi xuất.");
        using var dialog = new SaveFileDialog { Filter = "CSV UTF-8 (*.csv)|*.csv", FileName = "sach-trang-sach.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var rows = new List<(BookItem Book, ManagerBookDetails Details)>();
        foreach (var book in books) rows.Add((book, await _services.Manager.GetBookDetailsAsync(book.Id)));
        _bookFiles.Export(rows, dialog.FileName);
        MessageBox.Show(this, "Đã ghi danh sách sách ra file CSV dạng văn bản.", "Xuất dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task ImportBooksAsync()
    {
        using var dialog = new OpenFileDialog { Filter = "CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var rows = _bookFiles.ReadForImport(dialog.FileName);
        if (rows.Count == 0) throw new InvalidDataException("File không có dữ liệu sách.");
        var result = MessageBox.Show(this, $"Đọc được {rows.Count} dòng. Thêm các sách này vào database?\nMã danh mục, nhà xuất bản và tác giả phải hợp lệ.", "Nhập CSV", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;
        foreach (var row in rows)
            await _services.Manager.CreateBookAsync(new CreateManagerBook(row.Title, row.SalePrice, row.Unit, row.CategoryId, row.PublisherId, row.AuthorIds));
        await LoadBooksAsync();
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private bool Confirm(string question) => MessageBox.Show(this, question, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    private static T? Selected<T>(DataGridView grid) where T : class => grid.CurrentRow?.DataBoundItem as T;

    private async Task SafeAsync(Func<Task> action)
    {
        try { UseWaitCursor = true; await action(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Không thực hiện được", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { UseWaitCursor = false; }
    }

    private static void StyleCustomerGrid(DataGridView grid)
    {
        grid.BackgroundColor = Color.FromArgb(251, 250, 247);
        grid.GridColor = Color.FromArgb(233, 231, 223);
        grid.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 255);
        grid.DefaultCellStyle.ForeColor = Color.FromArgb(38, 45, 37);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 237, 227);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(38, 45, 37);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 244);
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(232, 237, 227);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(53, 86, 68);
        grid.RowTemplate.Height = 38;
    }

    private static TabPage CreateGridPage(string title, out DataGridView grid)
    {
        var page = new TabPage(title);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            ReadOnly = true,
            AutoGenerateColumns = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            RowHeadersVisible = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 36,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(230, 238, 247),
                ForeColor = Color.FromArgb(25, 45, 68),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 6, 0),
                WrapMode = DataGridViewTriState.False
            }
        };
        grid.RowTemplate.Height = 32;
        layout.Controls.Add(grid, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private static FlowLayoutPanel ToolBar(Control page)
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(8, 6, 8, 4),
            WrapContents = false,
            AutoScroll = true
        };
        if (page.Controls.OfType<TableLayoutPanel>().FirstOrDefault() is { } layout)
            layout.Controls.Add(bar, 0, 0);
        else
        {
            bar.Dock = DockStyle.Top;
            bar.Height = 48;
            page.Controls.Add(bar);
            bar.BringToFront();
        }
        return bar;
    }

    private static Button ActionButton(string text, Func<Task> action)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 32, Margin = new Padding(3), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(237, 242, 247) };
        button.Click += async (_, _) => { var form = button.FindForm(); try { if (form is not null) form.UseWaitCursor = true; await action(); } catch (Exception ex) { MessageBox.Show(form, ex.Message, "Không thực hiện được", MessageBoxButtons.OK, MessageBoxIcon.Error); } finally { if (form is not null) form.UseWaitCursor = false; } };
        return button;
    }

    private static Button HeaderButton(string text, int x, int y, int width) => new() { Text = text, Left = x, Top = y, Width = width, Height = 29, FlatStyle = FlatStyle.Flat };
}

internal static class EditDialog
{
    public static Dictionary<string, string>? Show(IWin32Window owner, string title, (string Key, string Label, string Value)[] fields)
    {
        using var dialog = new Form { Text = title, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false, Width = 540, Height = Math.Max(180, fields.Length * 58 + 90) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, RowCount = fields.Length + 1, AutoScroll = true };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var controls = new Dictionary<string, TextBox>();
        for (var i = 0; i < fields.Length; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.Controls.Add(new Label { Text = fields[i].Label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, i);
            var input = new TextBox { Text = fields[i].Value, Dock = DockStyle.Fill, UseSystemPasswordChar = fields[i].Key.Contains("password", StringComparison.OrdinalIgnoreCase) };
            controls[fields[i].Key] = input; layout.Controls.Add(input, 1, i);
        }
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var ok = new Button { Text = "Lưu", DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Width = 90 };
        buttons.Controls.Add(ok); buttons.Controls.Add(cancel); layout.Controls.Add(buttons, 0, fields.Length); layout.SetColumnSpan(buttons, 2);
        dialog.Controls.Add(layout); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
        if (dialog.ShowDialog(owner) != DialogResult.OK) return null;
        return controls.ToDictionary(pair => pair.Key, pair => pair.Value.Text.Trim());
    }
}







