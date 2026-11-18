using System;
using System.Collections.Generic;
using System.Linq;

namespace TrangSach.Shared.Security
{
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
        public static string HomePath(string? role)
        {
            switch (role)
            {
                case StaffRoles.Manager:
                    return "/admin";
                case StaffRoles.Sales:
                    return "/admin/orders";
                case StaffRoles.Inventory:
                    return "/admin/inventory";
                case StaffRoles.Catalog:
                    return "/admin/books";
                case StaffRoles.Marketing:
                    return "/admin/promotions";
                case StaffRoles.Accountant:
                    return "/admin/reports";
                default:
                    return "/admin/login";
            }
        }

        public static bool CanAccess(string? role, string area)
        {
            if (area == "profile")
            {
                return role != null && role != StaffRoles.Customer;
            }

            switch (role)
            {
                case StaffRoles.Manager:
                    return true;
                case StaffRoles.Sales:
                    return area == "customers" || area == "orders";
                case StaffRoles.Inventory:
                    return area == "inventory";
                case StaffRoles.Catalog:
                    return area == "books" || area == "records";
                case StaffRoles.Marketing:
                    return area == "promotions";
                case StaffRoles.Accountant:
                    return area == "dashboard" || area == "reports";
                default:
                    return false;
            }
        }

        public static string DisplayName(string role)
        {
            switch (role)
            {
                case StaffRoles.Manager:
                    return "Quản trị viên";
                case StaffRoles.Sales:
                    return "Nhân viên bán hàng";
                case StaffRoles.Inventory:
                    return "Nhân viên kho";
                case StaffRoles.Catalog:
                    return "Nhân viên danh mục sách";
                case StaffRoles.Marketing:
                    return "Nhân viên khuyến mãi";
                case StaffRoles.Accountant:
                    return "Kế toán / báo cáo";
                case StaffRoles.Customer:
                    return "Khách hàng";
                default:
                    return role ?? string.Empty;
            }
        }

        public static List<(string Name, string Label)> EmployeeRoles
        {
            get
            {
                return new List<(string Name, string Label)>
                {
                    (StaffRoles.Manager, DisplayName(StaffRoles.Manager)),
                    (StaffRoles.Sales, DisplayName(StaffRoles.Sales)),
                    (StaffRoles.Inventory, DisplayName(StaffRoles.Inventory)),
                    (StaffRoles.Catalog, DisplayName(StaffRoles.Catalog)),
                    (StaffRoles.Marketing, DisplayName(StaffRoles.Marketing)),
                    (StaffRoles.Accountant, DisplayName(StaffRoles.Accountant))
                };
            }
        }

        public static bool IsEmployeeRole(string? role)
        {
            return EmployeeRoles.Any(item => item.Name == role);
        }
    }
}
