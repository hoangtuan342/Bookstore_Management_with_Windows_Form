using TrangSach.Customer.Models;
using TrangSach.Customer.Services;
using TrangSach.Customer.Views;
using TrangSach.GUI.Common;

namespace TrangSach.Customer.Forms;

public partial class CustomerForm : Form
{
    private readonly CustomerAppServices _services = new();
    private readonly TextBox _userName = new();
    private readonly TextBox _password = new();
    private readonly Label _status = new();
    private readonly TabControl _tabs = new();
    private CustomerCatalogView? _customerCatalogView;
    private DataGridView _cartGrid = new();
    private DataGridView _myOrdersGrid = new();
    private readonly TextBox _shippingAddress = new();
    private readonly TextBox _receiverPhone = new();
    private readonly TextBox _voucherCode = new();
    private readonly ComboBox _paymentChoice = new();
    private string? _role;

    public CustomerForm()
    {
        Text = "Trang Sách | Cửa hàng khách hàng";
        ClientSize = new Size(1180, 780);
        MinimumSize = new Size(1020, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BuildShell();
        BuildGuestPages();
        Shown += async (_, _) => await SafeAsync(async () =>
        {
            _status.Text = "Đang kết nối máy chủ cửa hàng: " + _services.ApiAddress;
            await LoadCustomerPagesAsync();
        });
        FormClosed += (_, _) => _services.Dispose();
    }

    private void BuildShell()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 122, Padding = new Padding(18, 12, 18, 8), BackColor = Color.FromArgb(53, 86, 68) };
        header.Controls.Add(new Label { Text = "TRANG SÁCH · CỬA HÀNG KHÁCH HÀNG", ForeColor = Color.White, Font = new Font("Segoe UI", 18, FontStyle.Bold), AutoSize = true, Location = new Point(18, 10) });
        _status.ForeColor = Color.Gainsboro;
        _status.AutoSize = true;
        _status.Location = new Point(20, 48);
        header.Controls.Add(_status);

        _userName.PlaceholderText = "Tên đăng nhập hoặc email";
        _userName.SetBounds(18, 78, 230, 28);
        _password.PlaceholderText = "Mật khẩu";
        _password.UseSystemPasswordChar = true;
        _password.SetBounds(258, 78, 180, 28);
        var login = HeaderButton("Đăng nhập", 448, 77, 105);
        login.Click += async (_, _) => await SafeAsync(LoginAsync);
        var register = HeaderButton("Đăng ký khách", 560, 77, 120);
        register.Click += async (_, _) => await SafeAsync(RegisterCustomerAsync);
        var logout = HeaderButton("Đăng xuất", 687, 77, 95);
        logout.Click += async (_, _) => await SafeAsync(LogoutAsync);
        header.Controls.AddRange([_userName, _password, login, register, logout]);

        _tabs.Dock = DockStyle.Fill;
        _tabs.Padding = new Point(16, 5);
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
        Controls.Add(_tabs);
        Controls.Add(header);
    }
}

