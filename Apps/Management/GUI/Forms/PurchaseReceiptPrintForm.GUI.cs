using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

public sealed class PurchaseReceiptPrintForm : Form
{
    private readonly ManagerReceiptDto _receipt;
    private readonly PrintPreviewControl _preview = new();
    private readonly PrintDocument _doc = new();
    private readonly ToolStrip _toolBar = new();

    public PurchaseReceiptPrintForm(ManagerReceiptDto receipt)
    {
        _receipt = receipt;

        Text = $"Phiếu nhập kho - {_receipt.Id}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        WindowState = FormWindowState.Normal;
        Size = new Size(860, 850);
        MinimumSize = new Size(650, 600);
        BackColor = Color.White;

        _doc.DocumentName = $"PhieuNhap_{_receipt.Id}";
        _doc.PrintPage += OnPrintPage;

        InitComponents();
    }

    private void InitComponents()
    {
        _toolBar.GripStyle = ToolStripGripStyle.Hidden;
        _toolBar.BackColor = Color.FromArgb(248, 250, 252);
        _toolBar.Padding = new Padding(8, 4, 8, 4);

        var btnPrint = new ToolStripButton(" In phiếu nhập (Print / PDF)")
        {
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 111, 88),
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Padding = new Padding(8, 4, 8, 4)
        };
        btnPrint.Click += (_, _) =>
        {
            using var printDlg = new PrintDialog { Document = _doc, UseEXDialog = true };
            if (printDlg.ShowDialog(this) == DialogResult.OK)
            {
                try { _doc.Print(); }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Không thể in: {ex.Message}", "Lỗi in ấn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        };

        var btnZoomIn = new ToolStripButton(" Phóng to (+)")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Padding = new Padding(6, 4, 6, 4)
        };
        btnZoomIn.Click += (_, _) => _preview.Zoom = Math.Min(3.0, _preview.Zoom + 0.2);

