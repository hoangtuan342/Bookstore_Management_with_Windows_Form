using BookStore.Shared.Models;
using BookStore.Shared.Security;
using TrangSach.Customer.Views;

namespace TrangSach.Customer.Forms;

public partial class CustomerForm
{
    private void BuildGuestPages()
    {
        _tabs.TabPages.Clear();
        var shop = new TabPage("Cửa hàng") { BackColor = Color.FromArgb(251, 250, 247) };
        _customerCatalogView = new CustomerCatalogView { Dock = DockStyle.Fill };
        _customerCatalogView.AddToCartRequested += async book => await SafeAsync(async () =>
        {
            var cart = await _services.Store.GetCartAsync();
            var quantity = cart.FirstOrDefault(x => x.BookId == book.Id)?.Quantity ?? 0;
            await _services.Store.SetCartQuantityAsync(book.Id, quantity + 1);
            _status.Text = $"Đã thêm ‘{book.Title}’ vào giỏ hàng.";
            await LoadCartAsync();
        });
        shop.Controls.Add(_customerCatalogView);
        _tabs.TabPages.Add(shop);
        var cart = CreateGridPage("Giỏ hàng", out _cartGrid);
        StyleCustomerGrid(_cartGrid);
        var cartTools = ToolBar(cart);
        _shippingAddress.Width = 220; _shippingAddress.PlaceholderText = "Địa chỉ giao hàng";
        _receiverPhone.Width = 145; _receiverPhone.PlaceholderText = "Số điện thoại";
        _voucherCode.Width = 120; _voucherCode.PlaceholderText = "Mã voucher";
        _paymentChoice.DropDownStyle = ComboBoxStyle.DropDownList;
        _paymentChoice.Width = 205;
        _paymentChoice.Items.Clear();
        _paymentChoice.Items.AddRange([new CashOnDeliveryOption(), new BankTransferOption()]);
        _paymentChoice.SelectedIndex = 0;
        cartTools.Controls.Add(_shippingAddress); cartTools.Controls.Add(_receiverPhone);
        cartTools.Controls.Add(_voucherCode); cartTools.Controls.Add(_paymentChoice);
        cartTools.Controls.Add(ActionButton("Tải giỏ", async () => await LoadCartAsync()));
        cartTools.Controls.Add(ActionButton("Đặt hàng", async () => await CheckoutAsync()));
        _tabs.TabPages.Add(cart);
        var orders = CreateGridPage("Đơn của tôi", out _myOrdersGrid);
        StyleCustomerGrid(_myOrdersGrid);
        ToolBar(orders).Controls.Add(ActionButton("Tải đơn hàng", async () => await LoadMyOrdersAsync()));
        _tabs.TabPages.Add(orders);
    }

    private async Task LoadStoreBooksAsync()
    {
        var books = await _services.Catalog.GetBooksAsync();
        _customerCatalogView?.SetBooks(books);
    }

    private async Task LoadCartAsync() => _cartGrid.DataSource = (await _services.Store.GetCartAsync()).ToList();

    private async Task LoadMyOrdersAsync() => _myOrdersGrid.DataSource = (await _services.Store.GetMyOrdersAsync()).ToList();

    private async Task LoadCustomerPagesAsync()
    {
        var loaders = new List<(string Page, Func<Task> Load)> { ("Cửa hàng", LoadStoreBooksAsync) };
        if (_role == StaffRoles.Customer)
        {
            loaders.Add(("Giỏ hàng", LoadCartAsync));
            loaders.Add(("Đơn của tôi", LoadMyOrdersAsync));
        }

        var failures = new List<string>();
        foreach (var (page, load) in loaders)
        {
            try { await load(); }
            catch (Exception ex) { failures.Add($"{page}: {ex.Message}"); }
        }
        if (failures.Count > 0)
            MessageBox.Show(this, "Không tải được dữ liệu cho một số trang:\n\n• " + string.Join("\n• ", failures),
                "Một số trang chưa tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private async Task CheckoutAsync()
    {
        var payment = _paymentChoice.SelectedItem as IPaymentOption ?? new CashOnDeliveryOption();
        var order = await _services.Store.CheckoutAsync(new CheckoutDetails(_shippingAddress.Text.Trim(), _receiverPhone.Text.Trim(), payment.ApiCode,
            string.IsNullOrWhiteSpace(_voucherCode.Text) ? null : _voucherCode.Text.Trim()));
        await LoadCartAsync();
        MessageBox.Show(this, $"Đặt hàng thành công: {order.Id}\n{payment.CustomerInstruction}", "Đặt hàng", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

