using System.Drawing.Drawing2D;

namespace RamReimagined.UI;

internal enum ButtonKind
{
    Primary,
    Ghost,
    Subtle,
    Danger,
    Outline
}

/// <summary>Rounded, hand-painted button used everywhere in the shell.</summary>
internal sealed class PillButton : Control
{
    private bool _hover;
    private bool _down;
    private ButtonKind _kind = ButtonKind.Ghost;

    public PillButton(string text = "", IconKind? icon = null)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Font = Theme.Font(9.25f, FontStyle.Bold);
        ForeColor = Theme.Text;
        Text = text;
        Icon = icon;
        Height = 36;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
    }

    public IconKind? Icon { get; set; }

    public ButtonKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            Invalidate();
        }
    }

    public int CornerRadius { get; set; } = 10;

    public bool Busy
    {
        get => _busy;
        set
        {
            _busy = value;
            Enabled = !value;
            Invalidate();
        }
    }

    private bool _busy;

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _down = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _down = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _down = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        Invalidate();
        base.OnTextChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        var bounds = new Rectangle(0, 0, Width, Height);

        var (background, border, fore) = Palette();
        using var path = Theme.RoundedRect(bounds, CornerRadius);

        if (background.A > 0)
        {
            using var brush = new SolidBrush(background);
            g.FillPath(brush, path);
        }

        if (border.A > 0)
        {
            using var pen = new Pen(border, 1f);
            g.DrawPath(pen, path);
        }

        var hasIcon = Icon is not null;
        var hasText = !string.IsNullOrEmpty(Text);
        var iconSize = 16f;
        var gap = hasIcon && hasText ? 7f : 0f;
        var textSize = hasText ? TextRenderer.MeasureText(Text, Font).Width : 0;
        var total = (hasIcon ? iconSize : 0) + gap + textSize;
        var startX = (Width - total) / 2f;

        if (hasIcon)
        {
            Icons.Draw(g, Icon!.Value, new RectangleF(startX, (Height - iconSize) / 2f, iconSize, iconSize), fore);
            startX += iconSize + gap;
        }

        if (hasText)
        {
            var textBounds = new Rectangle((int)startX, 0, textSize + 2, Height);
            Theme.DrawText(g, Text, Font, fore, textBounds,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    private (Color Background, Color Border, Color Fore) Palette()
    {
        if (!Enabled)
        {
            return (Theme.WithAlpha(Theme.Elevated, 140), Theme.StrokeSoft, Theme.Faint);
        }

        return _kind switch
        {
            ButtonKind.Primary => _down
                ? (Shade(Theme.Accent, -18), Color.Empty, Color.White)
                : _hover ? (Shade(Theme.Accent, 22), Color.Empty, Color.White) : (Theme.Accent, Color.Empty, Color.White),

            ButtonKind.Danger => _down
                ? (Shade(Theme.Danger, -18), Color.Empty, Color.White)
                : _hover ? (Shade(Theme.Danger, 18), Color.Empty, Color.White) : (Theme.WithAlpha(Theme.Danger, 38), Theme.WithAlpha(Theme.Danger, 90), Theme.Danger),

            ButtonKind.Subtle => _down
                ? (Theme.Elevated, Theme.Stroke, Theme.Text)
                : _hover ? (Shade(Theme.Elevated, 10), Theme.Stroke, Theme.Text) : (Theme.CardHover, Theme.StrokeSoft, Theme.Text),

            ButtonKind.Outline => _hover
                ? (Theme.AccentAlpha(26), Theme.AccentAlpha(160), Theme.Text)
                : (Color.Transparent, Theme.Stroke, Theme.Text),

            _ => _hover
                ? (Theme.CardHover, Theme.StrokeSoft, Theme.Text)
                : (Color.Transparent, Color.Empty, Theme.Muted)
        };
    }

    private static Color Shade(Color color, int delta) => Color.FromArgb(
        color.A,
        Math.Clamp(color.R + delta, 0, 255),
        Math.Clamp(color.G + delta, 0, 255),
        Math.Clamp(color.B + delta, 0, 255));
}

internal sealed class WindowButton : Control
{
    private readonly bool _close;
    private bool _hover;

    public WindowButton(IconKind icon, bool close = false)
    {
        Icon = icon;
        _close = close;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(44, 34);
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    public IconKind Icon { get; }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        if (_hover)
        {
            Theme.FillRounded(g, new Rectangle(3, 4, Width - 6, Height - 8), 8,
                _close ? Color.FromArgb(232, 74, 98) : Theme.CardHover);
        }

        Icons.Draw(g, Icon, new RectangleF(0, 0, Width, Height),
            _close && _hover ? Color.White : Theme.Muted, 1.5f);
    }
}

/// <summary>Left-rail navigation item with an accent bar and tinted selection.</summary>
internal sealed class NavRailItem : Control
{
    private bool _hover;

    public NavRailItem(string text, IconKind icon)
    {
        Text = text;
        Icon = icon;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Font = Theme.Font(9.75f, FontStyle.Regular);
        Height = 42;
        Cursor = Cursors.Hand;
    }

    public IconKind Icon { get; }

    public bool Selected { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        var bounds = new Rectangle(10, 2, Width - 20, Height - 4);

        if (Selected)
        {
            using var path = Theme.RoundedRect(bounds, 10);
            using var brush = new LinearGradientBrush(bounds, Theme.AccentAlpha(46), Theme.AccentAlpha(14), 0f);
            g.FillPath(brush, path);
            Theme.FillRounded(g, new Rectangle(bounds.X + 1, bounds.Y + 9, 3, bounds.Height - 18), 2, Theme.Accent);
        }
        else if (_hover)
        {
            Theme.FillRounded(g, bounds, 10, Theme.CardHover);
        }

        Icons.Draw(g, Icon, new RectangleF(24, (Height - 18) / 2f, 18, 18),
            Selected ? Theme.Accent : Theme.Faint, 1.6f);

        Theme.DrawText(g, Text, Font, Selected ? Theme.Text : Theme.Muted,
            new Rectangle(52, 0, Width - 60, Height),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
