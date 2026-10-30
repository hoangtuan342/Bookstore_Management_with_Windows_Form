using BookStore.Shared.Security;
using TrangSach.Management.Views;

namespace TrangSach.Management.Forms;

public partial class ManagementForm
{
    private void BuildStaffPages()
    {
        if (_role is null || _role == StaffRoles.Customer) return;
        _reportView = null;

        if (StaffPermissions.CanAccess(_role, "dashboard"))
        {
            var page = new DashboardPageView();
            _dashboardGrid = page.Grid;
            AddManagementPage(page, new() { ["refresh"] = () => LoadDashboardAsync(true) });
        }
        if (StaffPermissions.CanAccess(_role, "books"))
        {
            var page = new BooksPageView();
            _booksGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadBooksAsync(true),
                ["add"] = () => EditBookAsync(null),
                ["edit"] = EditSelectedBookAsync,
                ["hide"] = HideSelectedBookAsync,
                ["import"] = ImportBooksAsync,
                ["export"] = () => ExportBooksAsync(_booksGrid)
            });
        }
        if (StaffPermissions.CanAccess(_role, "customers"))
        {
            var page = new CustomersPageView();
            _customersGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadCustomersAsync(true),
                ["add"] = () => EditCustomerAsync(null),
                ["edit"] = EditSelectedCustomerAsync,
                ["delete"] = DeleteSelectedCustomerAsync
            });
        }
        if (_role == StaffRoles.Manager)
        {
            var page = new EmployeesPageView();
            _employeesGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadEmployeesAsync(true),
                ["add"] = () => EditEmployeeAsync(null),
                ["edit"] = EditSelectedEmployeeAsync,
                ["delete"] = DeleteSelectedEmployeeAsync
            });
        }
        if (StaffPermissions.CanAccess(_role, "orders"))
        {
            var page = new OrdersPageView();
            _ordersGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadOrdersAsync(true),
                ["status"] = ChangeOrderStatusAsync,
                ["create"] = CreateOrderAsync,
                ["edit"] = EditSelectedOrderAsync,
                ["cancel"] = CancelSelectedOrderAsync
            });
        }
        if (StaffPermissions.CanAccess(_role, "inventory"))
        {
            var page = new ReceiptsPageView();
            _receiptsGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadReceiptsAsync(true),
                ["create"] = CreateReceiptAsync,
                ["cancel"] = CancelSelectedReceiptAsync
            });
        }
        if (StaffPermissions.CanAccess(_role, "reports"))
        {
            var page = new TabPage("Báo cáo");
            _reportView = new ReportDashboardView { Dock = DockStyle.Fill };
            _reportView.PeriodSelected += days => { _ = SafeAsync(() => LoadReportAsync(days)); };
            _reportView.DateRangeSelected += (from, to) => { _ = SafeAsync(() => LoadReportAsync(from, to)); };
            _reportView.ExportPdfRequested += report => SafeAction(() => ExportReportPdf(report));
            _reportView.ExportExcelRequested += report => SafeAction(() => ExportReportExcel(report));
            page.Controls.Add(_reportView);
            AddManagementTab(page);
        }
        if (StaffPermissions.CanAccess(_role, "records"))
        {
            var page = new ReferenceDataPageView();
            _referenceGrid = page.Grid;
            _referenceType = page.ReferenceType;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadReferencesAsync(true),
                ["add"] = () => SaveReferenceAsync(false),
                ["edit"] = () => SaveReferenceAsync(true),
                ["delete"] = DeleteReferenceAsync
            });
        }
        if (StaffPermissions.CanAccess(_role, "inventory"))
        {
            var page = new SuppliersPageView();
            _suppliersGrid = page.Grid;
            AddManagementPage(page, new()
            {
                ["load"] = () => LoadSuppliersAsync(true),
                ["add"] = () => SaveSupplierAsync(null),
                ["edit"] = EditSelectedSupplierAsync,
                ["delete"] = DeleteSelectedSupplierAsync
            });
        }
        if (StaffPermissions.CanAccess(_role, "promotions"))
        {
            var promotions = new PromotionsPageView();
            _promotionsGrid = promotions.Grid;
            AddManagementPage(promotions, new()
            {
                ["load"] = () => LoadPromotionsAsync(true),
                ["create"] = CreatePromotionAsync,
                ["edit"] = EditPromotionAsync,
                ["delete"] = DeletePromotionAsync
            });

            var vouchers = new VouchersPageView();
            _vouchersGrid = vouchers.Grid;
            AddManagementPage(vouchers, new()
            {
                ["load"] = () => LoadVouchersAsync(true),
                ["edit"] = EditVoucherAsync,
                ["delete"] = DeleteVoucherAsync
            });
        }
        if (_role == StaffRoles.Manager)
        {
            var page = new AuditLogsPageView();
            _logsGrid = page.Grid;
            AddManagementPage(page, new() { ["load"] = () => LoadLogsAsync(true) });
        }
    }

    private void AddManagementPage(ManagementGridPageView view, Dictionary<string, Func<Task>> actions)
    {
        view.ActionRequested += async (_, e) =>
        {
            if (actions.TryGetValue(e.Action, out var action))
                await SafeAsync(action);
        };

        var tab = new TabPage(view.PageTitle);
        tab.Controls.Add(view);
        AddManagementTab(tab);
    }

    private void AddManagementTab(TabPage tab)
    {
        _tabs.TabPages.Add(tab);
        var button = new Button
        {
            Text = tab.Text,
            Width = Math.Max(170, _navigation.ClientSize.Width - _navigation.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 2),
            Height = 40,
            Margin = new Padding(0, 0, 0, 5),
            Padding = new Padding(12, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(61, 78, 96)
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => _tabs.SelectedTab = tab;
        _navigationButtons[tab] = button;
        _navigationItems.Controls.Add(button);
        UpdateNavigationState();
    }

    private void UpdateNavigationState()
    {
        foreach (var (tab, button) in _navigationButtons)
        {
            var selected = _tabs.SelectedTab == tab;
            button.BackColor = selected ? Color.FromArgb(229, 240, 248) : Color.White;
            button.ForeColor = selected ? Color.FromArgb(25, 78, 112) : Color.FromArgb(61, 78, 96);
            button.FlatAppearance.BorderColor = selected ? Color.FromArgb(53, 130, 164) : Color.White;
        }
    }
}


