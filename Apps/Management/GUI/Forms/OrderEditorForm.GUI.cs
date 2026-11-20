using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

public sealed class OrderEditorForm : Form
{
    private sealed class OrderLineItem
    {
        public string BookId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
    }

    private readonly ComboBox _cboCustomer = new();
    private readonly ComboBox _cboEmployee = new();
    private readonly TextBox _txtPhone = new();
    private readonly TextBox _txtAddress = new();
    private readonly ComboBox _cboPaymentMethod = new();
    private readonly ComboBox _cboPaymentStatus = new();
    private readonly ComboBox _cboBook = new();
    private readonly NumericUpDown _numQty = new();
    private readonly Button _btnAddBook = new();
    private readonly Button _btnRemoveBook = new();
    private readonly DataGridView _gridLines = new();
    private readonly Label _lblTotal = new();
    private readonly Button _btnSave = new();
    private readonly Button _btnCancel = new();

    private readonly List<OrderLineItem> _lines = new();
    private readonly List<BookItemDto> _availableBooks;
    private readonly List<ManagerCustomerDto> _availableCustomers;
    private readonly List<ManagerEmployeeDto> _availableEmployees;

    public CreateAdminOrderDto? ResultCreateDto { get; private set; }
    public UpdateAdminOrderDto? ResultUpdateDto { get; private set; }

    public OrderEditorForm(
        List<ManagerCustomerDto> customers,
        List<ManagerEmployeeDto> employees,
        List<BookItemDto> books,
        ManagerOrderRowDto? existingOrder = null)
    {
        _availableCustomers = customers;
        _availableEmployees = employees;
        _availableBooks = books;

        Text = existingOrder == null ? "Tạo đơn hàng tại quầy" : $"Chỉnh sửa đơn hàng - {existingOrder.Id}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Size = new Size(880, 700);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        InitComponents(existingOrder);
    }

