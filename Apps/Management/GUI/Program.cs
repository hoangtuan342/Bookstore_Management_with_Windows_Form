namespace TrangSach.Management;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
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
                "dashboard", "books", "customers", "employees", "orders", "receipts",
                "reports", "references", "suppliers", "promotions", "vouchers", "audit-logs"
            };
            if (!allowedPages.Contains(args[1]))
            {
                MessageBox.Show($"Không có trang quản lý '{args[1]}'.", "Trang không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new TrangSach.Management.Forms.ManagementForm(initialPage: args[1]));
            return;
        }

        Application.Run(new TrangSach.Management.Forms.ManagementForm());
    }
}
