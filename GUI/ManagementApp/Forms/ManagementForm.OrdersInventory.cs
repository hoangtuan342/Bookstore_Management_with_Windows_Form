using BookStore.Shared.Models;
using System.Globalization;

namespace TrangSach.Management.Forms;

public partial class ManagementForm
{
    private async Task ChangeOrderStatusAsync()
    {
        var item = Selected<ManagerOrderRow>(_ordersGrid) ?? throw new InvalidOperationException("Chọn đơn hàng.");
        var values = EditDialog.Show(this, "Cập nhật trạng thái", [("status", "Pending / Confirmed / Shipping / Completed / Cancelled", item.OrderStatus)]);
        if (values is null) return;
        await _services.Manager.UpdateOrderStatusAsync(item.Id, values["status"]); await LoadOrdersAsync(true);
    }

    private async Task CancelSelectedOrderAsync()
    {
        var item = Selected<ManagerOrderRow>(_ordersGrid) ?? throw new InvalidOperationException("Chọn đơn hàng cần hủy.");
        if (Confirm($"Hủy đơn {item.Id}?")) { await _services.Manager.DeleteOrderAsync(item.Id); await LoadOrdersAsync(true); }
    }

    private async Task CancelSelectedReceiptAsync()
    {
        var item = Selected<ManagerReceipt>(_receiptsGrid) ?? throw new InvalidOperationException("Chọn phiếu nhập.");
        if (Confirm($"Đảo phiếu nhập {item.Id}?")) { await _services.Manager.CancelReceiptAsync(item.Id); await LoadReceiptsAsync(true); }
    }

    private async Task CreateReceiptAsync()
    {
        var lookups = await GetLookupsAsync();
        var values = EditDialog.Show(this, "Tạo phiếu nhập một mặt hàng", [
            ("supplier", "Mã nhà cung cấp (" + string.Join(", ", lookups.Suppliers.Select(x => x.Id)) + ")", lookups.Suppliers.FirstOrDefault()?.Id ?? ""),
            ("book", "Mã sách", ""), ("quantity", "Số lượng", "1"), ("cost", "Giá nhập mỗi quyển", "0")]);
        if (values is null) return;
        if (!int.TryParse(values["quantity"], out var quantity) || quantity <= 0 || !decimal.TryParse(values["cost"], NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) || cost < 0)
            throw new InvalidOperationException("Số lượng phải dương và giá nhập phải là số không âm.");
        await _services.Manager.CreateReceiptAsync(new CreateReceipt(values["supplier"], [new CreateReceiptLine(values["book"], quantity, cost)]));
        await LoadReceiptsAsync(true);
    }

    private async Task CreateOrderAsync()
    {
        var values = EditDialog.Show(this, "Tạo đơn hàng", [("customer", "Mã khách hàng", ""), ("book", "Mã sách", ""),
            ("quantity", "Số lượng", "1"), ("address", "Địa chỉ giao", ""), ("phone", "Điện thoại nhận", ""), ("payment", "CashOnDelivery / BankTransfer", "CashOnDelivery")]);
        if (values is null) return;
        if (!int.TryParse(values["quantity"], out var quantity) || quantity <= 0) throw new InvalidOperationException("Số lượng phải là số dương.");
        await _services.Manager.CreateOrderAsync(new CreateAdminOrder(values["customer"], values["address"], values["phone"], values["payment"], [new CreateAdminOrderLine(values["book"], quantity)]));
        await LoadOrdersAsync(true);
    }

    private async Task EditSelectedOrderAsync()
    {
        var order = Selected<ManagerOrderRow>(_ordersGrid) ?? throw new InvalidOperationException("Chọn đơn hàng cần sửa.");
        var values = EditDialog.Show(this, "Sửa đơn hàng", [("address", "Địa chỉ giao", order.ShippingAddress), ("phone", "Điện thoại", order.ReceiverPhone), ("payment", "CashOnDelivery / BankTransfer", order.PaymentMethod)]);
        if (values is null) return;
        await _services.Manager.UpdateOrderAsync(order.Id, new UpdateAdminOrder(values["address"], values["phone"], values["payment"])); await LoadOrdersAsync(true);
    }
}



