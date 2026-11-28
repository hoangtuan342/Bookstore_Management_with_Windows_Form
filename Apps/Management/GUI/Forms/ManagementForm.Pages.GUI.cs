using TrangSach.Shared.Security;
namespace TrangSach.Management.GUI;

public partial class ManagementForm
{
    private void BuildStaffPages()
    {
        if (_role is null || _role == StaffRoles.Customer) return;
        _reportView = null;
        _dashboardView = null;

        // 1. NHÓM PHÁP LÝ & KẾ TOÁN (Chứng từ phát sinh doanh thu & chi phí: Hóa đơn bán, Phiếu nhập kho, Báo cáo tài chính)
        if (StaffPermissions.CanAccess(_role, "orders"))
        {
            var page = new OrdersPageForm();
            _ordersGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadOrders(true),
                ["create"] = CreateOrder,
                ["print"] = PrintSelectedOrderInvoice,
                ["status"] = ChangeOrderStatus,
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
                ["print"] = PrintSelectedReceipt,
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
            _reportView.ExportExcelRequested += report => SafeAction(() => ExportReportExcel(report));
            _reportView.ExportCsvRequested += report => SafeAction(() => ExportReportCsv(report));
            page.Controls.Add(_reportView);
            AddManagementTab(page);
        }

        // 2. NHÓM ĐỐI TÁC & HÀNG HÓA (Chủ thể giao dịch & danh mục kinh doanh)
        if (StaffPermissions.CanAccess(_role, "books"))
        {
            var page = new BooksPageForm(bookId => _services.GetAuthorsByBookId(bookId));
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
        if (StaffPermissions.CanAccess(_role, "books"))
        {
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

        // 3. NHÓM CHÍNH SÁCH BÁN HÀNG (Khuyến mãi, Voucher)
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
                ["create"] = CreateVoucher,
                ["edit"] = EditVoucher,
                ["delete"] = DeleteVoucher
            });
        }

        // 4. NHÓM QUẢN TRỊ & GIÁM SÁT (Nhân viên, Audit log kiểm toán)
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

            var logPage = new AuditLogsPageForm();
            _logsGrid = logPage.Grid;
            AddManagementPage(logPage, new() { ["load"] = () => LoadLogs(true) });
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
                Width = Math.Max(180, _navigationItems.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2),
                Margin = new Padding(0, 0, 0, 8),
                Padding = Padding.Empty,
                BackColor = _navigation.BackColor
            };
            group.Controls.Add(new Label
            {
                Text = section.ToUpperInvariant(),
                UseMnemonic = false,
                Width = group.Width,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 2, 0, 0),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(139, 170, 187),
                BackColor = _navigation.BackColor
            });
            _navigationItems.Controls.Add(group);
            _navigationGroups[section] = group;
        }

        var (icon, title) = NavigationItemFormat(tab.Text);
        var button = new Button
        {
            Text = $"{icon,-3} {title}",
            UseMnemonic = false,
            Width = Math.Max(170, _navigationItems.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4),
            Height = 38,
            Margin = new Padding(2, 1, 2, 2),
            Padding = new Padding(10, 0, 6, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
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
        "Hóa đơn" or "Phiếu nhập" or "Báo cáo" => "Chứng từ & Tài chính",
        "Sách" or "Nhà cung cấp" or "Khách hàng" or "Tác giả" or "Danh mục tham chiếu" => "Hàng hóa & Đối tác",
        "Khuyến mãi" or "Voucher" => "Chính sách bán",
        _ => "Quản trị & Giám sát"
    };

    private static (string Icon, string Title) NavigationItemFormat(string pageTitle) => pageTitle switch
    {
        "Hóa đơn" => ("🧾", "Hóa đơn"),
        "Phiếu nhập" => ("📥", "Phiếu nhập kho"),
        "Báo cáo" => ("📊", "Báo cáo tài chính"),
        "Sách" => ("📚", "Sách"),
        "Nhà cung cấp" => ("🏢", "Nhà cung cấp"),
        "Khách hàng" => ("👥", "Khách hàng"),
        "Tác giả" => ("✍️", "Tác giả"),
        "Danh mục tham chiếu" => ("🏷️", "Danh mục chung"),
        "Khuyến mãi" => ("🏷️", "Khuyến mãi"),
        "Voucher" => ("🎟️", "Voucher"),
        "Nhân viên" => ("👤", "Nhân viên"),
        "Nhật ký hệ thống" => ("📜", "Nhật ký hệ thống"),
        _ => ("▪", pageTitle)
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