        var btnZoomOut = new ToolStripButton(" Thu nhỏ (-)")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Padding = new Padding(6, 4, 6, 4)
        };
        btnZoomOut.Click += (_, _) => _preview.Zoom = Math.Max(0.5, _preview.Zoom - 0.2);

        var btnClose = new ToolStripButton("Đóng")
        {
            Alignment = ToolStripItemAlignment.Right,
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Padding = new Padding(12, 4, 12, 4)
        };
        btnClose.Click += (_, _) => Close();

        _toolBar.Items.AddRange(new ToolStripItem[] { btnPrint, new ToolStripSeparator(), btnZoomIn, btnZoomOut, btnClose });

        _preview.Dock = DockStyle.Fill;
        _preview.Document = _doc;
        _preview.Zoom = 1.0;
        _preview.BackColor = Color.FromArgb(220, 226, 235);

        Controls.Add(_preview);
        Controls.Add(_toolBar);
    }

    private void OnPrintPage(object sender, PrintPageEventArgs e)
    {
        var g = e.Graphics;
        if (g == null) return;

        using var fontTitle = new Font("Segoe UI", 16F, FontStyle.Bold);
        using var fontSubTitle = new Font("Segoe UI", 11F, FontStyle.Bold);
        using var fontRegular = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        using var fontBold = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        using var fontSmall = new Font("Segoe UI", 8.5F, FontStyle.Italic);

        using var brushDark = new SolidBrush(Color.FromArgb(24, 34, 28));
        using var brushGreen = new SolidBrush(Color.FromArgb(31, 111, 88));
        using var brushGray = new SolidBrush(Color.FromArgb(100, 116, 139));
        using var penLine = new Pen(Color.FromArgb(203, 213, 225), 1F);
        using var penHeader = new Pen(Color.FromArgb(31, 111, 88), 1.5F);

        float left = e.MarginBounds.Left;
        float top = e.MarginBounds.Top;
        float right = e.MarginBounds.Right;
        float width = e.MarginBounds.Width;
        float y = top;

        // 1. Header cửa hàng
        g.DrawString("NHÀ SÁCH TRANG SÁCH", fontSubTitle, brushGreen, left, y);
        y += 22;
        g.DrawString("Địa chỉ: 123 Đường Sách, TP. Hồ Chí Minh · Hotline: 090 123 4567", fontSmall, brushGray, left, y);
        y += 30;

        // 2. Tiêu đề Phiếu nhập kho
        var title = "PHIẾU NHẬP KHO";
        var titleSize = g.MeasureString(title, fontTitle);
        g.DrawString(title, fontTitle, brushDark, left + (width - titleSize.Width) / 2, y);
        y += 32;

        var codeStr = $"Mã phiếu: {_receipt.Id}";
        var codeSize = g.MeasureString(codeStr, fontBold);
        g.DrawString(codeStr, fontBold, brushGreen, left + (width - codeSize.Width) / 2, y);
        y += 26;

        g.DrawLine(penHeader, left, y, right, y);
        y += 14;

        // 3. Thông tin chung
        float col2 = left + width / 2;
        g.DrawString($"Ngày nhập: {_receipt.CreatedAt:dd/MM/yyyy HH:mm}", fontRegular, brushDark, left, y);
        g.DrawString($"Trạng thái: {OrderStatusFormatter.FormatReceiptStatus(_receipt.Status)}", fontRegular, brushDark, col2, y);
        y += 20;

        g.DrawString($"Nhà cung cấp: {_receipt.SupplierName} ({_receipt.SupplierId})", fontRegular, brushDark, left, y);
        var empStr = string.IsNullOrWhiteSpace(_receipt.EmployeeName) ? "Chưa phân công" : $"{_receipt.EmployeeName} ({_receipt.EmployeeId})";
        g.DrawString($"Nhân viên nhập: {empStr}", fontRegular, brushDark, col2, y);
        y += 28;

        // 4. Bảng chi tiết mặt hàng
        float colNo = left;
        float colId = left + 35;
        float colName = left + 130;
        float colQty = left + width - 260;
        float colPrice = left + width - 180;
        float colTotal = left + width - 85;

        // Header bảng
        g.DrawLine(penHeader, left, y, right, y);
        y += 4;
        g.DrawString("STT", fontBold, brushDark, colNo, y);
        g.DrawString("Mã sách", fontBold, brushDark, colId, y);
        g.DrawString("Tên mặt hàng sách", fontBold, brushDark, colName, y);
        g.DrawString("SL", fontBold, brushDark, colQty, y);
        g.DrawString("Giá vốn", fontBold, brushDark, colPrice, y);
        g.DrawString("Thành tiền", fontBold, brushDark, colTotal, y);
        y += 20;
        g.DrawLine(penHeader, left, y, right, y);
        y += 6;

        // Nội dung từng dòng
        int stt = 1;
        foreach (var line in _receipt.Lines)
        {
            g.DrawString(stt.ToString(), fontRegular, brushDark, colNo, y);
            g.DrawString(line.BookId, fontRegular, brushDark, colId, y);

            // Cắt tên nếu quá dài
            string titleDisplay = line.BookTitle;
            if (g.MeasureString(titleDisplay, fontRegular).Width > (colQty - colName - 10))
            {
                while (titleDisplay.Length > 3 && g.MeasureString(titleDisplay + "...", fontRegular).Width > (colQty - colName - 10))
                    titleDisplay = titleDisplay[..^1];
                titleDisplay += "...";
            }
            g.DrawString(titleDisplay, fontRegular, brushDark, colName, y);

            g.DrawString(line.Quantity.ToString("N0"), fontRegular, brushDark, colQty, y);
            g.DrawString(line.UnitCost.ToString("N0"), fontRegular, brushDark, colPrice, y);
            g.DrawString(line.LineTotal.ToString("N0"), fontRegular, brushDark, colTotal, y);

            y += 22;
            g.DrawLine(penLine, left, y, right, y);
            y += 4;
            stt++;
        }

        // 5. Tổng kết
        y += 10;
        float summaryCol1 = left + width - 240;
        float summaryCol2 = left + width - 90;

        g.DrawString("Tổng số mặt hàng:", fontBold, brushDark, summaryCol1, y);
        g.DrawString($"{_receipt.Lines.Count} loại ({_receipt.Lines.Sum(l => l.Quantity):N0} cuốn)", fontBold, brushDark, summaryCol2, y);
        y += 22;

        g.DrawString("TỔNG TIỀN NHẬP:", fontTitle, brushGreen, summaryCol1 - 40, y);
        g.DrawString($"{_receipt.TotalAmount:N0} đ", fontTitle, brushGreen, summaryCol2 - 20, y);
        y += 45;

        // 6. Chữ ký đối soát kế toán & thủ kho
        g.DrawLine(penLine, left, y, right, y);
        y += 15;

        float signCol1 = left + 40;
        float signCol2 = left + width / 3 + 20;
        float signCol3 = left + (width * 2) / 3;

        g.DrawString("Người giao hàng", fontBold, brushDark, signCol1, y);
        g.DrawString("Thủ kho / Người lập", fontBold, brushDark, signCol2, y);
        g.DrawString("Kế toán trưởng / Quản lý", fontBold, brushDark, signCol3, y);

        y += 16;
        g.DrawString("(Ký, ghi rõ họ tên)", fontSmall, brushGray, signCol1 - 5, y);
        g.DrawString("(Ký, ghi rõ họ tên)", fontSmall, brushGray, signCol2 - 5, y);
        g.DrawString("(Ký, đóng dấu)", fontSmall, brushGray, signCol3, y);
    }
}
