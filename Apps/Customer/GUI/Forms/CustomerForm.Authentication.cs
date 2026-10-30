using BookStore.Shared.Security;
using TrangSach.GUI.Common;

namespace TrangSach.Customer.Forms;

public partial class CustomerForm
{
    private async Task LoginAsync()
    {
        var userName = _userName.Text.Trim();
        var password = _password.Text;
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Nhập tên đăng nhập/email và mật khẩu.");
        var result = await _services.Store.LoginAsync(userName, password);
        if (!result.Succeeded) throw new InvalidOperationException(result.Message);
        if (result.Role != StaffRoles.Customer)
        {
            await _services.Store.SignOutAsync();
            throw new InvalidOperationException("Tài khoản nhân viên hãy đăng nhập trong khu vực Quản lý.");
        }
        _role = result.Role;
        _status.Text = $"Đã đăng nhập: {userName} · {StaffPermissions.DisplayName(_role)} · API: {_services.ApiAddress}";
        _password.Clear();
        BuildGuestPages();
        await LoadCustomerPagesAsync();
    }

    private async Task RegisterCustomerAsync()
    {
        var values = EditDialog.Show(this, "Đăng ký tài khoản khách hàng", [
            ("user", "Tên đăng nhập", ""), ("email", "Email", ""), ("password", "Mật khẩu", ""),
            ("name", "Họ tên", ""), ("phone", "Số điện thoại", "")]);
        if (values is null) return;
        var result = await _services.Store.RegisterAsync(values["user"], values["email"], values["password"], values["name"], values["phone"]);
        if (!result.Succeeded) throw new InvalidOperationException(result.Message);
        _role = result.Role;
        _userName.Text = values["user"];
        _status.Text = "Đã tạo tài khoản và đăng nhập · API: " + _services.ApiAddress;
        BuildGuestPages();
        await LoadCustomerPagesAsync();
    }

    private async Task LogoutAsync()
    {
        await _services.Store.SignOutAsync();
        _role = null;
        _status.Text = "Đã đăng xuất · API: " + _services.ApiAddress;
        _password.Clear();
        BuildGuestPages();
        await LoadStoreBooksAsync();
    }
}

