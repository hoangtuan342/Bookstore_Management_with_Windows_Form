namespace TrangSach.Management.Views;

/// <summary>Simple WinForms sign-in screen for the store management application.</summary>
internal sealed class ManagementLoginView : UserControl
{
    private static readonly Color Navy = Color.FromArgb(29, 51, 75);
    private static readonly Color Teal = Color.FromArgb(25, 115, 112);
    private readonly TextBox _userName = new();
    private readonly TextBox _password = new();
    private readonly Label _error = new();
    private readonly Label _connection = new();
    private readonly Button _signIn = new();
    private readonly LinkLabel _setupManager = new();
    private readonly Panel _formPanel = new();

    public event EventHandler? LoginRequested;
    public event EventHandler? DatabaseSettingsRequested;
    public event EventHandler? SetupFirstManagerRequested;
    public string UserName => _userName.Text.Trim();
    public string Password => _password.Text;

    public ManagementLoginView()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(246, 248, 250);

        // Keep the login view to standard WinForms controls so it is easy to maintain and explain.
        var content = new Panel { Dock = DockStyle.Fill, BackColor = BackColor };
        Controls.Add(content);
        var brand = BuildBrandPanel();
        Controls.Add(brand);

        _formPanel.Size = new Size(440, 540);
        _formPanel.BackColor = BackColor;
        content.Controls.Add(_formPanel);
        content.Resize += (_, _) => CenterForm(content);
        CenterForm(content);
        BuildLoginForm();
    }

    public void SetConnectionCaption(string caption) => _connection.Text = $"KẾT NỐI: {caption}";
    public void ShowError(string message)
    {
        _error.Text = message;
        _error.Visible = !string.IsNullOrWhiteSpace(message);
    }
    public void SetBusy(bool busy)
    {
        _signIn.Enabled = !busy;
        _signIn.Text = busy ? "ĐANG KIỂM TRA..." : "ĐĂNG NHẬP";
        UseWaitCursor = busy;
    }
    public void ClearPassword() => _password.Clear();
    public void FocusUserName() => _userName.Focus();
    public void SetCredentials(string userName, string password)
    {
        _userName.Text = userName;
        _password.Text = password;
    }
    public void SetCanCreateManager(bool canCreate) => _setupManager.Visible = canCreate;

    private Panel BuildBrandPanel()
    {
        var panel = new Panel { Dock = DockStyle.Left, Width = 340, BackColor = Navy };
        panel.Controls.Add(new Label
        {
            Text = "TS", Location = new Point(36, 42), Size = new Size(52, 52),
            TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.White, BackColor = Teal
        });
        panel.Controls.Add(BrandLabel("TRANG SÁCH", 101, 43, 205, 28, 16, FontStyle.Bold, Color.White));
        panel.Controls.Add(BrandLabel("HỆ THỐNG QUẢN LÝ", 102, 72, 205, 20, 9, FontStyle.Bold, Color.FromArgb(194, 209, 222)));
        panel.Controls.Add(BrandLabel("KHU VỰC NHÂN VIÊN", 36, 188, 270, 22, 9, FontStyle.Bold, Color.FromArgb(113, 214, 204)));
        var firstHeadline = BrandLabel("Quản lý cửa", 34, 224, 280, 48, 25, FontStyle.Bold, Color.White);
        firstHeadline.AutoSize = true;
        panel.Controls.Add(firstHeadline);
        var secondHeadline = BrandLabel("hàng sách", 34, 276, 280, 48, 25, FontStyle.Bold, Color.White);
        secondHeadline.AutoSize = true;
        panel.Controls.Add(secondHeadline);
        panel.Controls.Add(BrandLabel("Sách · Đơn hàng · Kho · Báo cáo", 37, 333, 270, 28, 10, FontStyle.Regular, Color.FromArgb(218, 227, 235)));

        var separator = new Panel { Location = new Point(37, 397), Size = new Size(266, 1), BackColor = Color.FromArgb(83, 105, 125) };
        panel.Controls.Add(separator);
        _connection.SetBounds(37, 421, 266, 48);
        _connection.Font = new Font("Segoe UI", 8.5f);
        _connection.ForeColor = Color.FromArgb(218, 227, 235);
        _connection.AutoEllipsis = true;
        panel.Controls.Add(_connection);
        panel.Controls.Add(BrandLabel("TRANG SÁCH  |  QUẢN TRỊ NỘI BỘ", 37, 620, 270, 20, 8, FontStyle.Regular, Color.FromArgb(173, 192, 208)));
        return panel;
    }

    private void BuildLoginForm()
    {
        _formPanel.Controls.Add(FormLabel("ĐĂNG NHẬP NHÂN VIÊN", 0, 4, 440, 22, 9, FontStyle.Bold, Teal));
        _formPanel.Controls.Add(FormLabel("Chào mừng trở lại", 0, 42, 440, 42, 26, FontStyle.Bold, Navy));
        _formPanel.Controls.Add(FormLabel("Nhập tài khoản được cấp để mở khu vực quản lý.", 0, 91, 440, 28, 10, FontStyle.Regular, Color.FromArgb(103, 116, 130)));

        _formPanel.Controls.Add(FormLabel("Tên đăng nhập hoặc email", 0, 148, 440, 22, 9, FontStyle.Bold, Navy));
        StyleInput(_userName, "Nhập tên đăng nhập hoặc email");
        _userName.SetBounds(0, 174, 440, 38);
        _userName.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _password.Focus();
            }
        };
        _formPanel.Controls.Add(_userName);

        _formPanel.Controls.Add(FormLabel("Mật khẩu", 0, 235, 440, 22, 9, FontStyle.Bold, Navy));
        StyleInput(_password, "Nhập mật khẩu");
        _password.UseSystemPasswordChar = true;
        _password.SetBounds(0, 261, 440, 38);
        _password.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LoginRequested?.Invoke(this, EventArgs.Empty);
            }
        };
        _formPanel.Controls.Add(_password);

        var showPassword = new CheckBox
        {
            Text = "Hiển thị mật khẩu", AutoSize = true, Location = new Point(0, 311),
            Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(103, 116, 130)
        };
        showPassword.CheckedChanged += (_, _) => _password.UseSystemPasswordChar = !showPassword.Checked;
        _formPanel.Controls.Add(showPassword);

        _signIn.SetBounds(0, 352, 440, 46);
        _signIn.Text = "ĐĂNG NHẬP";
        _signIn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _signIn.ForeColor = Color.White;
        _signIn.BackColor = Navy;
        _signIn.FlatStyle = FlatStyle.Flat;
        _signIn.FlatAppearance.BorderSize = 0;
        _signIn.Click += (_, _) => LoginRequested?.Invoke(this, EventArgs.Empty);
        _formPanel.Controls.Add(_signIn);

        _error.SetBounds(0, 405, 440, 46);
        _error.Font = new Font("Segoe UI", 9);
        _error.ForeColor = Color.FromArgb(169, 54, 54);
        _error.Visible = false;
        _formPanel.Controls.Add(_error);

        _setupManager.Text = "Tạo tài khoản quản lý đầu tiên";
        _setupManager.AutoSize = true;
        _setupManager.Location = new Point(0, 462);
        _setupManager.LinkColor = Teal;
        _setupManager.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        _setupManager.Visible = false;
        _setupManager.LinkClicked += (_, _) => SetupFirstManagerRequested?.Invoke(this, EventArgs.Empty);
        _formPanel.Controls.Add(_setupManager);

        var databaseSettings = new LinkLabel
        {
            Text = "Cấu hình kết nối cơ sở dữ liệu", AutoSize = true,
            Location = new Point(0, 488), LinkColor = Teal,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        databaseSettings.LinkClicked += (_, _) => DatabaseSettingsRequested?.Invoke(this, EventArgs.Empty);
        _formPanel.Controls.Add(databaseSettings);
        _formPanel.Controls.Add(FormLabel("Mật khẩu do bạn tự đặt, tối thiểu 8 ký tự.", 0, 514, 440, 18, 8.5f, FontStyle.Regular, Color.FromArgb(103, 116, 130)));
    }

    private void CenterForm(Control parent) => _formPanel.Location = new Point(
        Math.Max(24, (parent.ClientSize.Width - _formPanel.Width) / 2),
        Math.Max(24, (parent.ClientSize.Height - _formPanel.Height) / 2));

    private static Label BrandLabel(string text, int x, int y, int width, int height, float size, FontStyle style, Color color) => new()
    {
        Text = text, Location = new Point(x, y), Size = new Size(width, height),
        Font = new Font("Segoe UI", size, style), ForeColor = color, BackColor = Color.Transparent
    };

    private static Label FormLabel(string text, int x, int y, int width, int height, float size, FontStyle style, Color color) => new()
    {
        Text = text, Location = new Point(x, y), Size = new Size(width, height),
        Font = new Font("Segoe UI", size, style), ForeColor = color, AutoSize = true
    };

    private static void StyleInput(TextBox input, string placeholder)
    {
        input.PlaceholderText = placeholder;
        input.Font = new Font("Segoe UI", 10.5f);
        input.ForeColor = Color.FromArgb(35, 48, 63);
        input.BackColor = Color.White;
        input.BorderStyle = BorderStyle.FixedSingle;
    }
}

