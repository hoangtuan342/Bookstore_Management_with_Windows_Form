using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

public sealed class ReceiptEditorForm : Form
{
    private sealed class ReceiptLineItem
    {
        public string BookId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal => Quantity * UnitCost;
    }

    private readonly ComboBox _cboSupplier = new();
    private readonly ComboBox _cboEmployee = new();
    private readonly ComboBox _cboBook = new();
    private readonly NumericUpDown _numQty = new();
    private readonly NumericUpDown _numCost = new();
    private readonly Button _btnAddLine = new();
    private readonly Button _btnRemoveLine = new();
    private readonly DataGridView _gridLines = new();
    private readonly Label _lblTotal = new();
    private readonly Button _btnSave = new();
    private readonly Button _btnCancel = new();

    private readonly List<ReceiptLineItem> _lines = new();
    private readonly List<BookItemDto> _availableBooks;
    private readonly List<SupplierOptionDto> _availableSuppliers;
    private readonly List<ManagerEmployeeDto> _availableEmployees;

    public CreateReceiptDto? ResultReceiptDto { get; private set; }

    public ReceiptEditorForm(
        List<SupplierOptionDto> suppliers,
        List<BookItemDto> books,
        List<ManagerEmployeeDto>? employees = null)
    {
        _availableSuppliers = suppliers;
        _availableBooks = books;
        _availableEmployees = employees ?? new List<ManagerEmployeeDto>();

        Text = "Tạo phiếu nhập kho";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Size = new Size(820, 640);
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
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // Chọn NCC
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Thêm sách & Giá nhập
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Lưới CTPNH
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Footer

        // 1. Nhà cung cấp & Nhân viên lập phiếu
        var pnlSupplier = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0)
        };
        pnlSupplier.Controls.Add(new Label { Text = "Nhà cung cấp:", AutoSize = true, Margin = new Padding(0, 6, 8, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(31, 111, 88) });
        _cboSupplier.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboSupplier.Width = 260;
        _cboSupplier.DisplayMember = nameof(SupplierOptionDto.Name);
        _cboSupplier.ValueMember = nameof(SupplierOptionDto.Id);
        _cboSupplier.DataSource = _availableSuppliers.ToList();
        pnlSupplier.Controls.Add(_cboSupplier);

        pnlSupplier.Controls.Add(new Label { Text = "Nhân viên nhập:", AutoSize = true, Margin = new Padding(16, 6, 8, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(31, 111, 88) });
        _cboEmployee.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboEmployee.Width = 240;
        var empList = _availableEmployees.Select(e => new { e.Id, Name = $"{e.LastName} {e.FirstName} ({e.Id})".Trim() }).ToList();
        _cboEmployee.DisplayMember = "Name";
        _cboEmployee.ValueMember = "Id";
        _cboEmployee.DataSource = empList;
        pnlSupplier.Controls.Add(_cboEmployee);

        mainLayout.Controls.Add(pnlSupplier, 0, 0);

        // 2. Thêm dòng nhập kho
        var pnlAddLine = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };
        pnlAddLine.Controls.Add(new Label { Text = "Sách:", AutoSize = true, Margin = new Padding(0, 6, 4, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });

        _cboBook.DropDownStyle = ComboBoxStyle.DropDown;
        _cboBook.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cboBook.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cboBook.Width = 260;
        var bookList = _availableBooks.Select(b => new { b.Id, Display = $"{b.Title} (Tồn: {b.AvailableStock})" }).ToList();
        _cboBook.DisplayMember = "Display";
        _cboBook.ValueMember = "Id";
        _cboBook.DataSource = bookList;
        pnlAddLine.Controls.Add(_cboBook);

        pnlAddLine.Controls.Add(new Label { Text = "SL:", AutoSize = true, Margin = new Padding(8, 6, 4, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
        _numQty.Minimum = 1;
        _numQty.Maximum = 10000;
        _numQty.Value = 10;
        _numQty.Width = 60;
        pnlAddLine.Controls.Add(_numQty);

        pnlAddLine.Controls.Add(new Label { Text = "Giá vốn (VNĐ):", AutoSize = true, Margin = new Padding(8, 6, 4, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
        _numCost.Minimum = 0;
        _numCost.Maximum = 1_000_000_000;
        _numCost.ThousandsSeparator = true;
        _numCost.DecimalPlaces = 0;
        _numCost.Value = 50000;
        _numCost.Width = 100;
        pnlAddLine.Controls.Add(_numCost);

        _btnAddLine.Text = "+ Thêm vào phiếu";
        _btnAddLine.AutoSize = true;
        _btnAddLine.Height = 28;
        _btnAddLine.BackColor = Color.FromArgb(31, 111, 88);
        _btnAddLine.ForeColor = Color.White;
        _btnAddLine.FlatStyle = FlatStyle.Flat;
        _btnAddLine.FlatAppearance.BorderSize = 0;
        _btnAddLine.Cursor = Cursors.Hand;
        _btnAddLine.Margin = new Padding(8, 1, 4, 0);
        _btnAddLine.Click += (_, _) => AddReceiptLine();
        pnlAddLine.Controls.Add(_btnAddLine);

        _btnRemoveLine.Text = "- Xóa dòng";
        _btnRemoveLine.AutoSize = true;
        _btnRemoveLine.Height = 28;
        _btnRemoveLine.BackColor = Color.FromArgb(255, 238, 237);
        _btnRemoveLine.ForeColor = Color.FromArgb(166, 57, 57);
        _btnRemoveLine.FlatStyle = FlatStyle.Flat;
        _btnRemoveLine.FlatAppearance.BorderSize = 0;
        _btnRemoveLine.Cursor = Cursors.Hand;
        _btnRemoveLine.Margin = new Padding(6, 1, 0, 0);
        _btnRemoveLine.Click += (_, _) => RemoveSelectedLine();
        pnlAddLine.Controls.Add(_btnRemoveLine);

        mainLayout.Controls.Add(pnlAddLine, 0, 1);

        // 3. Grid chi tiết phiếu nhập
        _gridLines.Dock = DockStyle.Fill;
        _gridLines.ReadOnly = true;
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

        SetupGridColumns();
        mainLayout.Controls.Add(_gridLines, 0, 2);

        // 4. Footer
        var pnlFooter = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        pnlFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        pnlFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _lblTotal.Text = "TỔNG GIÁ TRỊ NHẬP: 0 VNĐ";
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

        _btnSave.Text = "Lưu phiếu nhập";
        _btnSave.Size = new Size(130, 34);
        _btnSave.BackColor = Color.FromArgb(31, 111, 88);
        _btnSave.ForeColor = Color.White;
        _btnSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _btnSave.FlatStyle = FlatStyle.Flat;
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += (_, _) => SaveReceipt();

        pnlButtons.Controls.Add(_btnCancel);
        pnlButtons.Controls.Add(_btnSave);
        pnlFooter.Controls.Add(pnlButtons, 1, 0);

        mainLayout.Controls.Add(pnlFooter, 0, 3);
        Controls.Add(mainLayout);
    }

    private void SetupGridColumns()
    {
        _gridLines.Columns.Clear();

        var colBookId = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ReceiptLineItem.BookId),
            HeaderText = "Mã sách",
            FillWeight = 20
        };
        var colTitle = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ReceiptLineItem.Title),
            HeaderText = "Tên sách",
            FillWeight = 40
        };
        var colQty = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ReceiptLineItem.Quantity),
            HeaderText = "Số lượng nhập",
            FillWeight = 15
        };
        colQty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        var colCost = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ReceiptLineItem.UnitCost),
            HeaderText = "Giá vốn (VNĐ)",
            FillWeight = 20
        };
        colCost.DefaultCellStyle.Format = "N0";
        colCost.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        var colLineTotal = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(ReceiptLineItem.LineTotal),
            HeaderText = "Thành tiền (VNĐ)",
            FillWeight = 25
        };
        colLineTotal.DefaultCellStyle.Format = "N0";
        colLineTotal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        _gridLines.Columns.AddRange(colBookId, colTitle, colQty, colCost, colLineTotal);
    }

    private void RefreshGrid()
    {
        _gridLines.DataSource = null;
        _gridLines.DataSource = _lines.ToList();
        decimal total = _lines.Sum(x => x.LineTotal);
        _lblTotal.Text = $"TỔNG GIÁ TRỊ NHẬP: {total:N0} VNĐ ({_lines.Sum(x => x.Quantity)} quyển)";
    }

    private void AddReceiptLine()
    {
        var bookInput = _cboBook.Text.Trim();
        if (string.IsNullOrWhiteSpace(bookInput))
        {
            MessageBox.Show(this, "Vui lòng chọn hoặc gõ tên sách cần nhập.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _cboBook.Focus();
            return;
        }

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
                $"{b.Title} (Tồn: {b.AvailableStock})".Equals(bookInput, StringComparison.OrdinalIgnoreCase));
        }

        if (book == null)
        {
            book = _availableBooks.FirstOrDefault(b => b.Title.Contains(bookInput, StringComparison.OrdinalIgnoreCase));
        }

        if (book == null)
        {
            MessageBox.Show(this, $"Không tìm thấy sách phù hợp với từ khóa '{bookInput}'.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _cboBook.Focus();
            return;
        }

        int qty = (int)_numQty.Value;
        decimal cost = _numCost.Value;

        var existing = _lines.FirstOrDefault(l => l.BookId == book.Id);
        if (existing != null)
        {
            existing.Quantity += qty;
            existing.UnitCost = cost;
        }
        else
        {
            _lines.Add(new ReceiptLineItem
            {
                BookId = book.Id,
                Title = book.Title,
                Quantity = qty,
                UnitCost = cost
            });
        }

        RefreshGrid();
    }

    private void RemoveSelectedLine()
    {
        if (_gridLines.CurrentRow?.DataBoundItem is ReceiptLineItem item)
        {
            _lines.Remove(item);
            RefreshGrid();
        }
    }

    private void SaveReceipt()
    {
        if (_cboSupplier.SelectedValue is not string supplierId || string.IsNullOrWhiteSpace(supplierId))
        {
            MessageBox.Show(this, "Vui lòng chọn nhà cung cấp.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_lines.Count == 0)
        {
            MessageBox.Show(this, "Phiếu nhập phải có ít nhất 1 mặt hàng sách.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var lines = _lines.Select(l => new CreateReceiptLineDto(l.BookId, l.Quantity, l.UnitCost)).ToList();
        var employeeId = _cboEmployee.SelectedValue?.ToString();
        ResultReceiptDto = new CreateReceiptDto(supplierId, lines, employeeId);

        DialogResult = DialogResult.OK;
        Close();
    }
}
