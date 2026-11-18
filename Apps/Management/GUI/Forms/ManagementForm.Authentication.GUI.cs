using TrangSach.Shared.Security;
using TrangSach.Shared.DTO;
using TrangSach.Shared.GUI;

namespace TrangSach.Management.GUI;

public partial class ManagementForm
{
    private void Login()
    {
        var userName = _loginView.UserName;
        var password = _loginView.Password;
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Nhập tên đăng nhập/email và mật khẩu.");
        var result = _services.Store.AdminLogin(userName, password);
        if (!result.Succeeded) throw new InvalidOperationException(result.Message);
        var role = _services.Store.Role ?? throw new InvalidOperationException("Tài khoản không có vai trò hợp lệ.");
        _role = role;
        if (role == StaffRoles.Customer)
        {
            _services.Store.SignOut();
            _role = null;
            throw new InvalidOperationException("Tài khoản này không có quyền truy cập khu vực quản lý. Hãy dùng tài khoản nhân viên.");
        }

        _status.Text = $"Đã đăng nhập: {userName} · {StaffPermissions.DisplayName(role)} · SQL Server: {_services.DatabaseConnectionCaption}";
        _tabs.TabPages.Clear();
        ClearNavigationItems();
        BuildStaffPages();
        LoadAccessibleManagementPages(role);
        SelectInitialManagementPage();
        _loginView.ShowError(string.Empty);
        _loginView.ClearPassword();
        _loginView.Visible = false;
        _tabs.Visible = true;
        _workspace.Visible = true;
        _header.Visible = true;
    }

    private void LoadAccessibleManagementPages(string role)
    {
        var loaders = new List<(string Page, Action Load)>();
        if (StaffPermissions.CanAccess(role, "dashboard")) loaders.Add(("Tổng quan", () => LoadDashboard()));
        if (StaffPermissions.CanAccess(role, "books"))
        {
            loaders.Add(("Sách", () => LoadBooks()));
            loaders.Add(("Tác giả", () => LoadAuthors()));
        }
        if (StaffPermissions.CanAccess(role, "customers")) loaders.Add(("Khách hàng", () => LoadCustomers()));
        if (role == StaffRoles.Manager) loaders.Add(("Nhân viên", () => LoadEmployees()));
        if (StaffPermissions.CanAccess(role, "orders")) loaders.Add(("Đơn hàng", () => LoadOrders()));
        if (StaffPermissions.CanAccess(role, "inventory"))
        {
            loaders.Add(("Phiếu nhập", () => LoadReceipts()));
            loaders.Add(("Nhà cung cấp", () => LoadSuppliers()));
        }
        if (StaffPermissions.CanAccess(role, "reports")) loaders.Add(("Báo cáo", () => LoadReport(1825)));
        if (StaffPermissions.CanAccess(role, "records")) loaders.Add(("Danh mục tham chiếu", () => LoadReferences()));
        if (StaffPermissions.CanAccess(role, "promotions"))
        {
            loaders.Add(("Khuyến mãi", () => LoadPromotions()));
            loaders.Add(("Voucher", () => LoadVouchers()));
        }
        if (role == StaffRoles.Manager) loaders.Add(("Nhật ký hệ thống", () => LoadLogs()));

        var failures = new List<string>();
        foreach (var (page, load) in loaders)
        {
            try { load(); }
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

    private void ManagementLogin()
    {
        _loginView.ShowError(string.Empty);
        _loginView.SetBusy(true);
        try { Login(); }
        catch (Exception ex) { _loginView.ShowError(ex.Message); }
        finally { _loginView.SetBusy(false); }
    }

    private void SetupFirstManager()
    {
        var values = EditDialog.Show(this, "Tạo tài khoản quản lý đầu tiên", [
            ("fullName", "Họ và tên", ""), ("email", "Email", ""),
            ("userName", "Tên đăng nhập", ""), ("password", "Mật khẩu (tối thiểu 8 ký tự)", "")]);
        if (values is null) return;

        _loginView.SetBusy(true);
        _loginView.ShowError(string.Empty);
        try
        {
            var command = new RegisterAccountCommandDto(values["userName"], values["email"], values["password"], values["fullName"], null);
            var result = _services.Store.SetupFirstManager(command);
            if (!result.Succeeded) throw new InvalidOperationException(result.Message);
            _services.Store.SignOut();
            _loginView.SetCanCreateManager(false);
            _loginView.SetCredentials(values["userName"], values["password"]);
            Login();
        }
        catch (Exception ex)
        {
            _loginView.ShowError(ex.Message);
            _loginView.SetCanCreateManager(!_services.Store.HasManagerAccount());
        }
        finally { _loginView.SetBusy(false); }
    }

    private void Logout()
    {
        _services.Store.SignOut();
        _role = null;
        _status.Text = "Đã đăng xuất · SQL Server: " + _services.DatabaseConnectionCaption;
        _tabs.TabPages.Clear();
        ClearNavigationItems();
        _workspace.Visible = false;
        _header.Visible = false;
        _loginView.ShowError(string.Empty);
        _loginView.ClearPassword();
        _loginView.SetConnectionCaption(_services.DatabaseConnectionCaption);
        _loginView.SetCanCreateManager(!_services.Store.HasManagerAccount());
        _loginView.Visible = true;
        _loginView.BringToFront();
    }

    private void ClearNavigationItems()
    {
        foreach (Control group in _navigationItems.Controls) group.Dispose();
        _navigationButtons.Clear();
        _navigationGroups.Clear();
        _navigationItems.Controls.Clear();
    }
}





