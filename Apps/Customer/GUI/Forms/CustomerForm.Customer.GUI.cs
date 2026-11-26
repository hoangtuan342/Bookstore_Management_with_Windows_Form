using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;
namespace TrangSach.Customer.GUI;

public partial class CustomerForm
{
    private void BuildGuestPages()
    {
        _tabs.TabPages.Clear();
        var shop = new TabPage("Cửa hàng") { BackColor = Color.FromArgb(251, 250, 247) };
        shop.Controls.Add(BuildCatalogPage());
        _tabs.TabPages.Add(shop);
        var cart = CreateGridPage("Giỏ hàng", out _cartGrid);
        StyleCustomerGrid(_cartGrid);
        var cartTools = ToolBar(cart);
        cartTools.Controls.Add(ActionButton("Tải giỏ", async () => await LoadCartAsync()));
        cartTools.Controls.Add(ActionButton("Thanh toán", CheckoutAsync));
        _tabs.TabPages.Add(cart);
        var orders = CreateGridPage("Đơn của tôi", out _myOrdersGrid);
        StyleCustomerGrid(_myOrdersGrid);
        _myOrdersGrid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.Value is null) return;
            var colName = _myOrdersGrid.Columns[e.ColumnIndex].DataPropertyName;
            if (colName == nameof(TrangSach.Shared.DTO.StoreOrderDto.Status))
            {
                e.Value = OrderStatusFormatter.FormatOrderStatus(e.Value.ToString());
                e.FormattingApplied = true;
            }
            else if (colName == nameof(TrangSach.Shared.DTO.StoreOrderDto.PaymentStatus))
            {
                e.Value = OrderStatusFormatter.FormatPaymentStatus(e.Value.ToString());
                e.FormattingApplied = true;
            }
            else if (colName == nameof(TrangSach.Shared.DTO.StoreOrderDto.PaymentMethod))
            {
                e.Value = OrderStatusFormatter.FormatPaymentMethod(e.Value.ToString());
                e.FormattingApplied = true;
            }
        };
        _myOrdersGrid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0) await SafeAsync(OpenSelectedOrderAsync);
        };
        ToolBar(orders).Controls.AddRange([
            ActionButton("Tải hóa đơn", async () => await LoadMyOrdersAsync()),
            ActionButton("Xem chi tiết", OpenSelectedOrderAsync),
            ActionButton("Hủy đơn", CancelSelectedOrderAsync)]);
        _tabs.TabPages.Add(orders);
        var profile = new TabPage("Hồ sơ") { BackColor = Color.FromArgb(251, 250, 247), Padding = new Padding(24) };
        var profilePanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(18), BackColor = Color.White };
        profilePanel.Controls.Add(new Label { Text = "HỒ SƠ CÁ NHÂN", AutoSize = true, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(53, 86, 68), Margin = new Padding(3, 3, 3, 16) });
        profilePanel.Controls.AddRange([_profileName, _profileUser, _profileEmail, _profilePhone, _profileAddress]);
        foreach (var label in new[] { _profileName, _profileUser, _profileEmail, _profilePhone, _profileAddress }) label.Margin = new Padding(4, 6, 4, 6);
        profile.Controls.Add(profilePanel);
        _tabs.TabPages.Add(profile);
        if (_role != StaffRoles.Customer) _profileName.Text = "Đăng nhập để xem thông tin tài khoản.";
    }

    private async Task LoadStoreBooksAsync()
    {
        _books.Clear();
        _books.AddRange(await _store.GetBooksAsync());
        BuildCategoryChips();
        RenderCatalog();
    }

    private async Task LoadCartAsync() => _cartGrid.DataSource = (await _store.GetCartAsync()).ToList();

    private async Task LoadMyOrdersAsync()
    {
        _myOrdersGrid.DataSource = (await _store.GetMyOrdersAsync()).ToList();
        if (_myOrdersGrid.Columns[nameof(TrangSach.Shared.DTO.StoreOrderDto.Lines)] is { } lines) lines.Visible = false;
        if (_myOrdersGrid.Columns[nameof(TrangSach.Shared.DTO.StoreOrderDto.Id)] is { } id) id.HeaderText = "Mã đơn";
        if (_myOrdersGrid.Columns[nameof(TrangSach.Shared.DTO.StoreOrderDto.CreatedAt)] is { } created)
        {
            created.HeaderText = "Ngày đặt";
            created.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        }
        if (_myOrdersGrid.Columns[nameof(TrangSach.Shared.DTO.StoreOrderDto.Status)] is { } status) status.HeaderText = "Trạng thái";
        if (_myOrdersGrid.Columns[nameof(TrangSach.Shared.DTO.StoreOrderDto.PaymentStatus)] is { } payment) payment.HeaderText = "Thanh toán";
        if (_myOrdersGrid.Columns[nameof(TrangSach.Shared.DTO.StoreOrderDto.PaymentMethod)] is { } method) method.HeaderText = "Phương thức";
        if (_myOrdersGrid.Columns[nameof(TrangSach.Shared.DTO.StoreOrderDto.TotalAmount)] is { } total)
        {
            total.HeaderText = "Tổng tiền";
            total.DefaultCellStyle.Format = "N0";
        }
        if (_myOrdersGrid.Columns[nameof(TrangSach.Shared.DTO.StoreOrderDto.DiscountAmount)] is { } discount)
        {
            discount.HeaderText = "Giảm giá";
            discount.DefaultCellStyle.Format = "N0";
        }
    }

    private async Task CancelSelectedOrderAsync()
    {
        if (_myOrdersGrid.CurrentRow?.DataBoundItem is not StoreOrderDto order) throw new InvalidOperationException("Chọn hóa đơn cần hủy.");
        if (order.Status is not ("Pending" or "Confirmed")) throw new InvalidOperationException("Chỉ có thể hủy đơn đang chờ hoặc đã xác nhận.");
        if (MessageBox.Show(this, $"Hủy đơn {order.Id}?", "Xác nhận hủy đơn", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        await _store.CancelOrderAsync(order.Id);
        await LoadMyOrdersAsync();
    }

    private async Task LoadProfileAsync()
    {
        var profile = await _store.GetProfileAsync() ?? throw new InvalidOperationException("Máy chủ không trả về hồ sơ tài khoản.");
        _profileName.Text = "Họ và tên: " + profile.FullName;
        _profileUser.Text = "Tên đăng nhập: " + profile.UserName;
        _profileEmail.Text = "Email: " + profile.Email;
        _profilePhone.Text = "Điện thoại: " + (profile.PhoneNumber ?? "Chưa cập nhật");
        _profileAddress.Text = "Địa chỉ: " + (profile.Address ?? "Chưa cập nhật");
    }

    private async Task LoadCustomerPagesAsync()
    {
        var loaders = new List<(string Page, Func<Task> Load)> { ("Cửa hàng", LoadStoreBooksAsync) };
        if (_role == StaffRoles.Customer)
        {
            loaders.Add(("Giỏ hàng", LoadCartAsync));
            loaders.Add(("Đơn của tôi", LoadMyOrdersAsync));
            loaders.Add(("Hồ sơ", LoadProfileAsync));
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
        var cart = await _store.GetCartAsync();
        if (cart.Count == 0) throw new InvalidOperationException("Giỏ hàng đang trống.");
        using var checkout = new CustomerCheckoutForm(cart);
        if (checkout.ShowDialog(this) != DialogResult.OK || checkout.Details is null) return;
        var order = await _store.CheckoutAsync(checkout.Details);
        await LoadCartAsync();
        await LoadMyOrdersAsync();
        using var details = new CustomerOrderDetailsForm(order);
        details.ShowDialog(this);
    }

    private async Task OpenSelectedOrderAsync()
    {
        if (_myOrdersGrid.CurrentRow?.DataBoundItem is not TrangSach.Shared.DTO.StoreOrderDto selected)
            throw new InvalidOperationException("Chọn một hóa đơn để xem chi tiết.");
        var order = await _store.GetOrderAsync(selected.Id)
            ?? throw new InvalidOperationException("Không tìm thấy hóa đơn hoặc hóa đơn không thuộc tài khoản hiện tại.");
        using var details = new CustomerOrderDetailsForm(order);
        details.ShowDialog(this);
    }

    private Control BuildCatalogPage()
    {
        var page = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(251, 250, 247) };
        var content = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(28, 20, 28, 36), BackColor = page.BackColor };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var hero = new Panel { Dock = DockStyle.Top, Height = 260, BackColor = Color.FromArgb(232, 237, 227), Margin = new Padding(0, 0, 0, 30) };
        hero.Paint += (_, e) => DrawCatalogHero(hero, e.Graphics);
        hero.Controls.AddRange([
            new Label { Text = "Mỗi cuốn sách,\nmột thế giới mới.", AutoSize = true, Location = new Point(30, 48), Font = new Font("Georgia", 29), ForeColor = Color.FromArgb(38, 45, 37), BackColor = Color.Transparent },
            new Label { Text = "✳  MỘT GÓC NHỎ DÀNH CHO NGƯỜI YÊU SÁCH", AutoSize = true, Location = new Point(32, 22), Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(112, 123, 110), BackColor = Color.Transparent },
            new Label { Text = "Chọn một cuốn sách hay, dành cho mình một khoảng lặng và bắt đầu chuyến đi của riêng bạn.", Size = new Size(420, 40), Location = new Point(32, 136), Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(115, 121, 111), BackColor = Color.Transparent }
        ]);
        var heading = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = page.BackColor };
        heading.Controls.AddRange([
            new Label { Text = "ĐƯỢC BẠN ĐỌC YÊU THÍCH", AutoSize = true, Location = new Point(0, 0), Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(129, 135, 125) },
            new Label { Text = "Sách dành cho bạn", AutoSize = true, Location = new Point(0, 19), Font = new Font("Georgia", 20), ForeColor = Color.FromArgb(38, 45, 37) }
        ]);
        var filter = new TableLayoutPanel { Dock = DockStyle.Top, Height = 46, ColumnCount = 2 };
        filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filter.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 285));
        filter.Controls.Add(_categoryChips, 0, 0);
        filter.Controls.Add(_catalogSearch, 1, 0);
        _catalogSearch.Margin = new Padding(4, 1, 0, 8);
        _catalogSearch.TextChanged += (_, _) => RenderCatalog();
        var meta = new Panel { Dock = DockStyle.Top, Height = 33 };
        _catalogCount.Location = new Point(1, 7);
        meta.Controls.Add(_catalogCount);
        content.Controls.Add(hero, 0, 0);
        content.Controls.Add(heading, 0, 1);
        content.Controls.Add(filter, 0, 2);
        content.Controls.Add(meta, 0, 3);
        content.Controls.Add(_catalogItems, 0, 4);
        page.Controls.Add(content);
        page.Resize += (_, _) => ResizeCatalogCards(_catalogItems);
        return page;
    }

    private string _catalogCategory = "Tất cả";

    private void BuildCategoryChips()
    {
        _categoryChips.Controls.Clear();
        foreach (var name in new[] { "Tất cả" }.Concat(_books.Select(book => book.Category)
                     .Where(category => !string.IsNullOrWhiteSpace(category)).Distinct().OrderBy(category => category)))
        {
            var active = name == _catalogCategory;
            var chip = new Button { Text = name, AutoSize = true, Height = 31, Margin = new Padding(0, 1, 7, 2), Padding = new Padding(10, 0, 10, 0), FlatStyle = FlatStyle.Flat, BackColor = active ? Color.FromArgb(53, 86, 68) : Color.FromArgb(251, 250, 247), ForeColor = active ? Color.White : Color.FromArgb(82, 91, 79), Font = new Font("Segoe UI", 8), Cursor = Cursors.Hand };
            chip.FlatAppearance.BorderColor = Color.FromArgb(224, 225, 217);
            chip.Click += (_, _) => { _catalogCategory = name; BuildCategoryChips(); RenderCatalog(); };
            _categoryChips.Controls.Add(chip);
        }
    }

    private void RenderCatalog()
    {
        var keyword = _catalogSearch.Text.Trim();
        var found = _books.Where(book => (_catalogCategory == "Tất cả" || book.Category == _catalogCategory)
            && (keyword.Length == 0 || book.Title.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
                || book.Author.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))).ToArray();
        _catalogCount.Text = $"{found.Length} cuốn sách";
        _catalogItems.SuspendLayout();
        _catalogItems.Controls.Clear();
        foreach (var book in found) _catalogItems.Controls.Add(CreateBookCard(book));
        if (found.Length == 0)
            _catalogItems.Controls.Add(new Label { Text = "Chưa tìm thấy sách phù hợp. Thử từ khóa hoặc thể loại khác nhé.", AutoSize = true, Margin = new Padding(2, 30, 2, 50), ForeColor = Color.Gray });
        ResizeCatalogCards(_catalogItems);
        _catalogItems.ResumeLayout();
    }

    private Control CreateBookCard(TrangSach.Shared.DTO.BookItemDto book)
    {
        var background = Color.FromArgb(251, 250, 247);
        var card = new Panel { Width = 205, Height = 390, Margin = new Padding(0, 0, 18, 18), BackColor = background };
        var cover = new Panel { Location = new Point(1, 1), Size = new Size(198, 235), BackColor = CatalogCoverColor(book.CoverStyle) };
        cover.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(cover.ClientRectangle, ControlPaint.Light(cover.BackColor, .2f), ControlPaint.Dark(cover.BackColor, .14f), 135);
            e.Graphics.FillRectangle(brush, cover.ClientRectangle);
            using var pen = new Pen(Color.FromArgb(90, 255, 255, 255), 2);
            e.Graphics.DrawLine(pen, 12, 5, 12, cover.Height - 5);
        };
        cover.Controls.AddRange([
            new Label { Text = "T R A N G   S Á C H", Location = new Point(18, 14), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 7, FontStyle.Bold), BackColor = Color.Transparent },
            new Label { Text = book.Label, Location = new Point(126, 12), AutoSize = true, Padding = new Padding(4), BackColor = Color.FromArgb(245, 251, 250, 240), ForeColor = Color.FromArgb(70, 80, 68), Font = new Font("Segoe UI", 7, FontStyle.Bold) },
            new Label { Text = book.Title, Location = new Point(19, 112), Size = new Size(162, 78), ForeColor = Color.White, Font = new Font("Georgia", 15, FontStyle.Bold), BackColor = Color.Transparent },
            new Label { Text = book.Author, Location = new Point(20, 204), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 8), BackColor = Color.Transparent }
        ]);
        card.Controls.Add(cover);
        card.Controls.AddRange([
            new Label { Text = book.Category.ToUpperInvariant(), Location = new Point(2, 245), AutoSize = true, ForeColor = Color.FromArgb(137, 142, 131), Font = new Font("Segoe UI", 7, FontStyle.Bold) },
            new Label { Text = book.Title, Location = new Point(2, 263), Size = new Size(198, 22), AutoEllipsis = true, ForeColor = Color.FromArgb(38, 45, 37), Font = new Font("Georgia", 11, FontStyle.Bold) },
            new Label { Text = book.Author, Location = new Point(2, 286), Size = new Size(198, 19), AutoEllipsis = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8) },
            new Label { Text = book.Price.ToString("N0") + " ₫", Location = new Point(2, 315), AutoSize = true, ForeColor = Color.FromArgb(53, 86, 68), Font = new Font("Segoe UI", 10, FontStyle.Bold) },
            new Label { Text = book.AvailableStock > 0 ? $"Còn {book.AvailableStock} cuốn" : "Tạm hết hàng", Location = new Point(2, 340), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8) }
        ]);
        var details = new Button { Text = "Chi tiết", Location = new Point(2, 358), Size = new Size(74, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Color.FromArgb(53, 86, 68), Font = new Font("Segoe UI", 8, FontStyle.Bold), Cursor = Cursors.Hand };
        details.Click += async (_, _) => await SafeAsync(() => OpenBookDetailsAsync(book));
        var add = new Button { Text = book.AvailableStock > 0 ? "Thêm vào giỏ +" : "Tạm hết hàng", Location = new Point(80, 358), Size = new Size(108, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(53, 86, 68), ForeColor = Color.White, Font = new Font("Segoe UI", 8, FontStyle.Bold), Enabled = book.AvailableStock > 0, Cursor = Cursors.Hand };
        add.Click += async (_, _) => await SafeAsync(() => AddBookToCartAsync(book));
        card.Controls.AddRange([details, add]);
        AttachBookDetailsClick(card, book);
        return card;
    }

    private async Task OpenBookDetailsAsync(TrangSach.Shared.DTO.BookItemDto book)
    {
        var details = await _store.GetBookAsync(book.Id) ?? book;
        using var form = new CustomerBookDetailsForm(details);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        await AddBookToCartAsync(details);
        if (form.BuyNowRequested) await CheckoutAsync();
    }

    private void AttachBookDetailsClick(Control control, TrangSach.Shared.DTO.BookItemDto book)
    {
        if (control is Button) return;
        control.Cursor = Cursors.Hand;
        control.Click += async (_, _) => await SafeAsync(() => OpenBookDetailsAsync(book));
        foreach (Control child in control.Controls) AttachBookDetailsClick(child, book);
    }

    private async Task AddBookToCartAsync(TrangSach.Shared.DTO.BookItemDto book)
    {
        if (_role != StaffRoles.Customer) throw new UnauthorizedAccessException("Đăng nhập tài khoản khách hàng để thêm sách vào giỏ.");
        var cart = await _store.GetCartAsync();
        var quantity = cart.FirstOrDefault(item => item.BookId == book.Id)?.Quantity ?? 0;
        await _store.SetCartQuantityAsync(book.Id, quantity + 1);
        _status.Text = $"Đã thêm ‘{book.Title}’ vào giỏ hàng.";
        await LoadCartAsync();
    }

    private static void ResizeCatalogCards(Control container)
    {
        var width = Math.Max(600, container.ClientSize.Width - 95);
        var columns = Math.Max(1, width / 225);
        var cardWidth = Math.Max(190, (width - columns * 18) / columns);
        foreach (Control control in container.Controls)
            if (control is Panel panel && panel.Height > 300) panel.Width = cardWidth;
    }

    private static Color CatalogCoverColor(string style) => style switch
    {
        "cover-sand" => Color.FromArgb(190, 154, 100), "cover-coral" => Color.FromArgb(184, 102, 82),
        "cover-ink" => Color.FromArgb(57, 75, 80), "cover-blue" => Color.FromArgb(91, 120, 145),
        "cover-plum" => Color.FromArgb(121, 92, 102), _ => Color.FromArgb(104, 130, 115)
    };

    private static void DrawCatalogHero(Control panel, Graphics graphics)
    {
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var sun = new SolidBrush(Color.FromArgb(225, 217, 189));
        graphics.FillEllipse(sun, panel.Width - 390, 22, 215, 215);
        var colors = new[] { Color.FromArgb(191, 121, 92), Color.FromArgb(70, 100, 86), Color.FromArgb(214, 189, 131) };
        var titles = new[] { "THE QUIET\nMIND", "NHỮNG NGÀY\nTHƠ ẤU", "ĐI TÌM MỘT\nBÌNH YÊN" };
        for (var index = 0; index < 3; index++)
        {
            var rect = new Rectangle(panel.Width - 365 + index * 72, 52 + (index == 1 ? 9 : 0), 108, 168);
            using var brush = new SolidBrush(colors[index]);
            graphics.FillRectangle(brush, rect);
            using var pen = new Pen(Color.FromArgb(65, 30, 40, 32), 2);
            graphics.DrawLine(pen, rect.Left + 9, rect.Top + 4, rect.Left + 9, rect.Bottom - 4);
            TextRenderer.DrawText(graphics, titles[index], new Font("Georgia", 11, FontStyle.Bold), Rectangle.Inflate(rect, -10, -12), index == 2 ? Color.FromArgb(41, 59, 49) : Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        TextRenderer.DrawText(graphics, "GỢI Ý CHO MỘT NGÀY DỊU DÀNG", new Font("Segoe UI", 8, FontStyle.Bold), new Rectangle(panel.Width - 390, 232, 300, 18), Color.FromArgb(112, 123, 110), TextFormatFlags.HorizontalCenter);
    }
}



