using TrangSach.Shared.DTO;
using System.Globalization;

namespace TrangSach.Management.GUI;

public partial class ManagementForm
{
    private void ChangeOrderStatus()
    {
        var item = Selected<ManagerOrderRowDto>(_ordersGrid) ?? throw new InvalidOperationException("Chọn đơn hàng.");
        var values = EditDialog.Show(this, "Cập nhật trạng thái", [("status", "Pending / Confirmed / Shipping / Completed / Cancelled", item.OrderStatus)]);
        if (values is null) return;
        _services.Manager.UpdateOrderStatus(item.Id, values["status"]); LoadOrders(true);
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
        var values = EditDialog.Show(this, "Tạo phiếu nhập một mặt hàng", [
            ("supplier", "Mã nhà cung cấp (" + string.Join(", ", lookups.Suppliers.Select(x => x.Id)) + ")", lookups.Suppliers.FirstOrDefault()?.Id ?? ""),
            ("book", "Mã sách", ""), ("quantity", "Số lượng", "1"), ("cost", "Giá nhập mỗi quyển", "0")]);
        if (values is null) return;
        if (!int.TryParse(values["quantity"], out var quantity) || quantity <= 0 || !decimal.TryParse(values["cost"], NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) || cost < 0)
            throw new InvalidOperationException("Số lượng phải dương và giá nhập phải là số không âm.");
        _services.Manager.CreateReceipt(new CreateReceiptDto(values["supplier"], [new CreateReceiptLineDto(values["book"], quantity, cost)]));
        LoadReceipts(true);
    }

    private void CreateOrder()
    {
        var values = EditDialog.Show(this, "Tạo đơn hàng", [("customer", "Mã khách hàng", ""), ("book", "Mã sách", ""),
            ("quantity", "Số lượng", "1"), ("address", "Địa chỉ giao", ""), ("phone", "Điện thoại nhận", ""), ("payment", "CashOnDelivery / BankTransfer", "CashOnDelivery")]);
        if (values is null) return;
        if (!int.TryParse(values["quantity"], out var quantity) || quantity <= 0) throw new InvalidOperationException("Số lượng phải là số dương.");
        _services.Manager.CreateOrder(new CreateAdminOrderDto(values["customer"], values["address"], values["phone"], values["payment"], [new CreateAdminOrderLineDto(values["book"], quantity)]));
        LoadOrders(true);
    }

    private void EditSelectedOrder()
    {
        var order = Selected<ManagerOrderRowDto>(_ordersGrid) ?? throw new InvalidOperationException("Chọn đơn hàng cần sửa.");
        var values = EditDialog.Show(this, "Sửa đơn hàng", [("address", "Địa chỉ giao", order.ShippingAddress), ("phone", "Điện thoại", order.ReceiverPhone), ("payment", "CashOnDelivery / BankTransfer", order.PaymentMethod)]);
        if (values is null) return;
        _services.Manager.UpdateOrder(order.Id, new UpdateAdminOrderDto(values["address"], values["phone"], values["payment"])); LoadOrders(true);
    }
}





