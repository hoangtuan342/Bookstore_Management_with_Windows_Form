using BookStore.Shared.Security;
using TrangSach.BUS.Models;
using TrangSach.GUI.Common;

namespace TrangSach.Management.Forms;

public partial class ManagementForm
{
    private void ConfigureDatabaseConnection()
    {
        var values = EditDialog.Show(this, "Kết nối SQL Server", [("connection", "Connection string SQL Server", _services.DatabaseConnectionString)]);
        if (values is null) return;
        try
        {
            _services.ValidateDatabaseConnectionString(values["connection"]);
            _services.SaveDatabaseConnectionString(values["connection"]);
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
        var userName = _loginView.UserName;
        var password = _loginView.Password;
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Nhập tên đăng nhập/email và mật khẩu.");
        var result = await _services.Store.AdminLoginAsync(userName, password);
        if (!result.Succeeded) throw new InvalidOperationException(result.Message);
        var role = result.Role ?? throw new InvalidOperationException("Tài khoản không có vai trò hợp lệ.");
        _role = role;
        if (role == StaffRoles.Customer)
        {
            await _services.Store.SignOutAsync();
            _role = null;
            throw new InvalidOperationException("Tài khoản này không có quyền truy cập khu vực quản lý. Hãy dùng tài khoản nhân viên.");
        }

        _status.Text = $"Đã đăng nhập: {userName} · {StaffPermissions.DisplayName(role)} · SQL Server: {_services.DatabaseConnectionCaption}";
        _tabs.TabPages.Clear();
        ClearNavigationItems();
        BuildStaffPages();
        await LoadAccessibleManagementPagesAsync(role);
        SelectInitialManagementPage();
        _loginView.ShowError(string.Empty);
        _loginView.ClearPassword();
        _loginView.Visible = false;
        _tabs.Visible = true;
        _workspace.Visible = true;
        _header.Visible = true;
    }

    private async Task LoadAccessibleManagementPagesAsync(string role)
    {
        var loaders = new List<(string Page, Func<Task> Load)>();
        if (StaffPermissions.CanAccess(role, "dashboard")) loaders.Add(("Tổng quan", LoadDashboardAsync));
        if (StaffPermissions.CanAccess(role, "books")) loaders.Add(("Sách", LoadBooksAsync));
        if (StaffPermissions.CanAccess(role, "customers")) loaders.Add(("Khách hàng", LoadCustomersAsync));
        if (role == StaffRoles.Manager) loaders.Add(("Nhân viên", LoadEmployeesAsync));
        if (StaffPermissions.CanAccess(role, "orders")) loaders.Add(("Đơn hàng", LoadOrdersAsync));
        if (StaffPermissions.CanAccess(role, "inventory"))
        {
            loaders.Add(("Phiếu nhập", LoadReceiptsAsync));
            loaders.Add(("Nhà cung cấp", LoadSuppliersAsync));
        }
        if (StaffPermissions.CanAccess(role, "reports")) loaders.Add(("Báo cáo", () => LoadReportAsync(30)));
        if (StaffPermissions.CanAccess(role, "records")) loaders.Add(("Danh mục tham chiếu", LoadReferencesAsync));
        if (StaffPermissions.CanAccess(role, "promotions"))
        {
            loaders.Add(("Khuyến mãi", LoadPromotionsAsync));
            loaders.Add(("Voucher", LoadVouchersAsync));
        }
        if (role == StaffRoles.Manager) loaders.Add(("Nhật ký hệ thống", LoadLogsAsync));

        var failures = new List<string>();
        foreach (var (page, load) in loaders)
        {
            try { await load(); }
            catch (Exception ex) { failures.Add($"{page}: {ex.Message}"); }
        }

        if (failures.Count > 0)
            MessageBox.Show(this, "Không tải được dữ liệu cho một số trang:\n\n• " + string.Join("\n• ", failures),
                "Một số trang chưa tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void SelectInitialManagementPage()
    {
        if (string.IsNullOrWhiteSpace(_initialPage)) return;
        var pageTitle = _initialPage.ToLowerInvariant() switch
        {
            "dashboard" => "Tổng quan", "books" => "Sách", "customers" => "Khách hàng",
            "employees" => "Nhân viên", "orders" => "Đơn hàng", "receipts" => "Phiếu nhập",
            "reports" => "Báo cáo", "references" => "Danh mục tham chiếu", "suppliers" => "Nhà cung cấp",
            "promotions" => "Khuyến mãi", "vouchers" => "Voucher", "audit-logs" => "Nhật ký hệ thống",
            _ => null
        };
        if (pageTitle is null) return;
        var tab = _tabs.TabPages.Cast<TabPage>().FirstOrDefault(item => item.Text == pageTitle);
        if (tab is null)
        {
            _status.Text += $" · Tài khoản hiện tại không có quyền mở trang {pageTitle}";
            return;
        }
        _tabs.SelectedTab = tab;
    }

    private async Task ManagementLoginAsync()
    {
        _loginView.ShowError(string.Empty);
        _loginView.SetBusy(true);
        try { await LoginAsync(); }
        catch (Exception ex) { _loginView.ShowError(ex.Message); }
        finally { _loginView.SetBusy(false); }
    }

    private async Task SetupFirstManagerAsync()
    {
        var values = EditDialog.Show(this, "Tạo tài khoản quản lý đầu tiên", [
            ("fullName", "Họ và tên", ""), ("email", "Email", ""),
            ("userName", "Tên đăng nhập", ""), ("password", "Mật khẩu (tối thiểu 8 ký tự)", "")]);
        if (values is null) return;

        _loginView.SetBusy(true);
        _loginView.ShowError(string.Empty);
        try
        {
            var command = new RegisterAccountCommand(values["userName"], values["email"], values["password"], values["fullName"], null);
            var result = await _services.Store.SetupFirstManagerAsync(command);
            if (!result.Succeeded) throw new InvalidOperationException(result.Message);
            await _services.Store.SignOutAsync();
            _loginView.SetCanCreateManager(false);
            _loginView.SetCredentials(values["userName"], values["password"]);
            await LoginAsync();
        }
        catch (Exception ex)
        {
            _loginView.ShowError(ex.Message);
            _loginView.SetCanCreateManager(!await _services.Store.HasManagerAccountAsync());
        }
        finally { _loginView.SetBusy(false); }
    }

    private async Task LogoutAsync()
    {
        await _services.Store.SignOutAsync();
        _role = null;
        _status.Text = "Đã đăng xuất · SQL Server: " + _services.DatabaseConnectionCaption;
        _tabs.TabPages.Clear();
        ClearNavigationItems();
        _workspace.Visible = false;
        _header.Visible = false;
        _loginView.ShowError(string.Empty);
        _loginView.ClearPassword();
        _loginView.SetConnectionCaption(_services.DatabaseConnectionCaption);
        _loginView.SetCanCreateManager(!await _services.Store.HasManagerAccountAsync());
        _loginView.Visible = true;
        _loginView.BringToFront();
    }

    private void ClearNavigationItems()
    {
        foreach (var button in _navigationButtons.Values) button.Dispose();
        _navigationButtons.Clear();
        _navigationItems.Controls.Clear();
    }
}


