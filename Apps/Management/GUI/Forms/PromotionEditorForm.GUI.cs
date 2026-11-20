using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

public sealed class PromotionEditorForm : Form
{
    private sealed class RuleRowItem
    {
        public string TypeCode { get; set; } = string.Empty; // "BOOK", "ORDER", "VOUCHER"
        public string TypeName { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string TargetDisplay { get; set; } = string.Empty;
        public decimal ConditionValue { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    private readonly TextBox _txtName = new();
    private readonly DateTimePicker _dtpStart = new();
    private readonly DateTimePicker _dtpEnd = new();

    private readonly ComboBox _cboRuleType = new();
    private readonly ComboBox _cboBook = new();
    private readonly NumericUpDown _numMinOrder = new();
    private readonly TextBox _txtVoucher = new();
    private readonly NumericUpDown _numDiscountPercent = new();
    private readonly Button _btnAddRule = new();
    private readonly Button _btnRemoveRule = new();
    private readonly DataGridView _gridRules = new();
    private readonly Label _lblSummary = new();
    private readonly Button _btnSave = new();
    private readonly Button _btnCancel = new();

    private readonly FlowLayoutPanel _pnlInputContainer = new();
    private readonly Label _lblTarget = new();

    private readonly List<RuleRowItem> _rules = new();
    private readonly List<BookItemDto> _availableBooks;

    public CreatePromotionDto? ResultDto { get; private set; }

    public PromotionEditorForm(List<BookItemDto> books)
    {
        _availableBooks = books;

        Text = "Tạo chương trình khuyến mãi mới";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Size = new Size(880, 680);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        InitComponents();
    }

    private void InitComponents()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110)); // Thông tin chung (Tên, ngày BĐ, ngày KT)
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));  // Thêm quy tắc
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Lưới danh sách quy tắc
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Footer

        // 1. Group thông tin chung chương trình
        var grpInfo = new GroupBox
        {
            Text = "Thông tin chương trình khuyến mãi",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 111, 88)
        };
        var tblInfo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(45, 63, 82),
            Padding = new Padding(6)
        };
        tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        // Tên chương trình (chiếm 2 cột)
        tblInfo.Controls.Add(new Label { Text = "Tên chương trình:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);
        _txtName.Dock = DockStyle.Fill;
        _txtName.PlaceholderText = "Nhập tên chương trình khuyến mãi...";
        tblInfo.Controls.Add(_txtName, 1, 0);
        tblInfo.SetColumnSpan(_txtName, 3);

        // Ngày bắt đầu
        tblInfo.Controls.Add(new Label { Text = "Ngày bắt đầu:", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 1);
        _dtpStart.Format = DateTimePickerFormat.Short;
        _dtpStart.Value = DateTime.Today;
        _dtpStart.Dock = DockStyle.Fill;
        tblInfo.Controls.Add(_dtpStart, 1, 1);

        // Ngày kết thúc
        tblInfo.Controls.Add(new Label { Text = "Ngày kết thúc:", Anchor = AnchorStyles.Left, AutoSize = true }, 2, 1);
        _dtpEnd.Format = DateTimePickerFormat.Short;
        _dtpEnd.Value = DateTime.Today.AddDays(14);
        _dtpEnd.Dock = DockStyle.Fill;
        tblInfo.Controls.Add(_dtpEnd, 3, 1);

        grpInfo.Controls.Add(tblInfo);
        mainLayout.Controls.Add(grpInfo, 0, 0);

        // 2. Panel thêm quy tắc
        _pnlInputContainer.Dock = DockStyle.Fill;
        _pnlInputContainer.WrapContents = false;
        _pnlInputContainer.Padding = new Padding(0, 8, 0, 0);

        _pnlInputContainer.Controls.Add(new Label { Text = "Hình thức:", AutoSize = true, Margin = new Padding(0, 6, 4, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
        _cboRuleType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboRuleType.Width = 155;
        _cboRuleType.Items.Add(new KeyValuePair<string, string>("BOOK", "1. Giảm theo Sách"));
        _cboRuleType.Items.Add(new KeyValuePair<string, string>("ORDER", "2. Giảm theo Hóa đơn"));
        _cboRuleType.Items.Add(new KeyValuePair<string, string>("VOUCHER", "3. Giảm qua Voucher"));
        _cboRuleType.DisplayMember = "Value";
        _cboRuleType.ValueMember = "Key";
        _cboRuleType.SelectedIndex = 0;
        _cboRuleType.SelectedIndexChanged += (_, _) => OnRuleTypeChanged();
        _pnlInputContainer.Controls.Add(_cboRuleType);

        _lblTarget.Text = "Sách:";
        _lblTarget.AutoSize = true;
        _lblTarget.Margin = new Padding(8, 6, 4, 0);
        _lblTarget.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _pnlInputContainer.Controls.Add(_lblTarget);

        _cboBook.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboBook.Width = 220;
        _cboBook.DisplayMember = "Title";
        _cboBook.ValueMember = "Id";
        _cboBook.DataSource = _availableBooks.ToList();
        _pnlInputContainer.Controls.Add(_cboBook);

        _numMinOrder.Minimum = 0;
        _numMinOrder.Maximum = 1_000_000_000;
        _numMinOrder.ThousandsSeparator = true;
        _numMinOrder.DecimalPlaces = 0;
        _numMinOrder.Value = 200000;
        _numMinOrder.Width = 130;
        _numMinOrder.Visible = false;
        _pnlInputContainer.Controls.Add(_numMinOrder);

        _txtVoucher.Width = 130;
        _txtVoucher.Text = $"KM{DateTime.UtcNow:yyMMdd}";
        _txtVoucher.Visible = false;
        _pnlInputContainer.Controls.Add(_txtVoucher);

        _pnlInputContainer.Controls.Add(new Label { Text = "% Giảm:", AutoSize = true, Margin = new Padding(8, 6, 4, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
        _numDiscountPercent.Minimum = 1;
        _numDiscountPercent.Maximum = 90;
        _numDiscountPercent.Value = 10;
        _numDiscountPercent.Width = 55;
        _pnlInputContainer.Controls.Add(_numDiscountPercent);

        _btnAddRule.Text = "+ Thêm quy tắc";
        _btnAddRule.AutoSize = true;
        _btnAddRule.Height = 28;
        _btnAddRule.BackColor = Color.FromArgb(31, 111, 88);
        _btnAddRule.ForeColor = Color.White;
        _btnAddRule.FlatStyle = FlatStyle.Flat;
        _btnAddRule.FlatAppearance.BorderSize = 0;
        _btnAddRule.Cursor = Cursors.Hand;
        _btnAddRule.Margin = new Padding(8, 1, 4, 0);
        _btnAddRule.Click += (_, _) => AddRule();
        _pnlInputContainer.Controls.Add(_btnAddRule);

        _btnRemoveRule.Text = "- Xóa quy tắc";
        _btnRemoveRule.AutoSize = true;
        _btnRemoveRule.Height = 28;
        _btnRemoveRule.BackColor = Color.FromArgb(255, 238, 237);
        _btnRemoveRule.ForeColor = Color.FromArgb(166, 57, 57);
        _btnRemoveRule.FlatStyle = FlatStyle.Flat;
        _btnRemoveRule.FlatAppearance.BorderSize = 0;
        _btnRemoveRule.Cursor = Cursors.Hand;
        _btnRemoveRule.Margin = new Padding(4, 1, 0, 0);
        _btnRemoveRule.Click += (_, _) => RemoveSelectedRule();
        _pnlInputContainer.Controls.Add(_btnRemoveRule);

        mainLayout.Controls.Add(_pnlInputContainer, 0, 1);

        // 3. Grid danh sách quy tắc
        _gridRules.Dock = DockStyle.Fill;
        _gridRules.ReadOnly = true;
        _gridRules.AllowUserToAddRows = false;
        _gridRules.AllowUserToDeleteRows = false;
        _gridRules.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _gridRules.MultiSelect = false;
        _gridRules.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _gridRules.BackgroundColor = Color.White;
        _gridRules.RowHeadersVisible = false;
        _gridRules.ColumnHeadersHeight = 32;
        _gridRules.EnableHeadersVisualStyles = false;
        _gridRules.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(237, 242, 247),
            ForeColor = Color.FromArgb(39, 58, 78),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        SetupGridColumns();
        mainLayout.Controls.Add(_gridRules, 0, 2);

        // 4. Footer
        var pnlFooter = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        pnlFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        pnlFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _lblSummary.Text = "TỔNG SỐ QUY TẮC: 0";
        _lblSummary.Anchor = AnchorStyles.Left;
        _lblSummary.AutoSize = true;
        _lblSummary.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _lblSummary.ForeColor = Color.FromArgb(45, 63, 82);
        pnlFooter.Controls.Add(_lblSummary, 0, 0);

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

        _btnSave.Text = "Tạo khuyến mãi";
        _btnSave.Size = new Size(130, 34);
        _btnSave.BackColor = Color.FromArgb(31, 111, 88);
        _btnSave.ForeColor = Color.White;
        _btnSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnSave.FlatStyle = FlatStyle.Flat;
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += (_, _) => SavePromotion();

        pnlButtons.Controls.Add(_btnCancel);
        pnlButtons.Controls.Add(_btnSave);
        pnlFooter.Controls.Add(pnlButtons, 1, 0);

        mainLayout.Controls.Add(pnlFooter, 0, 3);
        Controls.Add(mainLayout);
    }

    private void SetupGridColumns()
    {
        _gridRules.Columns.Clear();

        var colType = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(RuleRowItem.TypeName),
            HeaderText = "Hình thức ưu đãi",
            FillWeight = 25
        };
        var colTarget = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(RuleRowItem.TargetDisplay),
            HeaderText = "Đối tượng / Điều kiện áp dụng",
            FillWeight = 45
        };
        var colDiscount = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(RuleRowItem.DiscountPercent),
            HeaderText = "Mức giảm (%)",
            FillWeight = 20
        };
        colDiscount.DefaultCellStyle.Format = "0.#\\%";
        colDiscount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        _gridRules.Columns.AddRange(colType, colTarget, colDiscount);
    }

    private void OnRuleTypeChanged()
    {
        var type = ((KeyValuePair<string, string>)_cboRuleType.SelectedItem).Key;
        if (type == "BOOK")
        {
            _lblTarget.Text = "Sách:";
            _cboBook.Visible = true;
            _numMinOrder.Visible = false;
            _txtVoucher.Visible = false;
        }
        else if (type == "ORDER")
        {
            _lblTarget.Text = "Đơn tối thiểu:";
            _cboBook.Visible = false;
            _numMinOrder.Visible = true;
            _txtVoucher.Visible = false;
        }
        else
        {
            _lblTarget.Text = "Mã voucher:";
            _cboBook.Visible = false;
            _numMinOrder.Visible = false;
            _txtVoucher.Visible = true;
        }
    }

    private void RefreshGrid()
    {
        _gridRules.DataSource = null;
        _gridRules.DataSource = _rules.ToList();
        _lblSummary.Text = $"TỔNG SỐ QUY TẮC: {_rules.Count} (Sách: {_rules.Count(x => x.TypeCode == "BOOK")}, Đơn: {_rules.Count(x => x.TypeCode == "ORDER")}, Voucher: {_rules.Count(x => x.TypeCode == "VOUCHER")})";
    }

    private void AddRule()
    {
        var type = ((KeyValuePair<string, string>)_cboRuleType.SelectedItem).Key;
        decimal discount = _numDiscountPercent.Value;

        if (type == "BOOK")
        {
            if (_cboBook.SelectedValue is not string bookId) return;
            var book = _availableBooks.FirstOrDefault(b => b.Id == bookId);
            if (book == null) return;

            var existing = _rules.FirstOrDefault(r => r.TypeCode == "BOOK" && r.TargetId == bookId);
            if (existing != null)
            {
                existing.DiscountPercent = discount;
            }
            else
            {
                _rules.Add(new RuleRowItem
                {
                    TypeCode = "BOOK",
                    TypeName = "Giảm theo Sách",
                    TargetId = book.Id,
                    TargetDisplay = book.Title,
                    ConditionValue = 0,
                    DiscountPercent = discount
                });
            }
        }
        else if (type == "ORDER")
        {
            decimal min = _numMinOrder.Value;
            var existing = _rules.FirstOrDefault(r => r.TypeCode == "ORDER" && r.ConditionValue == min);
            if (existing != null)
            {
                existing.DiscountPercent = discount;
            }
            else
            {
                _rules.Add(new RuleRowItem
                {
                    TypeCode = "ORDER",
                    TypeName = "Giảm theo Đơn hàng",
                    TargetId = min.ToString(CultureInfo.InvariantCulture),
                    TargetDisplay = $"Đơn từ {min:N0} ₫",
                    ConditionValue = min,
                    DiscountPercent = discount
                });
            }
        }
        else
        {
            var code = _txtVoucher.Text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show(this, "Vui lòng nhập mã voucher.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existing = _rules.FirstOrDefault(r => r.TypeCode == "VOUCHER" && r.TargetId == code);
            if (existing != null)
            {
                existing.DiscountPercent = discount;
            }
            else
            {
                _rules.Add(new RuleRowItem
                {
                    TypeCode = "VOUCHER",
                    TypeName = "Giảm qua Voucher",
                    TargetId = code,
                    TargetDisplay = $"Mã '{code}'",
                    ConditionValue = 0,
                    DiscountPercent = discount
                });
            }
        }

        RefreshGrid();
    }

    private void RemoveSelectedRule()
    {
        if (_gridRules.CurrentRow?.DataBoundItem is RuleRowItem item)
        {
            _rules.Remove(item);
            RefreshGrid();
        }
    }

    private void SavePromotion()
    {
        var name = _txtName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
        {
            MessageBox.Show(this, "Vui lòng nhập tên chương trình khuyến mãi (ít nhất 2 ký tự).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            return;
        }

        if (_dtpEnd.Value <= _dtpStart.Value)
        {
            MessageBox.Show(this, "Ngày kết thúc phải sau ngày bắt đầu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_rules.Count == 0)
        {
            MessageBox.Show(this, "Chương trình khuyến mãi phải có ít nhất 1 quy tắc ưu đãi (giảm theo sách, theo đơn hàng hoặc theo voucher).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var books = _rules.Where(r => r.TypeCode == "BOOK")
            .Select(r => new PromotionBookRuleDto(r.TargetId, r.DiscountPercent)).ToList();
        var orders = _rules.Where(r => r.TypeCode == "ORDER")
            .Select(r => new PromotionOrderRuleDto(r.ConditionValue, r.DiscountPercent)).ToList();
        var vouchers = _rules.Where(r => r.TypeCode == "VOUCHER")
            .Select(r => new PromotionVoucherRuleDto(r.TargetId, r.DiscountPercent)).ToList();

        // Tạo DTO tương thích cho cả 1 dòng hoặc nhiều dòng
        var firstBook = books.FirstOrDefault();
        var firstOrder = orders.FirstOrDefault();
        var firstVoucher = vouchers.FirstOrDefault();

        ResultDto = new CreatePromotionDto(
            name, _dtpStart.Value.Date, _dtpEnd.Value.Date,
            firstBook?.BookId, firstBook?.DiscountPercent,
            firstOrder?.MinimumOrderAmount, firstOrder?.DiscountPercent,
            firstVoucher?.VoucherId, firstVoucher?.DiscountPercent, 100)
        {
            BookRules = books,
            OrderRules = orders,
            VoucherRules = vouchers
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
