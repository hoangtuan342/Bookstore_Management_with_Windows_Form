using System.Drawing.Drawing2D;

namespace TrangSach.Management.GUI;

public partial class ManagementForm
{
    internal static readonly Image SharedBackgroundArtwork = CreateBackgroundArtwork();

    private static Bitmap CreateBackgroundArtwork()
    {
        const int width = 1920, height = 1200;
        var image = new Bitmap(width, height);
        using var g = Graphics.FromImage(image);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var wall = new LinearGradientBrush(new Rectangle(0, 0, width, height), Color.FromArgb(255, 246, 226), Color.FromArgb(228, 244, 242), 28f))
            g.FillRectangle(wall, 0, 0, width, height);

        using (var glow = new SolidBrush(Color.FromArgb(45, 255, 148, 68))) g.FillEllipse(glow, -150, 80, 650, 520);
        using (var glow = new SolidBrush(Color.FromArgb(48, 51, 188, 174))) g.FillEllipse(glow, 1390, 10, 690, 540);
        using (var glow = new SolidBrush(Color.FromArgb(39, 133, 104, 197))) g.FillEllipse(glow, 1420, 820, 650, 480);
        using (var glow = new SolidBrush(Color.FromArgb(38, 229, 105, 90))) g.FillEllipse(glow, -120, 810, 560, 500);

        DrawShelf(g, 0, 0, 425, 420, [Color.FromArgb(224,91,82),Color.FromArgb(51,146,166),Color.FromArgb(239,187,59),Color.FromArgb(132,99,181),Color.FromArgb(83,157,104),Color.FromArgb(230,129,71)]);
        DrawShelf(g, 1510, 0, 410, 390, [Color.FromArgb(230,104,83),Color.FromArgb(69,142,200),Color.FromArgb(237,191,58),Color.FromArgb(151,108,188),Color.FromArgb(71,160,117)]);
        DrawBookStack(g, 80, 900);
        DrawPlant(g, 1660, 520);
        DrawOpenBook(g, 1190, 865);

        using (var wash = new SolidBrush(Color.FromArgb(48, 255, 253, 247))) g.FillRectangle(wash, 430, 0, 1060, height);
        return image;
    }

    private static void DrawShelf(Graphics g, int x, int y, int width, int height, Color[] colors)
    {
        using (var backing = new SolidBrush(Color.FromArgb(54, 242, 194, 125))) g.FillRectangle(backing, x, y + 22, width, height - 42);
        using var wood = new LinearGradientBrush(new Rectangle(x, y, width, height), Color.FromArgb(192,117,65), Color.FromArgb(129,75,53), 90f);
        var bookX = x + 20;
        for (var i = 0; i < colors.Length; i++)
        {
            var bookWidth = 38 + (i * 13 % 25);
            var bookHeight = height * 2 / 3 + (i * 19 % Math.Max(30, height / 5));
            var top = y + 32 + (height - bookHeight) / 2;
            using var cover = new SolidBrush(Color.FromArgb(216, colors[i]));
            using var page = new SolidBrush(Color.FromArgb(224, 255, 232, 193));
            g.FillRectangle(cover, bookX, top, bookWidth, bookHeight);
            g.FillRectangle(page, bookX + 7, top + 10, bookWidth - 14, bookHeight - 24);
            using var stripe = new Pen(Color.FromArgb(190, colors[i]), 3f);
            g.DrawLine(stripe, bookX + bookWidth / 2, top + 18, bookX + bookWidth / 2, top + bookHeight - 32);
            bookX += bookWidth + 7;
            if (bookX > x + width - 48) break;
        }
        g.FillRectangle(wood, x, y, width, 23);
        g.FillRectangle(wood, x, y + height - 20, width, 20);
    }

    private static void DrawBookStack(Graphics g, int x, int y)
    {
        Color[] colors=[Color.FromArgb(47,126,144),Color.FromArgb(238,187,62),Color.FromArgb(224,111,91),Color.FromArgb(142,105,177)];
        for (var i = 0; i < colors.Length; i++)
        {
            var inset = i * 11;
            using var cover = new SolidBrush(Color.FromArgb(218, colors[i]));
            g.FillRectangle(cover, x + inset, y + i * 43, 260 - inset, 36);
            using var line = new Pen(Color.FromArgb(220, 255, 235, 201), 3f);
            g.DrawLine(line, x + inset + 22, y + i * 43 + 27, x + 235, y + i * 43 + 27);
        }
    }

    private static void DrawPlant(Graphics g, int x, int y)
    {
        using var leaves = new SolidBrush(Color.FromArgb(212, 55, 151, 104));
        using var stem = new Pen(Color.FromArgb(210, 49, 130, 91), 7f);
        g.DrawLine(stem, x + 85, y + 170, x + 85, y + 15);
        g.FillEllipse(leaves, x + 28, y + 63, 73, 30); g.FillEllipse(leaves, x + 82, y + 39, 74, 29);
        g.FillEllipse(leaves, x + 13, y + 19, 78, 30); g.FillEllipse(leaves, x + 89, y - 10, 73, 31);
        using var pot = new SolidBrush(Color.FromArgb(218, 238, 119, 91));
        g.FillPolygon(pot, [new Point(x + 30, y + 160), new Point(x + 140, y + 160), new Point(x + 126, y + 225), new Point(x + 44, y + 225)]);
    }

    private static void DrawOpenBook(Graphics g, int x, int y)
    {
        using var left = new SolidBrush(Color.FromArgb(228, 255, 253, 235));
        using var right = new SolidBrush(Color.FromArgb(228, 255, 247, 219));
        using var outline = new Pen(Color.FromArgb(210, 193, 139, 77), 7f);
        g.FillPie(left, x, y, 240, 190, 180, 180); g.FillPie(right, x + 225, y, 240, 190, 180, 180);
        g.DrawArc(outline, x, y, 240, 190, 180, 180); g.DrawArc(outline, x + 225, y, 240, 190, 180, 180);
        using var line = new Pen(Color.FromArgb(150, 61, 151, 142), 5f);
        for (var i = 0; i < 3; i++) { g.DrawLine(line, x + 55, y + 74 + i * 24, x + 187, y + 74 + i * 24); g.DrawLine(line, x + 293, y + 74 + i * 24, x + 425, y + 74 + i * 24); }
    }
}
