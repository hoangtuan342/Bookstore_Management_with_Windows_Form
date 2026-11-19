using TrangSach.Shared.Security;
namespace TrangSach.Management.GUI;

public partial class ManagementForm
{
    private void BuildStaffPages()
    {
        if (_role is null || _role == StaffRoles.Customer) return;
        _reportView = null;

        if (StaffPermissions.CanAccess(_role, "dashboard"))
        {
            var page = new DashboardPageForm();
            _dashboardView = page;
            page.RefreshRequested += (_, _) => SafeAction(() => LoadDashboard(true));
            page.OpenReportRequested += (_, _) =>
            {
                var reportTab = _tabs.TabPages.Cast<TabPage>().FirstOrDefault(item => item.Text == "Báo cáo");
                if (reportTab is not null) _tabs.SelectedTab = reportTab;
            };
            var dashboardTab = new TabPage("Tổng quan");
            dashboardTab.Controls.Add(page);
            AddManagementTab(dashboardTab);
        }
        if (StaffPermissions.CanAccess(_role, "books"))
        {
            var page = new BooksPageForm();
            _booksGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadBooks(true),
                ["add"] = () => EditBook(null),
                ["edit"] = EditSelectedBook,
                ["hide"] = HideSelectedBook,
                ["import"] = ImportBooks,
                ["export"] = () => ExportBooks(_booksGrid)
            });

