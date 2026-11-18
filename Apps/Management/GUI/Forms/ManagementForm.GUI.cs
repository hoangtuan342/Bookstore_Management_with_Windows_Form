using TrangSach.Shared.Security;
using TrangSach.Shared.DTO;
using TrangSach.Shared.GUI;

namespace TrangSach.Management.GUI;

public partial class ManagementForm : Form
{
    [STAThread]
    public static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length > 0 && args[0].Equals("--page", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length < 2)
            {
                MessageBox.Show("Hãy chỉ định trang cần mở. Ví dụ: --page books", "Thiếu tên trang", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var allowedPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "dashboard", "books", "authors", "customers", "employees", "orders", "receipts",
                "reports", "references", "suppliers", "promotions", "vouchers", "audit-logs"
            };
            if (!allowedPages.Contains(args[1]))
            {
                MessageBox.Show($"Không có trang quản lý '{args[1]}'.", "Trang không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new ManagementForm(initialPage: args[1]));
            return;
        }

        Application.Run(new ManagementForm());
    }

    private readonly ManagementApplicationFacade _services = new();
    private readonly BookCsvFileService _bookFiles = new();
    private readonly BookExcelFileService _bookExcelFiles = new();
    private readonly string? _initialPage;
    private readonly Panel _header = new();
    private readonly Panel _workspace = new();
    private readonly Panel _navigation = new();
    private readonly FlowLayoutPanel _navigationItems = new();
    private readonly Dictionary<string, FlowLayoutPanel> _navigationGroups = new(StringComparer.Ordinal);
    private readonly ManagementLoginForm _loginView = new();
    private readonly Label _status = new();
    private readonly TabControl _tabs = new();
    private readonly Dictionary<TabPage, Button> _navigationButtons = [];
    private DashboardPageForm? _dashboardView;
    private DataGridView _booksGrid = new();
    private DataGridView _authorsGrid = new();
    private DataGridView _customersGrid = new();
    private DataGridView _employeesGrid = new();
    private DataGridView _ordersGrid = new();
    private DataGridView _receiptsGrid = new();
    private ReportDashboardForm? _reportView;
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
        Shown += (_, _) => SafeAction(() =>
        {
            _services.InitializeDatabase();
            _status.Text = "Đăng nhập nhân viên để mở khu vực quản lý · SQL Server: " + _services.DatabaseConnectionCaption;
            _loginView.SetConnectionCaption(_services.DatabaseConnectionCaption);
            _loginView.SetCanCreateManager(!_services.Store.HasManagerAccount());
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
        logout.Click += (_, _) => SafeAction(Logout);
        _header.Controls.Add(logout);

        _tabs.Dock = DockStyle.Fill;
        _tabs.Appearance = TabAppearance.FlatButtons;
        _tabs.SizeMode = TabSizeMode.Fixed;
        _tabs.ItemSize = new Size(1, 1);
        _tabs.Multiline = true;
        _tabs.Visible = false;
        _workspace.Dock = DockStyle.Fill;
        _workspace.BackColor = Color.FromArgb(244, 247, 250);
        _workspace.BackgroundImage = SharedBackgroundArtwork;
        _workspace.BackgroundImageLayout = ImageLayout.Stretch;
        _workspace.Visible = false;
        _navigation.Dock = DockStyle.Left;
        _navigation.Width = 218;
        _navigation.Padding = new Padding(12, 14, 12, 12);
        _navigation.BackColor = Color.FromArgb(25, 48, 68);
        _navigationItems.Dock = DockStyle.Fill;
        _navigationItems.FlowDirection = FlowDirection.TopDown;
        _navigationItems.WrapContents = false;
        _navigationItems.AutoScroll = true;
        _navigationItems.Padding = new Padding(0, 8, 0, 8);
        _navigationItems.BackColor = _navigation.BackColor;
        _navigation.Controls.Add(_navigationItems);
        _navigation.Controls.Add(new Label
        {
            Text = "ĐIỀU HƯỚNG",
            Dock = DockStyle.Top,
            Height = 36,
            Padding = new Padding(1, 5, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            ForeColor = Color.FromArgb(151, 180, 194),
            BackColor = _navigation.BackColor
        });
        _workspace.Controls.Add(_tabs);
        _workspace.Controls.Add(_navigation);
        Controls.Add(_workspace);
        Controls.Add(_header);

        _loginView.Dock = DockStyle.Fill;
        _header.Visible = false;
        _loginView.SetConnectionCaption(_services.DatabaseConnectionCaption);
        _loginView.LoginRequested += (_, _) => ManagementLogin();
        _loginView.SetupFirstManagerRequested += (_, _) => SetupFirstManager();
        Controls.Add(_loginView);
        _loginView.Show();
        _loginView.BringToFront();
    }
}




