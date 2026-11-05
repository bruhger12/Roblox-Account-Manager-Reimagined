namespace RamReimagined.UI;

/// <summary>Flat switch: filled bone when on, hairline track when off.</summary>
internal sealed class ToggleSwitch : Control
{
    private bool _checked;
    private bool _hover;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        BackColor = Color.Transparent;
        Size = new Size(42, 24);
        Cursor = Cursors.Hand;
        TabStop = true;
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
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            e.Handled = true;
            Checked = !Checked;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        var track = new Rectangle(0, (Height - 24) / 2, 42, 24);
        var on = _checked;

        Theme.FillRounded(g, track, 12, on ? Theme.Text : _hover ? Theme.Elevated : Theme.Card);
        Theme.DrawRounded(g, track, 12, on ? Theme.Text : Theme.Stroke);

        var knob = new Rectangle(on ? 21 : 3, track.Y + 3, 18, 18);
        Theme.FillRounded(g, knob, 9, on ? Theme.Backdrop : Theme.Muted);

        if (Focused)
        {
            Theme.DrawFocus(g, new Rectangle(0, 0, Width - 1, Height - 1), 12);
        }
    }
}

/// <summary>Flat text input with placeholder, optional multiline body and a visible focus rule.</summary>
internal sealed class TextField : Control
{
    private readonly TextBox _box;
    private bool _hover;
    private bool _multiline;

    public TextField(string placeholder = "")
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Placeholder = placeholder;
        Cursor = Cursors.IBeam;

        // The inner TextBox must exist before any resize/layout runs, because OnLayout positions it.
        _box = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Card,
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

    public bool Multiline
    {
        get => _multiline;
        set
        {
            if (_multiline == value)
            {
                return;
            }

            _multiline = value;
            _box.Multiline = value;
            _box.AcceptsReturn = value;
            _box.ScrollBars = value ? ScrollBars.Vertical : ScrollBars.None;
            _box.WordWrap = value;
            PerformLayout();
            Invalidate();
        }
    }

    public void FocusInput() => _box.Focus();

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_box is null)
        {
            return;
        }

        _box.BackColor = Theme.Card;
        if (_multiline)
        {
            _box.Bounds = new Rectangle(12, 10, Math.Max(10, Width - 24), Math.Max(20, Height - 20));
        }
        else
        {
            _box.Bounds = new Rectangle(12, Math.Max(9, (Height - _box.PreferredHeight) / 2), Math.Max(10, Width - 24),
                _box.PreferredHeight);
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
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        Theme.FillRounded(g, bounds, Theme.RadiusControl, Theme.Card);
        Theme.DrawRounded(g, bounds, Theme.RadiusControl,
            _box.Focused ? Theme.Accent : _hover ? Theme.Stroke : Theme.StrokeSoft);

        if (string.IsNullOrEmpty(_box.Text) && !_box.Focused)
        {
            var flags = _multiline
                ? TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak
                : TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            var placeholderBounds = _multiline
                ? new Rectangle(13, 11, Width - 26, Height - 20)
                : new Rectangle(13, 0, Width - 26, Height);
            Theme.DrawText(g, Placeholder, Theme.Font(9.5f), Theme.Faint, placeholderBounds, flags);
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
        Padding = new Padding(0, 0, 10, 0);
    }

    public int Gap { get; set; } = 10;

    public void Relayout()
    {
        var y = 0;
        foreach (Control child in Controls)
        {
            if (!child.Visible)
            {
                continue;
            }

            child.SetBounds(0, y, Math.Max(120, ClientSize.Width - Padding.Right), child.Height);
            y += child.Height + Gap;
        }

        AutoScrollMinSize = new Size(0, y);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        Relayout();
        Invalidate();
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        Relayout();
    }
}
