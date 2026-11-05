using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace RamReimagined.UI;

internal static class Theme
{
    public static Color Backdrop { get; } = Color.FromArgb(9, 11, 16);
    public static Color Rail { get; } = Color.FromArgb(13, 17, 24);
    public static Color Panel { get; } = Color.FromArgb(17, 22, 31);
    public static Color Card { get; } = Color.FromArgb(20, 26, 37);
    public static Color CardHover { get; } = Color.FromArgb(26, 33, 46);
    public static Color Elevated { get; } = Color.FromArgb(24, 31, 43);
    public static Color Stroke { get; } = Color.FromArgb(34, 42, 57);
    public static Color StrokeSoft { get; } = Color.FromArgb(27, 34, 46);
    public static Color Text { get; } = Color.FromArgb(233, 236, 245);
    public static Color Muted { get; } = Color.FromArgb(140, 150, 170);
    public static Color Faint { get; } = Color.FromArgb(96, 106, 126);
    public static Color Success { get; } = Color.FromArgb(46, 213, 168);
    public static Color Warning { get; } = Color.FromArgb(255, 184, 76);
    public static Color Danger { get; } = Color.FromArgb(255, 94, 120);

    public static Color Accent { get; private set; } = Color.FromArgb(124, 96, 255);

    public static readonly (string Name, Color Color)[] Accents =
    {
        ("Violet", Color.FromArgb(124, 96, 255)),
        ("Cyan", Color.FromArgb(56, 189, 248)),
        ("Emerald", Color.FromArgb(52, 211, 153)),
        ("Magenta", Color.FromArgb(232, 96, 214)),
        ("Amber", Color.FromArgb(251, 176, 64))
    };

    public static void SetAccent(string? name)
    {
        var match = Accents.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
        Accent = match.Color == default ? Accents[0].Color : match.Color;
    }

    public static Color AccentAlpha(int alpha) => Color.FromArgb(alpha, Accent);

    private static readonly Dictionary<(float Size, FontStyle Style), Font> FontCache = new();

    public static Font Font(float size, FontStyle style = FontStyle.Regular)
    {
        var key = (size, style);
        if (!FontCache.TryGetValue(key, out var font))
        {
            font = new Font("Segoe UI", size, style, GraphicsUnit.Point);
            FontCache[key] = font;
        }

        return font;
    }

    public static Font FontDisplay(float size, FontStyle style = FontStyle.Bold)
    {
        var key = (size + 100, style);
        if (!FontCache.TryGetValue(key, out var font))
        {
            font = new Font("Segoe UI Variable Display", size, style, GraphicsUnit.Point);
            FontCache[key] = font;
        }

        return font;
    }

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return path;
        }

        var r = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2);
        if (r <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var d = r * 2;
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void FillRounded(Graphics g, Rectangle bounds, int radius, Color color)
    {
        using var path = RoundedRect(bounds, radius);
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }

    public static void DrawRounded(Graphics g, Rectangle bounds, int radius, Color color, float width = 1f)
    {
        using var path = RoundedRect(new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1), radius);
        using var pen = new Pen(color, width);
        g.DrawPath(pen, path);
    }

    public static void FillGradient(Graphics g, Rectangle bounds, int radius, Color from, Color to, float angle = 35f)
    {
        using var path = RoundedRect(bounds, radius);
        using var brush = new LinearGradientBrush(bounds, from, to, angle);
        g.FillPath(brush, path);
    }

    public static void DrawText(Graphics g, string text, Font font, Color color, Rectangle bounds,
        TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis)
    {
        TextRenderer.DrawText(g, text, font, bounds, color, flags);
    }

    public static void DrawText(Graphics g, string text, Font font, Color color, Point location)
    {
        TextRenderer.DrawText(g, text, font, location, color, TextFormatFlags.Left | TextFormatFlags.Top);
    }

    public static Color WithAlpha(Color color, int alpha) => Color.FromArgb(alpha, color);

    public static void EnableQuality(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    }
}