    private void InitComponents(ManagerOrderRowDto? existingOrder)
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 168)); // Thông tin chung
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Thêm sách
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Lưới chi tiết
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Footer (Tổng tiền + Nút)

        // 1. Panel thông tin chung
        var grpInfo = new GroupBox
        {
            Text = "Thông tin chung đơn hàng",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 111, 88)
        };
        var tblInfo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 3,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(45, 63, 82),
            Padding = new Padding(8)
        };
        tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        // Hàng 0: Tên Khách hàng (cho phép gõ và gợi ý) & Nhân viên lập đơn
        tblInfo.Controls.Add(new Label { Text = "Tên khách hàng:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);
        _cboCustomer.DropDownStyle = ComboBoxStyle.DropDown;
        _cboCustomer.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cboCustomer.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cboCustomer.Dock = DockStyle.Fill;
        _cboCustomer.DisplayMember = nameof(ManagerCustomerDto.FullName);
        _cboCustomer.ValueMember = nameof(ManagerCustomerDto.Id);
        _cboCustomer.DataSource = _availableCustomers.ToList();
        _cboCustomer.SelectedIndexChanged += (_, _) =>
        {
            if (_cboCustomer.SelectedItem is ManagerCustomerDto cus && existingOrder == null)
            {
                if (!string.IsNullOrWhiteSpace(cus.PhoneNumber)) _txtPhone.Text = cus.PhoneNumber;
                if (!string.IsNullOrWhiteSpace(cus.Address)) _txtAddress.Text = cus.Address;
            }
        };
        tblInfo.Controls.Add(_cboCustomer, 1, 0);

        tblInfo.Controls.Add(new Label { Text = "Nhân viên lập đơn:", Anchor = AnchorStyles.Left, AutoSize = true }, 2, 0);
        _cboEmployee.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboEmployee.Dock = DockStyle.Fill;
        var empList = _availableEmployees.Select(e => new { e.Id, FullName = $"{e.LastName} {e.FirstName}".Trim() }).ToList();
        _cboEmployee.DisplayMember = "FullName";
        _cboEmployee.ValueMember = "Id";
        _cboEmployee.DataSource = empList;
        tblInfo.Controls.Add(_cboEmployee, 3, 0);

        // Hàng 1: Số điện thoại & Địa chỉ giao hàng
        tblInfo.Controls.Add(new Label { Text = "Số điện thoại:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 1);
        _txtPhone.Dock = DockStyle.Fill;
        tblInfo.Controls.Add(_txtPhone, 1, 1);

        tblInfo.Controls.Add(new Label { Text = "Địa chỉ giao hàng:", Anchor = AnchorStyles.Left, AutoSize = true }, 2, 1);
        _txtAddress.Dock = DockStyle.Fill;
        tblInfo.Controls.Add(_txtAddress, 3, 1);

        // Hàng 2: Phương thức thanh toán & Trạng thái thanh toán
        tblInfo.Controls.Add(new Label { Text = "Phương thức TT:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 2);
        _cboPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboPaymentMethod.Dock = DockStyle.Fill;
        _cboPaymentMethod.Items.Add(new KeyValuePair<string, string>("CashOnDelivery", "Thanh toán khi nhận hàng (COD)"));
        _cboPaymentMethod.Items.Add(new KeyValuePair<string, string>("BankTransfer", "Chuyển khoản ngân hàng"));
        _cboPaymentMethod.DisplayMember = "Value";
        _cboPaymentMethod.ValueMember = "Key";
        _cboPaymentMethod.SelectedIndex = 0;
        tblInfo.Controls.Add(_cboPaymentMethod, 1, 2);

        tblInfo.Controls.Add(new Label { Text = "Trạng thái TT:", Anchor = AnchorStyles.Left, AutoSize = true }, 2, 2);
        _cboPaymentStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboPaymentStatus.Dock = DockStyle.Fill;
        _cboPaymentStatus.Items.Add(new KeyValuePair<string, string>("Unpaid", "Chưa thanh toán (Unpaid)"));
        _cboPaymentStatus.Items.Add(new KeyValuePair<string, string>("Paid", "Đã thanh toán (Paid)"));
        _cboPaymentStatus.Items.Add(new KeyValuePair<string, string>("AwaitingPayment", "Chờ xác nhận CK (AwaitingPayment)"));
        _cboPaymentStatus.DisplayMember = "Value";
        _cboPaymentStatus.ValueMember = "Key";
        _cboPaymentStatus.SelectedIndex = 0;
        tblInfo.Controls.Add(_cboPaymentStatus, 3, 2);

        grpInfo.Controls.Add(tblInfo);
        mainLayout.Controls.Add(grpInfo, 0, 0);

        // 2. Panel thêm sách vào chi tiết đơn
        var pnlAddBook = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };
        pnlAddBook.Controls.Add(new Label { Text = "Chọn sách:", AutoSize = true, Margin = new Padding(0, 6, 4, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });

        _cboBook.DropDownStyle = ComboBoxStyle.DropDown;
        _cboBook.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cboBook.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cboBook.Width = 360;
        var bookList = _availableBooks.Select(b => new { b.Id, Display = $"{b.Title} - {b.Price:N0}₫ (Tồn: {b.AvailableStock})" }).ToList();
        _cboBook.DisplayMember = "Display";
        _cboBook.ValueMember = "Id";
        _cboBook.DataSource = bookList;
        pnlAddBook.Controls.Add(_cboBook);

        pnlAddBook.Controls.Add(new Label { Text = "SL:", AutoSize = true, Margin = new Padding(10, 6, 4, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
        _numQty.Minimum = 1;
        _numQty.Maximum = 999;
        _numQty.Value = 1;
        _numQty.Width = 60;
        pnlAddBook.Controls.Add(_numQty);

        _btnAddBook.Text = "+ Thêm sách vào đơn";
        _btnAddBook.AutoSize = true;
        _btnAddBook.Height = 28;
        _btnAddBook.BackColor = Color.FromArgb(31, 111, 88);
        _btnAddBook.ForeColor = Color.White;
        _btnAddBook.FlatStyle = FlatStyle.Flat;
        _btnAddBook.FlatAppearance.BorderSize = 0;
        _btnAddBook.Cursor = Cursors.Hand;
        _btnAddBook.Margin = new Padding(10, 1, 4, 0);
        _btnAddBook.Click += (_, _) => AddBookLine();
        pnlAddBook.Controls.Add(_btnAddBook);

        _btnRemoveBook.Text = "- Xóa dòng đang chọn";
        _btnRemoveBook.AutoSize = true;
        _btnRemoveBook.Height = 28;
        _btnRemoveBook.BackColor = Color.FromArgb(255, 238, 237);
        _btnRemoveBook.ForeColor = Color.FromArgb(166, 57, 57);
        _btnRemoveBook.FlatStyle = FlatStyle.Flat;
        _btnRemoveBook.FlatAppearance.BorderSize = 0;
        _btnRemoveBook.Cursor = Cursors.Hand;
        _btnRemoveBook.Margin = new Padding(8, 1, 0, 0);
        _btnRemoveBook.Click += (_, _) => RemoveSelectedLine();
        pnlAddBook.Controls.Add(_btnRemoveBook);

        mainLayout.Controls.Add(pnlAddBook, 0, 1);

        // 3. Grid chi tiết mặt hàng
        _gridLines.Dock = DockStyle.Fill;
        _gridLines.ReadOnly = false;
        _gridLines.AllowUserToAddRows = false;
        _gridLines.AllowUserToDeleteRows = false;
        _gridLines.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _gridLines.MultiSelect = false;
        _gridLines.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _gridLines.BackgroundColor = Color.White;
        _gridLines.RowHeadersVisible = false;
        _gridLines.ColumnHeadersHeight = 32;
        _gridLines.EnableHeadersVisualStyles = false;
        _gridLines.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(237, 242, 247),
            ForeColor = Color.FromArgb(39, 58, 78),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        _gridLines.CellValueChanged += (_, _) => RecalculateTotals();
        _gridLines.DataError += (_, _) => { };

        SetupGridColumns();
        mainLayout.Controls.Add(_gridLines, 0, 2);

        // 4. Panel Footer: Tổng tiền & Nút Lưu/Hủy
        var pnlFooter = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        pnlFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        pnlFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _lblTotal.Text = "TỔNG TIỀN ĐƠN: 0 VNĐ";
        _lblTotal.Anchor = AnchorStyles.Left;
        _lblTotal.AutoSize = true;
        _lblTotal.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        _lblTotal.ForeColor = Color.FromArgb(180, 40, 40);
        pnlFooter.Controls.Add(_lblTotal, 0, 0);

        var pnlButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 6, 0, 0)
        };

        _btnCancel.Text = "Hủy bỏ";
        _btnCancel.Size = new Size(100, 34);
        _btnCancel.BackColor = Color.FromArgb(241, 245, 249);
        _btnCancel.FlatStyle = FlatStyle.Flat;
        _btnCancel.FlatAppearance.BorderSize = 0;
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnSave.Text = existingOrder == null ? "Tạo đơn hàng" : "Lưu thay đổi";
        _btnSave.Size = new Size(130, 34);
        _btnSave.BackColor = Color.FromArgb(31, 111, 88);
        _btnSave.ForeColor = Color.White;
        _btnSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnSave.FlatStyle = FlatStyle.Flat;
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += (_, _) => SaveOrder(existingOrder);

        pnlButtons.Controls.Add(_btnCancel);
        pnlButtons.Controls.Add(_btnSave);
        pnlFooter.Controls.Add(pnlButtons, 1, 0);

        mainLayout.Controls.Add(pnlFooter, 0, 3);
        Controls.Add(mainLayout);

        // Tải dữ liệu ban đầu nếu là sửa đơn
        if (existingOrder != null)
        {
            _cboCustomer.Text = existingOrder.CustomerName;
            var foundCustomer = _availableCustomers.FirstOrDefault(c => c.FullName == existingOrder.CustomerName || c.Id == existingOrder.CustomerName);
            if (foundCustomer != null) _cboCustomer.SelectedItem = foundCustomer;

            var foundEmp = _availableEmployees.FirstOrDefault(e => $"{e.LastName} {e.FirstName}".Trim() == existingOrder.EmployeeName || e.Id == existingOrder.EmployeeName);
            if (foundEmp != null) _cboEmployee.SelectedValue = foundEmp.Id;

            _txtPhone.Text = existingOrder.ReceiverPhone;
            _txtAddress.Text = existingOrder.ShippingAddress;

            for (int i = 0; i < _cboPaymentMethod.Items.Count; i++)
            {
                if (((KeyValuePair<string, string>)_cboPaymentMethod.Items[i]).Key == existingOrder.PaymentMethod)
                {
                    _cboPaymentMethod.SelectedIndex = i;
                    break;
                }
            }

            for (int i = 0; i < _cboPaymentStatus.Items.Count; i++)
            {
                if (((KeyValuePair<string, string>)_cboPaymentStatus.Items[i]).Key == existingOrder.PaymentStatus)
                {
                    _cboPaymentStatus.SelectedIndex = i;
                    break;
                }
            }

            foreach (var line in existingOrder.Lines)
            {
                _lines.Add(new OrderLineItem
                {
                    BookId = line.BookId,
                    Title = line.Title,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                });
            }
            RefreshGrid();
        }
        else
        {
            if (_availableCustomers.Count > 0)
            {
                var first = _availableCustomers[0];
                _cboCustomer.Text = first.FullName;
                _txtPhone.Text = first.PhoneNumber ?? "";
                _txtAddress.Text = first.Address ?? "Nhận tại quầy";
            }
            else
            {
                _cboCustomer.Text = "Khách vãng lai";
                _txtAddress.Text = "Nhận tại quầy";
                _txtPhone.Text = "0900000000";
            }
        }
    }

    private void SetupGridColumns()
    {
        _gridLines.Columns.Clear();

        var colBookId = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(OrderLineItem.BookId),
            HeaderText = "Mã sách",
            ReadOnly = true,
            FillWeight = 20
        };
        var colTitle = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(OrderLineItem.Title),
            HeaderText = "Tên sách",
            ReadOnly = true,
            FillWeight = 40
        };
        var colQty = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(OrderLineItem.Quantity),
            HeaderText = "Số lượng",
            ReadOnly = false,
            FillWeight = 15
        };
        colQty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        var colUnitPrice = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(OrderLineItem.UnitPrice),
            HeaderText = "Đơn giá (VNĐ)",
            ReadOnly = true,
            FillWeight = 20
        };
        colUnitPrice.DefaultCellStyle.Format = "N0";
        colUnitPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        var colLineTotal = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(OrderLineItem.LineTotal),
            HeaderText = "Thành tiền (VNĐ)",
            ReadOnly = true,
            FillWeight = 25
        };
        colLineTotal.DefaultCellStyle.Format = "N0";
        colLineTotal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        _gridLines.Columns.AddRange(colBookId, colTitle, colQty, colUnitPrice, colLineTotal);
    }

    private void RefreshGrid()
    {
        _gridLines.DataSource = null;
        _gridLines.DataSource = _lines.ToList();
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        decimal total = _lines.Sum(x => x.LineTotal);
        _lblTotal.Text = $"TỔNG TIỀN ĐƠN: {total:N0} VNĐ ({_lines.Sum(x => x.Quantity)} mặt hàng)";
    }

    private void AddBookLine()
    {
        var bookInput = _cboBook.Text.Trim();
        if (string.IsNullOrWhiteSpace(bookInput))
        {
            MessageBox.Show(this, "Vui lòng chọn hoặc gõ tên sách cần mua.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _cboBook.Focus();
            return;
        }

        // Tìm sách theo SelectedValue hoặc tìm theo Tên sách / Mã sách từ text người dùng gõ
        BookItemDto? book = null;
        if (_cboBook.SelectedValue is string bookId)
        {
            book = _availableBooks.FirstOrDefault(b => b.Id == bookId);
        }

        if (book == null)
        {
            book = _availableBooks.FirstOrDefault(b =>
                b.Title.Equals(bookInput, StringComparison.OrdinalIgnoreCase) ||
                b.Id.Equals(bookInput, StringComparison.OrdinalIgnoreCase) ||
                $"{b.Title} - {b.Price:N0}₫ (Tồn: {b.AvailableStock})".Equals(bookInput, StringComparison.OrdinalIgnoreCase));
        }

        if (book == null)
        {
            // Tìm kiếm tương đối nếu người dùng chỉ gõ một phần tên sách
            book = _availableBooks.FirstOrDefault(b => b.Title.Contains(bookInput, StringComparison.OrdinalIgnoreCase));
        }

        if (book == null)
        {
            MessageBox.Show(this, $"Không tìm thấy sách phù hợp với từ khóa '{bookInput}'. Vui lòng kiểm tra lại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _cboBook.Focus();
            return;
        }

        int qty = (int)_numQty.Value;

        var existing = _lines.FirstOrDefault(l => l.BookId == book.Id);
        if (existing != null)
        {
            existing.Quantity += qty;
        }
        else
        {
            _lines.Add(new OrderLineItem
            {
                BookId = book.Id,
                Title = book.Title,
                Quantity = qty,
                UnitPrice = book.Price
            });
        }

        RefreshGrid();
    }

    private void RemoveSelectedLine()
    {
        if (_gridLines.CurrentRow?.DataBoundItem is OrderLineItem item)
        {
            _lines.Remove(item);
            RefreshGrid();
        }
    }

    private void SaveOrder(ManagerOrderRowDto? existingOrder)
    {
        var customerInput = _cboCustomer.Text.Trim();
        if (string.IsNullOrWhiteSpace(customerInput))
        {
            MessageBox.Show(this, "Vui lòng nhập hoặc chọn tên khách hàng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _cboCustomer.Focus();
            return;
        }

        var address = _txtAddress.Text.Trim();
        var phone = _txtPhone.Text.Trim();
        if (address.Length < 4)
        {
            MessageBox.Show(this, "Vui lòng nhập địa chỉ giao hàng hợp lệ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtAddress.Focus();
            return;
        }
        if (phone.Length < 8)
        {
            MessageBox.Show(this, "Vui lòng nhập số điện thoại người nhận hợp lệ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtPhone.Focus();
            return;
        }
        if (_lines.Count == 0)
        {
            MessageBox.Show(this, "Đơn hàng phải có ít nhất 1 mặt hàng sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var paymentMethod = ((KeyValuePair<string, string>)_cboPaymentMethod.SelectedItem).Key;
        var paymentStatus = ((KeyValuePair<string, string>)_cboPaymentStatus.SelectedItem).Key;
        var employeeId = _cboEmployee.SelectedValue?.ToString();
        var orderLines = _lines.Select(l => new CreateAdminOrderLineDto(l.BookId, l.Quantity)).ToList();

        // Tìm khách hàng theo ID hoặc Tên
        var matchedCustomer = _availableCustomers.FirstOrDefault(c => c.FullName.Equals(customerInput, StringComparison.OrdinalIgnoreCase)
                                                                  || c.Id.Equals(customerInput, StringComparison.OrdinalIgnoreCase));
        var customerId = matchedCustomer?.Id ?? _availableCustomers.FirstOrDefault()?.Id ?? "KH00000001";

        if (existingOrder == null)
        {
            ResultCreateDto = new CreateAdminOrderDto(customerId, employeeId, address, phone, paymentMethod, orderLines, customerInput);
        }
        else
        {
            ResultUpdateDto = new UpdateAdminOrderDto(address, phone, paymentMethod, paymentStatus, orderLines, employeeId, customerInput);
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
