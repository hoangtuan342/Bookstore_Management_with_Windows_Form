namespace BookStore.Shared.Security;

public static class StaffRoles
{
    public const string Manager = "Manager";
    public const string Sales = "Sales";
    public const string Inventory = "Inventory";
    public const string Catalog = "Catalog";
    public const string Marketing = "Marketing";
    public const string Accountant = "Accountant";
    public const string Customer = "Customer";

    public const string ManagerOrCatalog = Manager + "," + Catalog;
    public const string ProductReaders = Manager + "," + Catalog + "," + Sales + "," + Inventory + "," + Marketing;
    public const string ManagerCatalogInventory = Manager + "," + Catalog + "," + Inventory;
    public const string ManagerOrSales = Manager + "," + Sales;
    public const string ManagerOrInventory = Manager + "," + Inventory;
    public const string ManagerOrMarketing = Manager + "," + Marketing;
    public const string ManagerOrAccountant = Manager + "," + Accountant;
}

public static class StaffPermissions
{
    public static string HomePath(string? role) => role switch
    {
        StaffRoles.Manager => "/admin",
        StaffRoles.Sales => "/admin/orders",
        StaffRoles.Inventory => "/admin/inventory",
        StaffRoles.Catalog => "/admin/books",
        StaffRoles.Marketing => "/admin/promotions",
        StaffRoles.Accountant => "/admin/reports",
        _ => "/admin/login"
    };

    public static bool CanAccess(string? role, string area) => area == "profile"
        ? role is not null && role != StaffRoles.Customer
        : role switch
    {
        StaffRoles.Manager => true,
        StaffRoles.Sales => area is "customers" or "orders",
        StaffRoles.Inventory => area == "inventory",
        StaffRoles.Catalog => area is "books" or "records",
        StaffRoles.Marketing => area == "promotions",
        StaffRoles.Accountant => area is "dashboard" or "reports",
        _ => false
    };

    public static string DisplayName(string role) => role switch
    {
        StaffRoles.Manager => "Quản trị viên",
        StaffRoles.Sales => "Nhân viên bán hàng",
        StaffRoles.Inventory => "Nhân viên kho",
        StaffRoles.Catalog => "Nhân viên danh mục sách",
        StaffRoles.Marketing => "Nhân viên khuyến mãi",
        StaffRoles.Accountant => "Kế toán / báo cáo",
        StaffRoles.Customer => "Khách hàng",
        _ => role
    };

    public static IReadOnlyList<(string Name, string Label)> EmployeeRoles =>
    [
        (StaffRoles.Manager, DisplayName(StaffRoles.Manager)),
        (StaffRoles.Sales, DisplayName(StaffRoles.Sales)),
        (StaffRoles.Inventory, DisplayName(StaffRoles.Inventory)),
        (StaffRoles.Catalog, DisplayName(StaffRoles.Catalog)),
        (StaffRoles.Marketing, DisplayName(StaffRoles.Marketing)),
        (StaffRoles.Accountant, DisplayName(StaffRoles.Accountant))
    ];

    public static bool IsEmployeeRole(string? role) => EmployeeRoles.Any(item => item.Name == role);
}
