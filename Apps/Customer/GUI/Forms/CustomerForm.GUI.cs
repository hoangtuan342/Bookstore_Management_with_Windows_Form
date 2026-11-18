using TrangSach.Shared.GUI;

namespace TrangSach.Customer.GUI;

public partial class CustomerForm : Form
{
    private readonly TrangSach.Customer.BUS.CustomerApiGateway _store;
    private readonly string _apiAddress;
    private readonly TextBox _userName = new();
    private readonly TextBox _password = new();
    private readonly Label _status = new();
    private readonly TabControl _tabs = new();
    private readonly List<TrangSach.Shared.DTO.BookItemDto> _books = [];
    private readonly FlowLayoutPanel _catalogItems = new() { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, BackColor = Color.FromArgb(251, 250, 247), Padding = new Padding(0, 4, 0, 20) };
    private readonly FlowLayoutPanel _categoryChips = new() { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.FromArgb(251, 250, 247) };
    private readonly TextBox _catalogSearch = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), PlaceholderText = "⌕  Tìm tên sách, tác giả..." };
    private readonly Label _catalogCount = new() { AutoSize = true, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(115, 121, 111) };
    private DataGridView _cartGrid = new();
    private DataGridView _myOrdersGrid = new();
    private readonly Label _profileName = new() { AutoSize = true };
    private readonly Label _profileUser = new() { AutoSize = true };
    private readonly Label _profileEmail = new() { AutoSize = true };
    private readonly Label _profilePhone = new() { AutoSize = true };
    private readonly Label _profileAddress = new() { AutoSize = true };
    private string? _role;

    [STAThread]
    public static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new CustomerForm(CreateStoreGateway()));
    }

    private static TrangSach.Customer.BUS.CustomerApiGateway CreateStoreGateway()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.GUI.json");
        var baseAddress = "https://localhost:7215/";
        if (File.Exists(settingsPath))
        {
            using var settings = System.Text.Json.JsonDocument.Parse(File.ReadAllText(settingsPath));
            if (settings.RootElement.TryGetProperty("Api", out var api)
                && api.TryGetProperty("BaseAddress", out var value)
                && value.ValueKind == System.Text.Json.JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                baseAddress = value.GetString()!;
            }
        }

        if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("Cấu hình Api:BaseAddress trong appsettings.GUI.json không hợp lệ.");

        return new TrangSach.Customer.BUS.CustomerApiGateway(uri);
    }

    public CustomerForm(TrangSach.Customer.BUS.CustomerApiGateway store)
    {
        _store = store;
        _apiAddress = store.BaseAddress;
        Text = "Trang Sách | Cửa hàng khách hàng";
        ClientSize = new Size(1180, 780);
        MinimumSize = new Size(1020, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BuildShell();
        BuildGuestPages();
        Shown += async (_, _) => await SafeAsync(async () =>
        {
            _status.Text = "Đang kết nối máy chủ cửa hàng: " + _apiAddress;
            await LoadCustomerPagesAsync();
        });
        FormClosed += (_, _) => _store.Dispose();
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




