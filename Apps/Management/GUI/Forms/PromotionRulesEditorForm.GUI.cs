using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

public sealed class PromotionRulesEditorForm : Form
{
    private sealed class RuleRowItem
    {
        public string TypeCode { get; set; } = string.Empty; // "BOOK", "ORDER", "VOUCHER"
        public string TypeName { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string TargetDisplay { get; set; } = string.Empty;
        public decimal ConditionValue { get; set; } // Min order amount or 0
        public decimal DiscountPercent { get; set; }
    }

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

    public UpdatePromotionRulesDto? ResultDto { get; private set; }

    public PromotionRulesEditorForm(ManagerPromotionDto promo, List<BookItemDto> books)
    {
        _availableBooks = books;

        Text = $"Quản lý quy tắc ưu đãi - {promo.Name} ({promo.Id})";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Size = new Size(880, 640);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        InitComponents(promo);
    }

    private void InitComponents(ManagerPromotionDto promo)
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Thêm quy tắc
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Lưới danh sách quy tắc
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Footer

        // 1. Panel thêm quy tắc
        _pnlInputContainer.Dock = DockStyle.Fill;
        _pnlInputContainer.WrapContents = false;
        _pnlInputContainer.Padding = new Padding(0, 8, 0, 0);

        _pnlInputContainer.Controls.Add(new Label { Text = "Loại ưu đãi:", AutoSize = true, Margin = new Padding(0, 6, 4, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
        _cboRuleType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboRuleType.Width = 160;
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
        _cboBook.Width = 200;
        _cboBook.DisplayMember = "Title";
        _cboBook.ValueMember = "Id";
        _cboBook.DataSource = _availableBooks.ToList();
        _pnlInputContainer.Controls.Add(_cboBook);

        _numMinOrder.Minimum = 0;
        _numMinOrder.Maximum = 1_000_000_000;
        _numMinOrder.ThousandsSeparator = true;
        _numMinOrder.DecimalPlaces = 0;
        _numMinOrder.Value = 200000;
        _numMinOrder.Width = 120;
        _numMinOrder.Visible = false;
        _pnlInputContainer.Controls.Add(_numMinOrder);

        _txtVoucher.Width = 120;
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

        mainLayout.Controls.Add(_pnlInputContainer, 0, 0);

        // 2. Grid danh sách quy tắc
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
        mainLayout.Controls.Add(_gridRules, 0, 1);

        // 3. Footer
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

        _btnCancel.Text = "Đóng";
        _btnCancel.Size = new Size(100, 34);
        _btnCancel.BackColor = Color.FromArgb(241, 245, 249);
        _btnCancel.FlatStyle = FlatStyle.Flat;
        _btnCancel.FlatAppearance.BorderSize = 0;
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnSave.Text = "Lưu quy tắc";
        _btnSave.Size = new Size(130, 34);
        _btnSave.BackColor = Color.FromArgb(31, 111, 88);
        _btnSave.ForeColor = Color.White;
        _btnSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnSave.FlatStyle = FlatStyle.Flat;
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += (_, _) => SaveRules();

        pnlButtons.Controls.Add(_btnCancel);
        pnlButtons.Controls.Add(_btnSave);
        pnlFooter.Controls.Add(pnlButtons, 1, 0);

        mainLayout.Controls.Add(pnlFooter, 0, 2);
        Controls.Add(mainLayout);

        // Nạp các quy tắc cũ
        foreach (var b in promo.BookRules)
        {
            var book = _availableBooks.FirstOrDefault(x => x.Id == b.BookId);
            _rules.Add(new RuleRowItem
            {
                TypeCode = "BOOK",
                TypeName = "Giảm theo Sách",
                TargetId = b.BookId,
                TargetDisplay = book?.Title ?? b.BookId,
                ConditionValue = 0,
                DiscountPercent = b.DiscountPercent
            });
        }
        foreach (var o in promo.OrderRules)
        {
            _rules.Add(new RuleRowItem
            {
                TypeCode = "ORDER",
                TypeName = "Giảm theo Đơn hàng",
                TargetId = o.MinimumOrderAmount.ToString(CultureInfo.InvariantCulture),
                TargetDisplay = $"Đơn từ {o.MinimumOrderAmount:N0} ₫",
                ConditionValue = o.MinimumOrderAmount,
                DiscountPercent = o.DiscountPercent
            });
        }
        foreach (var v in promo.VoucherRules)
        {
            _rules.Add(new RuleRowItem
            {
                TypeCode = "VOUCHER",
                TypeName = "Giảm qua Voucher",
                TargetId = v.VoucherId,
                TargetDisplay = $"Mã '{v.VoucherId}'",
                ConditionValue = 0,
                DiscountPercent = v.DiscountPercent
            });
        }

        RefreshGrid();
    }

    private void SetupGridColumns()
    {
        _gridRules.Columns.Clear();

        var colType = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(RuleRowItem.TypeName),
            HeaderText = "Hình thức áp dụng",
            FillWeight = 25
        };
        var colTarget = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(RuleRowItem.TargetDisplay),
            HeaderText = "Đối tượng / Điều kiện",
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

    private void SaveRules()
    {
        var books = _rules.Where(r => r.TypeCode == "BOOK")
            .Select(r => new PromotionBookRuleDto(r.TargetId, r.DiscountPercent)).ToList();
        var orders = _rules.Where(r => r.TypeCode == "ORDER")
            .Select(r => new PromotionOrderRuleDto(r.ConditionValue, r.DiscountPercent)).ToList();
        var vouchers = _rules.Where(r => r.TypeCode == "VOUCHER")
            .Select(r => new PromotionVoucherRuleDto(r.TargetId, r.DiscountPercent)).ToList();

        ResultDto = new UpdatePromotionRulesDto(books, orders, vouchers);
        DialogResult = DialogResult.OK;
        Close();
    }
}
