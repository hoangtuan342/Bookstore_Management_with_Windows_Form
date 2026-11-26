using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

public sealed class OrderInvoicePrintForm : Form
{
    private readonly ManagerOrderRowDto _order;
    private readonly PrintPreviewControl _preview = new();
    private readonly PrintDocument _doc = new();
    private readonly ToolStrip _toolBar = new();

    public OrderInvoicePrintForm(ManagerOrderRowDto order)
    {
        _order = order;

        Text = $"Hóa đơn bán hàng - {_order.Id}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        WindowState = FormWindowState.Normal;
        Size = new Size(860, 850);
        MinimumSize = new Size(650, 600);
        BackColor = Color.White;

        _doc.DocumentName = $"HoaDon_{_order.Id}";
        _doc.PrintPage += OnPrintPage;

        InitComponents();
    }

    private void InitComponents()
    {
        _toolBar.GripStyle = ToolStripGripStyle.Hidden;
        _toolBar.BackColor = Color.FromArgb(248, 250, 252);
        _toolBar.Padding = new Padding(8, 4, 8, 4);

        var btnPrint = new ToolStripButton(" In hóa đơn (Print)")
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
        using var fontSub = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        using var fontSubBold = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        using var fontHead = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        using var fontBody = new Font("Segoe UI", 9F, FontStyle.Regular);
        using var fontBodyBold = new Font("Segoe UI", 9F, FontStyle.Bold);
        using var fontTotal = new Font("Segoe UI", 11F, FontStyle.Bold);

        using var brushDark = new SolidBrush(Color.FromArgb(30, 41, 59));
        using var brushGreen = new SolidBrush(Color.FromArgb(31, 111, 88));
        using var brushMuted = new SolidBrush(Color.FromArgb(100, 116, 139));
        using var penLine = new Pen(Color.FromArgb(226, 232, 240), 1);
        using var penThick = new Pen(Color.FromArgb(31, 111, 88), 2);
        using var brushHeaderBg = new SolidBrush(Color.FromArgb(241, 245, 249));

        float left = e.MarginBounds.Left;
        float right = e.MarginBounds.Right;
        float width = e.MarginBounds.Width;
        float top = e.MarginBounds.Top;
        float y = top;

        // 1. Tiêu đề hiệu sách
        var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString("NHÀ SÁCH TRANG SÁCH", fontTitle, brushGreen, left + width / 2, y, sfCenter);
        y += 28;
        g.DrawString("Hệ thống phát hành sách & Văn hóa phẩm chuyên nghiệp", fontSub, brushMuted, left + width / 2, y, sfCenter);
        y += 18;
        g.DrawString("Địa chỉ: 54 Nguyễn Lương Bằng, Liên Chiểu, Đà Nẵng · Hotline: 1900 6868", fontSub, brushMuted, left + width / 2, y, sfCenter);
        y += 24;

        g.DrawLine(penThick, left, y, right, y);
        y += 16;

        // 2. Tên phiếu
        g.DrawString("HÓA ĐƠN BÁN LẺ", new Font("Segoe UI", 14F, FontStyle.Bold), brushDark, left + width / 2, y, sfCenter);
        y += 24;
        g.DrawString($"Mã HĐ: {_order.Id}  ·  Ngày lập: {_order.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}", fontSub, brushDark, left + width / 2, y, sfCenter);
        y += 26;

        // 3. Thông tin khách hàng & Nhân viên
        float colMid = left + width * 0.55f;
        g.DrawString("Khách hàng:", fontSubBold, brushDark, left, y);
        g.DrawString(_order.CustomerName, fontSub, brushDark, left + 90, y);

        g.DrawString("Thu ngân:", fontSubBold, brushDark, colMid, y);
        g.DrawString(string.IsNullOrWhiteSpace(_order.EmployeeName) ? "Tại quầy" : _order.EmployeeName, fontSub, brushDark, colMid + 75, y);
        y += 20;

        g.DrawString("Điện thoại:", fontSubBold, brushDark, left, y);
        g.DrawString(string.IsNullOrWhiteSpace(_order.ReceiverPhone) ? "Chưa có" : _order.ReceiverPhone, fontSub, brushDark, left + 90, y);

        g.DrawString("Hình thức TT:", fontSubBold, brushDark, colMid, y);
        var pm = _order.PaymentMethod == "BankTransfer" ? "Chuyển khoản ngân hàng" : "Tiền mặt (COD)";
        g.DrawString(pm, fontSub, brushDark, colMid + 95, y);
        y += 20;

        g.DrawString("Địa chỉ:", fontSubBold, brushDark, left, y);
        g.DrawString(string.IsNullOrWhiteSpace(_order.ShippingAddress) ? "Nhận tại quầy" : _order.ShippingAddress, fontSub, brushDark, left + 90, y);

        g.DrawString("Trạng thái:", fontSubBold, brushDark, colMid, y);
        g.DrawString($"{OrderStatusFormatter.FormatOrderStatus(_order.OrderStatus)} ({OrderStatusFormatter.FormatPaymentStatus(_order.PaymentStatus)})", fontSub, brushDark, colMid + 75, y);
        y += 30;

        // 4. Bảng chi tiết mặt hàng
        float colNo = left;
        float wNo = 40;
        float colTitle = colNo + wNo;
        float wTitle = width - 330;
        float colQty = colTitle + wTitle;
        float wQty = 60;
        float colPrice = colQty + wQty;
        float wPrice = 110;
        float colTotal = colPrice + wPrice;
        float wTotal = 120;

        // Header bảng
        g.FillRectangle(brushHeaderBg, left, y, width, 26);
        g.DrawRectangle(penLine, left, y, width, 26);

        var sfLeft = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
        var sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        var sfCenterAlign = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        g.DrawString("STT", fontHead, brushDark, new RectangleF(colNo, y, wNo, 26), sfCenterAlign);
        g.DrawString("Tên sách / Sản phẩm", fontHead, brushDark, new RectangleF(colTitle + 6, y, wTitle - 6, 26), sfLeft);
        g.DrawString("SL", fontHead, brushDark, new RectangleF(colQty, y, wQty, 26), sfCenterAlign);
        g.DrawString("Đơn giá", fontHead, brushDark, new RectangleF(colPrice, y, wPrice - 6, 26), sfRight);
        g.DrawString("Thành tiền", fontHead, brushDark, new RectangleF(colTotal, y, wTotal - 6, 26), sfRight);
        y += 26;

        // Các dòng sách
        int index = 1;
        foreach (var line in _order.Lines)
        {
            float rowH = 26;
            g.DrawRectangle(penLine, left, y, width, rowH);

            g.DrawString(index.ToString(), fontBody, brushDark, new RectangleF(colNo, y, wNo, rowH), sfCenterAlign);

            var bookTitle = line.Title;
            if (bookTitle.Length > 42) bookTitle = bookTitle.Substring(0, 39) + "...";
            g.DrawString(bookTitle, fontBody, brushDark, new RectangleF(colTitle + 6, y, wTitle - 6, rowH), sfLeft);

            g.DrawString(line.Quantity.ToString(), fontBody, brushDark, new RectangleF(colQty, y, wQty, rowH), sfCenterAlign);
            g.DrawString($"{line.UnitPrice:N0} ₫", fontBody, brushDark, new RectangleF(colPrice, y, wPrice - 6, rowH), sfRight);
            g.DrawString($"{line.LineTotal:N0} ₫", fontBodyBold, brushDark, new RectangleF(colTotal, y, wTotal - 6, rowH), sfRight);

            y += rowH;
            index++;
        }

        y += 15;

        // 5. Tổng kết
        float sumBoxW = 260;
        float sumBoxX = right - sumBoxW;

        g.DrawString($"Tổng số lượng:", fontSub, brushDark, sumBoxX, y);
        g.DrawString($"{_order.ItemCount} quyển", fontSubBold, brushDark, right - 6, y, sfRight);
        y += 20;

        g.DrawString("Tổng cộng thanh toán:", fontTotal, brushGreen, sumBoxX, y);
        g.DrawString($"{_order.TotalAmount:N0} VNĐ", fontTotal, brushGreen, right - 6, y, sfRight);
        y += 40;

        // 6. Lời cảm ơn & Chữ ký
        g.DrawLine(penLine, left, y, right, y);
        y += 18;

        float signW = width / 2;
        g.DrawString("Người mua hàng\n(Ký và ghi rõ họ tên)", fontSub, brushMuted, left + signW / 2, y, sfCenter);
        g.DrawString("Người lập hóa đơn\n(Ký và ghi rõ họ tên)", fontSub, brushMuted, right - signW / 2, y, sfCenter);
        y += 85;

        g.DrawString("Cảm ơn Quý khách và Hẹn gặp lại!", fontSubBold, brushGreen, left + width / 2, y, sfCenter);

        e.HasMorePages = false;
    }
}
