using BookStore.Shared.Security;
using TrangSach.BUS.Models;
using TrangSach.Management.Services;
using TrangSach.Management.Views;
using TrangSach.GUI.Common;

namespace TrangSach.Management.Forms;

public partial class ManagementForm : Form
{
    private readonly ManagementAppServices _services = new();
    private readonly BookCsvFileService _bookFiles = new();
    private readonly BookExcelFileService _bookExcelFiles = new();
    private readonly ReportExportService _reportFiles = new();
    private readonly string? _initialPage;
    private readonly Panel _header = new();
    private readonly Panel _workspace = new();
    private readonly Panel _navigation = new();
    private readonly FlowLayoutPanel _navigationItems = new();
    private readonly ManagementLoginView _loginView = new();
    private readonly Label _status = new();
    private readonly TabControl _tabs = new();
    private readonly Dictionary<TabPage, Button> _navigationButtons = [];
    private DataGridView _dashboardGrid = new();
    private DataGridView _booksGrid = new();
    private DataGridView _customersGrid = new();
    private DataGridView _employeesGrid = new();
    private DataGridView _ordersGrid = new();
    private DataGridView _receiptsGrid = new();
    private ReportDashboardView? _reportView;
    private DataGridView _logsGrid = new();
    private DataGridView _referenceGrid = new();
    private DataGridView _suppliersGrid = new();
    private DataGridView _promotionsGrid = new();
    private DataGridView _vouchersGrid = new();
    private ComboBox _referenceType = new();
    private string? _role;

    public ManagementForm(string? initialPage = null)
    {
        _initialPage = initialPage;
        Text = "Trang Sách | Quản lý";
        ClientSize = new Size(1280, 820);
        MinimumSize = new Size(1080, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BuildShell();
        Shown += async (_, _) => await SafeAsync(async () =>
        {
            await _services.InitializeDatabaseAsync();
            _status.Text = "Đăng nhập nhân viên để mở khu vực quản lý · SQL Server: " + _services.DatabaseConnectionCaption;
            _loginView.SetConnectionCaption(_services.DatabaseConnectionCaption);
            _loginView.SetCanCreateManager(!await _services.Store.HasManagerAccountAsync());
            _loginView.FocusUserName();
        });
        FormClosed += (_, _) => _services.Dispose();
        _tabs.SelectedIndexChanged += (_, _) => UpdateNavigationState();
    }

    private void BuildShell()
    {
        _header.Dock = DockStyle.Top;
        _header.Height = 86;
        _header.Padding = new Padding(20, 8, 20, 8);
        _header.BackColor = Color.FromArgb(25, 45, 68);
        _header.Controls.Add(new Label
        {
            Text = "TRANG SÁCH",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 11)
        });
        _header.Controls.Add(new Label
        {
            Text = "QUẢN TRỊ CỬA HÀNG",
            ForeColor = Color.FromArgb(159, 190, 210),
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(22, 47)
        });
        _status.AutoEllipsis = true;
        _status.ForeColor = Color.Gainsboro;
        _status.Location = new Point(300, 28);
        _status.Size = new Size(465, 32);
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _header.Controls.Add(_status);

        var logout = HeaderButton("Đăng xuất", 0, 26, 108);
        logout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        logout.Left = ClientSize.Width - 20 - logout.Width;
        logout.BackColor = Color.FromArgb(41, 64, 86);
        logout.ForeColor = Color.White;
        logout.FlatAppearance.BorderColor = Color.FromArgb(78, 101, 123);
        logout.Click += async (_, _) => await SafeAsync(LogoutAsync);
        var database = HeaderButton("Cơ sở dữ liệu", 0, 26, 142);
        database.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        database.Left = logout.Left - database.Width - 10;
        database.BackColor = Color.FromArgb(41, 64, 86);
        database.ForeColor = Color.White;
        database.FlatAppearance.BorderColor = Color.FromArgb(78, 101, 123);
        database.Click += (_, _) => ConfigureDatabaseConnection();
        _header.Controls.AddRange([logout, database]);

        _tabs.Dock = DockStyle.Fill;
        _tabs.Appearance = TabAppearance.FlatButtons;
        _tabs.SizeMode = TabSizeMode.Fixed;
        _tabs.ItemSize = new Size(1, 1);
        _tabs.Multiline = true;
        _tabs.Visible = false;
        _workspace.Dock = DockStyle.Fill;
        _workspace.BackColor = Color.FromArgb(244, 247, 250);
        _workspace.Visible = false;
        _navigation.Dock = DockStyle.Left;
        _navigation.Width = 218;
        _navigation.Padding = new Padding(12, 14, 12, 12);
        _navigation.BackColor = Color.White;
        _navigationItems.Dock = DockStyle.Fill;
        _navigationItems.FlowDirection = FlowDirection.TopDown;
        _navigationItems.WrapContents = false;
        _navigationItems.AutoScroll = true;
        _navigationItems.Padding = new Padding(0, 8, 0, 0);
        _navigationItems.BackColor = Color.White;
        _navigation.Controls.Add(_navigationItems);
        _navigation.Controls.Add(new Label
        {
            Text = "KHU VỰC LÀM VIỆC",
            Dock = DockStyle.Top,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            ForeColor = Color.FromArgb(112, 128, 143)
        });
        _workspace.Controls.Add(_tabs);
        _workspace.Controls.Add(_navigation);
        Controls.Add(_workspace);
        Controls.Add(_header);

        _loginView.Dock = DockStyle.Fill;
        _header.Visible = false;
        _loginView.SetConnectionCaption(_services.DatabaseConnectionCaption);
        _loginView.LoginRequested += async (_, _) => await ManagementLoginAsync();
        _loginView.DatabaseSettingsRequested += (_, _) => ConfigureDatabaseConnection();
        _loginView.SetupFirstManagerRequested += async (_, _) => await SetupFirstManagerAsync();
        Controls.Add(_loginView);
        _loginView.BringToFront();
    }
}

