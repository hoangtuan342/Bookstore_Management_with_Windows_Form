using TrangSach.Shared.DTO;
using TrangSach.Shared.GUI;
using System.Globalization;

namespace TrangSach.Management.GUI;

public partial class ManagementForm
{
    private static readonly List<(string Id, string Name)> OrderStatusOptions = new()
    {
        ("Pending", "Chờ xử lý (Pending)"),
        ("Confirmed", "Đã xác nhận (Confirmed)"),
        ("Shipping", "Đang giao hàng (Shipping)"),
        ("Completed", "Đã hoàn thành (Completed)"),
        ("Cancelled", "Đã hủy (Cancelled)")
    };

    private static readonly List<(string Id, string Name)> PaymentMethodOptions = new()
    {
        ("CashOnDelivery", "Thanh toán khi nhận hàng (COD)"),
        ("BankTransfer", "Chuyển khoản ngân hàng")
    };

    private void ChangeOrderStatus()
    {
        var item = Selected<ManagerOrderRowDto>(_ordersGrid) ?? throw new InvalidOperationException("Chọn đơn hàng.");
        var fields = new List<EditDialogField>
        {
            new("status", "Trạng thái đơn hàng", item.OrderStatus, OrderStatusOptions)
        };
        var values = EditDialog.Show(this, $"Cập nhật trạng thái đơn {item.Id}", fields);
        if (values is null) return;
        _services.Manager.UpdateOrderStatus(item.Id, values["status"]);
        LoadOrders(true);
    }

    private void PrintSelectedOrderInvoice()
    {
        var item = Selected<ManagerOrderRowDto>(_ordersGrid) ?? throw new InvalidOperationException("Vui lòng chọn đơn hàng cần in hóa đơn.");
        using var invoiceForm = new OrderInvoicePrintForm(item);
        invoiceForm.ShowDialog(this);
    }

    private void CancelSelectedOrder()
    {
        var item = Selected<ManagerOrderRowDto>(_ordersGrid) ?? throw new InvalidOperationException("Chọn đơn hàng cần hủy.");
        if (Confirm($"Hủy đơn {item.Id}?")) { _services.Manager.DeleteOrder(item.Id); LoadOrders(true); }
    }

    private void CancelSelectedReceipt()
    {
        var item = Selected<ManagerReceiptDto>(_receiptsGrid) ?? throw new InvalidOperationException("Chọn phiếu nhập.");
        if (Confirm($"Đảo phiếu nhập {item.Id}?")) { _services.Manager.CancelReceipt(item.Id); LoadReceipts(true); }
    }

    private void CreateReceipt()
    {
        var lookups = GetLookups();
        var books = _services.GetBooks();
        var employees = _services.GetEmployees();

        if (lookups.Suppliers.Count == 0) throw new InvalidOperationException("Chưa có nhà cung cấp nào. Hãy tạo nhà cung cấp trước.");
        if (books.Count == 0) throw new InvalidOperationException("Chưa có sách nào trong danh mục. Hãy tạo sách trước.");

        using var editor = new ReceiptEditorForm(lookups.Suppliers.ToList(), books, employees);
        if (editor.ShowDialog(this) == DialogResult.OK && editor.ResultReceiptDto != null)
        {
            _services.Manager.CreateReceipt(editor.ResultReceiptDto);
            LoadReceipts(true);
            LoadBooks(true);
        }
    }

    private void CreateOrder()
    {
        var customers = _services.GetCustomers();
        var employees = _services.GetEmployees();
        var books = _services.GetBooks();

        if (customers.Count == 0) throw new InvalidOperationException("Chưa có khách hàng nào. Hãy thêm khách hàng trước.");
        if (books.Count == 0) throw new InvalidOperationException("Chưa có sách nào trong danh mục.");

        using var editor = new OrderEditorForm(customers, employees, books);
        if (editor.ShowDialog(this) == DialogResult.OK && editor.ResultCreateDto != null)
        {
            _services.Manager.CreateOrder(editor.ResultCreateDto);
            LoadOrders(true);
            LoadBooks(true);
        }
    }

    private void EditSelectedOrder()
    {
        var order = Selected<ManagerOrderRowDto>(_ordersGrid) ?? throw new InvalidOperationException("Chọn đơn hàng cần sửa.");
        var customers = _services.GetCustomers();
        var employees = _services.GetEmployees();
        var books = _services.GetBooks();

        using var editor = new OrderEditorForm(customers, employees, books, order);
        if (editor.ShowDialog(this) == DialogResult.OK && editor.ResultUpdateDto != null)
        {
            _services.Manager.UpdateOrder(order.Id, editor.ResultUpdateDto);
            LoadOrders(true);
            LoadBooks(true);
        }
    }
}
