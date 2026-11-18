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

        var supplierOptions = lookups.Suppliers.Select(s => (s.Id, $"{s.Name} ({s.Id})")).ToList();
        var bookOptions = books.Select(b => (b.Id, $"{b.Title} (Tồn: {b.AvailableStock})")).ToList();

        if (supplierOptions.Count == 0) throw new InvalidOperationException("Chưa có nhà cung cấp nào. Hãy tạo nhà cung cấp trước.");
        if (bookOptions.Count == 0) throw new InvalidOperationException("Chưa có sách nào trong danh mục. Hãy tạo sách trước.");

        var fields = new List<EditDialogField>
        {
            new("supplier", "Nhà cung cấp", supplierOptions.First().Id, supplierOptions),
            new("book", "Sản phẩm sách", bookOptions.First().Id, bookOptions),
            new("quantity", "Số lượng nhập", "1"),
            new("cost", "Giá nhập mỗi quyển (VNĐ)", "0")
        };

        var values = EditDialog.Show(this, "Tạo phiếu nhập kho", fields);
        if (values is null) return;

        if (!int.TryParse(values["quantity"], out var quantity) || quantity <= 0)
            throw new InvalidOperationException("Số lượng nhập phải là số nguyên dương lớn hơn 0.");
        if (!decimal.TryParse(values["cost"], NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) || cost < 0)
            throw new InvalidOperationException("Giá nhập phải là số không âm.");

        _services.Manager.CreateReceipt(new CreateReceiptDto(values["supplier"], [new CreateReceiptLineDto(values["book"], quantity, cost)]));
        LoadReceipts(true);
        LoadBooks(true);
    }

    private void CreateOrder()
    {
        var customers = _services.GetCustomers();
        var books = _services.GetBooks();

        var customerOptions = customers.Select(c => (c.Id, $"{c.FullName} ({c.PhoneNumber ?? c.Id})")).ToList();
        var bookOptions = books.Where(b => b.AvailableStock > 0).Select(b => (b.Id, $"{b.Title} - {b.Price:N0}₫ (Tồn: {b.AvailableStock})")).ToList();

        if (customerOptions.Count == 0) throw new InvalidOperationException("Chưa có khách hàng nào. Hãy thêm khách hàng trước.");
        if (bookOptions.Count == 0) throw new InvalidOperationException("Hiện tại không có sách nào còn tồn kho để tạo đơn hàng.");

        var firstCustomer = customers.First();

        var fields = new List<EditDialogField>
        {
            new("customer", "Khách hàng", firstCustomer.Id, customerOptions),
            new("book", "Sách đặt mua", bookOptions.First().Id, bookOptions),
            new("quantity", "Số lượng mua", "1"),
            new("address", "Địa chỉ giao hàng", firstCustomer.Address ?? "Nhận tại quầy"),
            new("phone", "Điện thoại nhận hàng", firstCustomer.PhoneNumber ?? "0900000000"),
            new("payment", "Phương thức thanh toán", "CashOnDelivery", PaymentMethodOptions)
        };

        var values = EditDialog.Show(this, "Tạo đơn hàng tại quầy", fields);
        if (values is null) return;

        if (!int.TryParse(values["quantity"], out var quantity) || quantity <= 0)
            throw new InvalidOperationException("Số lượng đặt phải là số nguyên dương.");

        _services.Manager.CreateOrder(new CreateAdminOrderDto(values["customer"], values["address"], values["phone"], values["payment"], [new CreateAdminOrderLineDto(values["book"], quantity)]));
        LoadOrders(true);
        LoadBooks(true);
    }

    private void EditSelectedOrder()
    {
        var order = Selected<ManagerOrderRowDto>(_ordersGrid) ?? throw new InvalidOperationException("Chọn đơn hàng cần sửa.");
        var fields = new List<EditDialogField>
        {
            new("address", "Địa chỉ giao hàng", order.ShippingAddress),
            new("phone", "Điện thoại nhận hàng", order.ReceiverPhone),
            new("payment", "Phương thức thanh toán", order.PaymentMethod, PaymentMethodOptions)
        };
        var values = EditDialog.Show(this, $"Sửa đơn hàng {order.Id}", fields);
        if (values is null) return;
        _services.Manager.UpdateOrder(order.Id, new UpdateAdminOrderDto(values["address"], values["phone"], values["payment"]));
        LoadOrders(true);
    }
}