            var authorsPage = new AuthorsPageForm();
            _authorsGrid = authorsPage.Grid;
            AddManagementPage(authorsPage, new()
            {
                ["load"] = () => LoadAuthors(true),
                ["add"] = () => EditAuthor(null),
                ["edit"] = EditSelectedAuthor,
                ["delete"] = DeleteSelectedAuthor
            });
        }
        if (StaffPermissions.CanAccess(_role, "customers"))
        {
            var page = new CustomersPageForm();
            _customersGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadCustomers(true),
                ["add"] = () => EditCustomer(null),
                ["edit"] = EditSelectedCustomer,
                ["delete"] = DeleteSelectedCustomer
            });
        }
        if (_role == StaffRoles.Manager)
        {
            var page = new EmployeesPageForm();
            _employeesGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadEmployees(true),
                ["add"] = () => EditEmployee(null),
                ["edit"] = EditSelectedEmployee,
                ["delete"] = DeleteSelectedEmployee
            });
        }
        if (StaffPermissions.CanAccess(_role, "orders"))
        {
            var page = new OrdersPageForm();
            _ordersGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadOrders(true),
                ["status"] = ChangeOrderStatus,
                ["create"] = CreateOrder,
                ["edit"] = EditSelectedOrder,
                ["cancel"] = CancelSelectedOrder
            });
        }
        if (StaffPermissions.CanAccess(_role, "inventory"))
        {
            var page = new ReceiptsPageForm();
            _receiptsGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadReceipts(true),
                ["create"] = CreateReceipt,
                ["cancel"] = CancelSelectedReceipt
            });
        }
        if (StaffPermissions.CanAccess(_role, "reports"))
        {
            var page = new TabPage("Báo cáo");
            _reportView = new ReportDashboardForm { Dock = DockStyle.Fill };
            _reportView.PeriodSelected += days => SafeAction(() => LoadReport(days));
            _reportView.DateRangeSelected += (from, to) => SafeAction(() => LoadReport(from, to));
            _reportView.FullDailyTrendRequested += report => { using var detail = new RevenueTrendDetailForm(report); detail.ShowDialog(this); };
            _reportView.ExportPdfRequested += report => SafeAction(() => ExportReportPdf(report));
            _reportView.ExportExcelRequested += report => SafeAction(() => ExportReportExcel(report));
            _reportView.ExportCsvRequested += report => SafeAction(() => ExportReportCsv(report));
            page.Controls.Add(_reportView);
            AddManagementTab(page);
        }
        if (StaffPermissions.CanAccess(_role, "records"))
        {
            var page = new ReferenceDataPageForm();
            _referenceGrid = page.Grid;
            _referenceType = page.ReferenceType;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadReferences(true),
                ["add"] = () => SaveReference(false),
                ["edit"] = () => SaveReference(true),
                ["delete"] = DeleteReference
            });
        }
        if (StaffPermissions.CanAccess(_role, "inventory"))
        {
            var page = new SuppliersPageForm();
            _suppliersGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadSuppliers(true),
                ["add"] = () => SaveSupplier(null),
                ["edit"] = EditSelectedSupplier,
                ["delete"] = DeleteSelectedSupplier
            });
        }
        if (StaffPermissions.CanAccess(_role, "promotions"))
        {
            var promotions = new PromotionsPageForm();
            _promotionsGrid = promotions.Grid;
            AddManagementPage(promotions, new()
            {
                ["load"] = () => LoadPromotions(true),
                ["create"] = CreatePromotion,
                ["edit"] = EditPromotion,
                ["rules"] = EditPromotionRules,
                ["delete"] = DeletePromotion
            });

            var vouchers = new VouchersPageForm();
            _vouchersGrid = vouchers.Grid;
            AddManagementPage(vouchers, new()
            {
                ["load"] = () => LoadVouchers(true),
                ["edit"] = EditVoucher,
                ["delete"] = DeleteVoucher
            });
        }
        if (_role == StaffRoles.Manager)
        {
            var page = new AuditLogsPageForm();
            _logsGrid = page.Grid;
            AddManagementPage(page, new() { ["load"] = () => LoadLogs(true) });
        }
    }

    private void AddManagementPage(ManagementGridPageForm view, Dictionary<string, Action> actions)
    {
        view.ActionRequested += (_, e) =>
        {
            if (actions.TryGetValue(e.Action, out var action))
                SafeAction(action);
        };

        var tab = new TabPage(view.PageTitle);
        tab.Controls.Add(view);
        AddManagementTab(tab);
    }

    private void AddManagementTab(TabPage tab)
    {
        _tabs.TabPages.Add(tab);
        foreach (var childForm in tab.Controls.OfType<Form>()) childForm.Show();
        var section = NavigationSection(tab.Text);
        if (!_navigationGroups.TryGetValue(section, out var group))
        {
            group = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Width = Math.Max(160, _navigationItems.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2),
                Margin = new Padding(0, 0, 0, 9),
                Padding = Padding.Empty,
                BackColor = _navigation.BackColor
            };
            group.Controls.Add(new Label
            {
                Text = section.ToUpperInvariant(),
                Width = group.Width,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 2, 0, 0),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(139, 170, 187),
                BackColor = _navigation.BackColor
            });
            _navigationItems.Controls.Add(group);
            _navigationGroups[section] = group;
        }

        var button = new Button
        {
            Text = $"{NavigationIcon(tab.Text)}   {tab.Text}",
            Width = Math.Max(150, _navigationItems.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8),
            Height = 39,
            Margin = new Padding(2, 1, 2, 3),
            Padding = new Padding(9, 0, 5, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Symbol", 9, FontStyle.Regular),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = _navigation.BackColor,
            ForeColor = Color.FromArgb(214, 227, 235),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(43, 71, 91);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(48, 91, 102);
        button.Click += (_, _) => _tabs.SelectedTab = tab;
        _navigationButtons[tab] = button;
        group.Controls.Add(button);
        UpdateNavigationState();
    }

    private static string NavigationSection(string pageTitle) => pageTitle switch
    {
        "Tổng quan" => "Tổng quan",
        "Sách" or "Tác giả" or "Khách hàng" or "Danh mục tham chiếu" => "Danh mục",
        "Báo cáo" or "Nhật ký hệ thống" => "Phân tích",
        _ => "Vận hành"
    };

    private static string NavigationIcon(string pageTitle) => pageTitle switch
    {
        "Tổng quan" => "▦",
        "Sách" => "▤",
        "Tác giả" => "✍",
        "Khách hàng" => "♙",
        "Nhân viên" => "♟",
        "Đơn hàng" => "☷",
        "Phiếu nhập" => "▣",
        "Báo cáo" => "▥",
        "Danh mục tham chiếu" => "☰",
        "Nhà cung cấp" => "▱",
        "Khuyến mãi" => "%",
        "Voucher" => "◇",
        "Nhật ký hệ thống" => "◷",
        _ => "•"
    };

    private void UpdateNavigationState()
    {
        foreach (var (tab, button) in _navigationButtons)
        {
            var selected = _tabs.SelectedTab == tab;
            button.BackColor = selected ? Color.FromArgb(43, 91, 99) : _navigation.BackColor;
            button.ForeColor = selected ? Color.White : Color.FromArgb(214, 227, 235);
            button.FlatAppearance.BorderColor = selected ? Color.FromArgb(113, 194, 158) : _navigation.BackColor;
            button.FlatAppearance.BorderSize = selected ? 1 : 0;
        }
    }
}




