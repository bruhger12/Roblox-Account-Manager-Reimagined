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

/// <summary>Flat, hairline button. The primary action is bone ink inverted, not a glowing accent.</summary>
internal sealed class PillButton : Control
{
    private bool _hover;
    private bool _down;
    private ButtonKind _kind = ButtonKind.Ghost;
    private bool _busy;

    public PillButton(string text = "", IconKind? icon = null)
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        BackColor = Color.Transparent;
        Font = Theme.Font(9.25f, FontStyle.Bold);
        ForeColor = Theme.Text;
        Text = text;
        Icon = icon;
        Height = 36;
        TabStop = true;
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

    public int CornerRadius { get; set; } = Theme.RadiusControl;

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

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            e.Handled = true;
            OnClick(EventArgs.Empty);
            return;
        }

        base.OnKeyDown(e);
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
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        var (background, border, fore) = Palette();
        if (background.A > 0)
        {
            Theme.FillRounded(g, bounds, CornerRadius, background);
        }

        if (border.A > 0)
        {
            Theme.DrawRounded(g, bounds, CornerRadius, border);
        }

        var hasIcon = Icon is not null;
        var hasText = !string.IsNullOrEmpty(Text);
        const float iconSize = 15f;
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

        if (Focused)
        {
            Theme.DrawFocus(g, bounds, CornerRadius);
        }
    }

    private (Color Background, Color Border, Color Fore) Palette()
    {
        if (!Enabled)
        {
            return (Theme.WithAlpha(Theme.Elevated, 120), Theme.StrokeSoft, Theme.Faint);
        }

        return _kind switch
        {
            ButtonKind.Primary => (
                _down ? Color.FromArgb(214, 210, 202) : _hover ? Color.FromArgb(248, 246, 241) : Theme.Text,
                Color.Empty,
                Theme.Backdrop),

            ButtonKind.Danger => (
                _down ? Color.FromArgb(42, 210, 104, 92) : _hover ? Color.FromArgb(32, 210, 104, 92) : Color.FromArgb(20, 210, 104, 92),
                Theme.WithAlpha(Theme.Danger, 120),
                Theme.Danger),

            ButtonKind.Subtle => (
                _down ? Theme.Elevated : _hover ? Theme.CardHover : Theme.Card,
                _hover ? Theme.Stroke : Theme.StrokeSoft,
                Theme.Text),

            ButtonKind.Outline => (
                _hover ? Theme.CardHover : Color.Transparent,
                _hover ? Theme.Stroke : Theme.StrokeSoft,
                Theme.Text),

            _ => (
                _hover ? Theme.CardHover : Color.Transparent,
                Color.Empty,
                _hover ? Theme.Text : Theme.Muted)
        };
    }
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
        Size = new Size(36, 30);
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
            Theme.FillRounded(g, new Rectangle(2, 3, Width - 4, Height - 6), 6,
                _close ? Color.FromArgb(196, 74, 66) : Theme.CardHover);
        }

        Icons.Draw(g, Icon, new RectangleF(0, 0, Width, Height),
            _close && _hover ? Color.FromArgb(250, 249, 246) : Theme.Muted, 1.5f);
    }
}

/// <summary>Left-rail navigation: flat hover, one accent rule on the active section.</summary>
internal sealed class NavRailItem : Control
{
    private bool _hover;

    public NavRailItem(string text, IconKind icon)
    {
        Text = text;
        Icon = icon;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        BackColor = Color.Transparent;
        Font = Theme.Font(9.75f);
        Height = 38;
        Cursor = Cursors.Hand;
        TabStop = true;
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

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            e.Handled = true;
            OnClick(EventArgs.Empty);
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        var bounds = new Rectangle(10, 2, Width - 20, Height - 4);

        if (Selected)
        {
            Theme.FillRounded(g, bounds, Theme.RadiusControl, Theme.Card);
            Theme.FillRounded(g, new Rectangle(0, bounds.Y + 9, 2, bounds.Height - 18), 1, Theme.Accent);
        }
        else if (_hover)
        {
            Theme.FillRounded(g, bounds, Theme.RadiusControl, Theme.WithAlpha(Theme.CardHover, 160));
        }

        Icons.Draw(g, Icon, new RectangleF(24, (Height - 17) / 2f, 17, 17),
            Selected ? Theme.Text : Theme.Muted, 1.6f);

        Theme.DrawText(g, Text, Font, Selected ? Theme.Text : Theme.Muted,
            new Rectangle(50, 0, Width - 58, Height),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        if (Focused)
        {
            Theme.DrawFocus(g, bounds, Theme.RadiusControl);
        }
    }
}
