namespace RamReimagined.UI;

/// <summary>iOS-style switch used for boolean settings.</summary>
internal sealed class ToggleSwitch : Control
{
    private bool _checked;
    private bool _hover;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Size = new Size(44, 24);
        Cursor = Cursors.Hand;
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
            {
                return;
            }

            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CheckedChanged;

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

    protected override void OnMouseClick(MouseEventArgs e)
    {
        Checked = !Checked;
        base.OnMouseClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        var track = new Rectangle(0, (Height - 24) / 2, 44, 24);
        var on = _checked;
        Theme.FillRounded(g, track, 12, on
            ? Theme.Accent
            : _hover ? Theme.Stroke : Color.FromArgb(40, 49, 64));

        if (!on)
        {
            Theme.DrawRounded(g, track, 12, Theme.StrokeSoft);
        }

        var knob = new Rectangle(on ? 22 : 3, track.Y + 3, 18, 18);
        Theme.FillRounded(g, knob, 9, on ? Color.White : Color.FromArgb(180, 190, 205));
    }
}

/// <summary>Rounded single-line text input with placeholder support.</summary>
internal sealed class TextField : Control
{
    private readonly TextBox _box;
    private bool _hover;

    public TextField(string placeholder = "")
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Placeholder = placeholder;
        Cursor = Cursors.IBeam;

        // The inner TextBox must exist before any resize/layout runs, because
        // OnLayout positions it.
        _box = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Elevated,
            ForeColor = Theme.Text,
            Font = Theme.Font(9.75f),
            AutoSize = false,
            Multiline = false
        };
        _box.TextChanged += (_, _) => TextChanged?.Invoke(this, EventArgs.Empty);
        _box.GotFocus += (_, _) => Invalidate();
        _box.LostFocus += (_, _) => Invalidate();
        Controls.Add(_box);
        Height = 38;
    }

    public string Placeholder { get; set; }

    public new event EventHandler? TextChanged;

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text
    {
        get => _box.Text;
        set => _box.Text = value ?? string.Empty;
    }

    public bool UseSystemPasswordChar
    {
        get => _box.UseSystemPasswordChar;
        set => _box.UseSystemPasswordChar = value;
    }

    public void FocusInput() => _box.Focus();

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_box is null)
        {
            return;
        }

        _box.Bounds = new Rectangle(14, Math.Max(9, (Height - _box.PreferredHeight) / 2), Math.Max(10, Width - 28), _box.PreferredHeight);
        _box.BackColor = Theme.Card;
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
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        FocusInput();
        base.OnMouseClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        var bounds = new Rectangle(0, 0, Width, Height);
        Theme.FillRounded(g, bounds, 10, Theme.Card);
        Theme.DrawRounded(g, bounds, 10, _box.Focused ? Theme.AccentAlpha(170) : _hover ? Theme.Stroke : Theme.StrokeSoft);

        if (string.IsNullOrEmpty(_box.Text) && !_box.Focused)
        {
            Theme.DrawText(g, Placeholder, Theme.Font(9.5f), Theme.Faint, new Rectangle(15, 0, Width - 28, Height));
        }
    }
}

/// <summary>Vertical scrolling surface that lays its children out top-to-bottom.</summary>
internal sealed class StackPanel : Panel
{
    public StackPanel()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        BackColor = Color.Transparent;
        Padding = new Padding(0, 0, 8, 0);
    }

    public int Gap { get; set; } = 10;

    public override void Refresh()
    {
        base.Refresh();
    }

    public void Relayout()
    {
        var y = 0;
        foreach (Control child in Controls)
        {
            if (!child.Visible)
            {
                continue;
            }

            child.SetBounds(0, y, Math.Max(120, ClientSize.Width - Gap), child.Height);
            y += child.Height + Gap;
        }
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        Relayout();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        using var brush = new SolidBrush(Color.FromArgb(18, 22, 31));
        g.FillRectangle(brush, new Rectangle(ClientSize.Width - 6, 0, 6, ClientSize.Height));
        base.OnPaint(e);
    }
}
