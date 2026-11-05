using RamReimagined.Core;

namespace RamReimagined.UI;

internal sealed class MainForm : Form
{
    private const int ResizeBorder = 6;

    private readonly AppState _app;
    private readonly RailPanel _rail;
    private readonly Panel _titleStrip;
    private readonly Panel _header;
    private readonly Label _heading;
    private readonly Label _subheading;
    private readonly FlowLayoutPanel _actions;
    private readonly Panel _pageHost;
    private readonly StatusToast _toast;
    private readonly System.Windows.Forms.Timer _presenceTimer;
    private readonly List<(PageView Page, NavRailItem Item)> _pages = new();
    private PageView? _current;
    private bool _refreshingPresence;

    public MainForm()
    {
        _app = new AppState();

        Text = "Roblox Account Manager Reimagined";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Theme.Backdrop;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1060, 690);
        ClientSize = new Size(Math.Clamp(_app.Settings.WindowWidth, 1060, 2400), Math.Clamp(_app.Settings.WindowHeight, 690, 1500));
        KeyPreview = true;
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);

        _rail = new RailPanel(_app) { Dock = DockStyle.Fill };

        _titleStrip = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Backdrop };
        var minimize = new WindowButton(IconKind.Minimize);
        minimize.Click += (_, _) => WindowState = FormWindowState.Minimized;
        var maximize = new WindowButton(IconKind.Maximize);
        maximize.Click += (_, _) => ToggleMaximize();
        var close = new WindowButton(IconKind.Close, close: true);
        close.Click += (_, _) => Close();
        _titleStrip.Controls.Add(minimize);
        _titleStrip.Controls.Add(maximize);
        _titleStrip.Controls.Add(close);
        _titleStrip.Resize += (_, _) =>
        {
            close.Location = new Point(_titleStrip.ClientSize.Width - 48, 6);
            maximize.Location = new Point(close.Left - 44, 6);
            minimize.Location = new Point(maximize.Left - 44, 6);
        };

        _header = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Backdrop };
        _heading = new Label
        {
            AutoSize = false,
            Font = Theme.FontDisplay(19f),
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = 34
        };
        _subheading = new Label
        {
            AutoSize = false,
            Font = Theme.Font(9.5f),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = 20
        };
        _actions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            Height = 40,
            Margin = Padding.Empty
        };
        _header.Controls.Add(_heading);
        _header.Controls.Add(_subheading);
        _header.Controls.Add(_actions);
        _header.Resize += (_, _) =>
        {
            _heading.SetBounds(32, 16, Math.Max(200, _header.ClientSize.Width - 420), 34);
            _subheading.SetBounds(32, 52, Math.Max(200, _header.ClientSize.Width - 420), 20);
            _actions.Location = new Point(_header.ClientSize.Width - 32 - _actions.Width, 26);
        };

        _pageHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Backdrop, Padding = new Padding(32, 0, 32, 22) };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Backdrop,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(_titleStrip, 0, 0);
        content.Controls.Add(_header, 0, 1);
        content.Controls.Add(_pageHost, 0, 2);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Backdrop,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 238));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(_rail, 0, 0);
        root.Controls.Add(content, 1, 0);

        Controls.Add(root);

        _toast = new StatusToast();
        Controls.Add(_toast);
        _toast.BringToFront();

        _app.ToastRequested += OnToast;

        BuildPages();

        _titleStrip.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                WindowChrome.Drag(this);
            }
        };
        _titleStrip.DoubleClick += (_, _) => ToggleMaximize();

        _presenceTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
        _presenceTimer.Tick += async (_, _) => await RefreshPresenceAsync();
        _presenceTimer.Start();

        Load += (_, _) =>
        {
            UpdateRegion();
            PositionToast();
            ShowPage(_pages[0].Page);
            _ = RefreshPresenceAsync();
            AppTimers.SetInterval(_presenceTimer, Math.Max(30, _app.Settings.RefreshSeconds) * 1000);
        };

        Resize += (_, _) =>
        {
            UpdateRegion();
            PositionToast();
        };

        FormClosing += (_, _) =>
        {
            _app.Settings.WindowWidth = WindowState == FormWindowState.Normal ? Width : _app.Settings.WindowWidth;
            _app.Settings.WindowHeight = WindowState == FormWindowState.Normal ? Height : _app.Settings.WindowHeight;
            _app.Settings.WindowMaximized = WindowState == FormWindowState.Maximized;
            _app.Settings.Save();
        };

        FormClosed += (_, _) =>
        {
            _presenceTimer.Stop();
            _presenceTimer.Dispose();
            _app.Dispose();
        };

        if (_app.Settings.WindowMaximized)
        {
            WindowState = FormWindowState.Maximized;
        }
    }

    private void BuildPages()
    {
        var accounts = new AccountsPage(_app);
        var servers = new ServersPage(_app);
        var tools = new ToolsPage(_app);
        var settings = new SettingsPage(_app);
        var about = new AboutPage(_app);

        accounts.OpenServersRequested += (_, _) => ShowPage(servers);

        AddPage(accounts);
        AddPage(servers);
        AddPage(tools);
        AddPage(settings);
        AddPage(about);
    }

    private void AddPage(PageView page)
    {
        var item = new NavRailItem(page.NavLabel, page.NavIcon);
        item.Click += (_, _) => ShowPage(page);
        _rail.AddItem(item);
        _pages.Add((page, item));
    }

    private void ShowPage(PageView page)
    {
        if (ReferenceEquals(_current, page))
        {
            return;
        }

        _current = page;
        _pageHost.SuspendLayout();
        _pageHost.Controls.Clear();
        _pageHost.Controls.Add(page);
        _pageHost.ResumeLayout();

        _heading.Text = page.Heading;
        _subheading.Text = page.Subheading;

        _actions.SuspendLayout();
        _actions.Controls.Clear();
        foreach (var action in page.Actions)
        {
            action.Margin = new Padding(8, 0, 0, 0);
            _actions.Controls.Add(action);
        }

        _actions.ResumeLayout();
        _header.PerformLayout();

        foreach (var (candidate, item) in _pages)
        {
            item.Selected = ReferenceEquals(candidate, page);
            item.Invalidate();
        }

        AppTimers.SetInterval(_presenceTimer, Math.Max(30, _app.Settings.RefreshSeconds) * 1000);
        page.Activated();
    }

    private void OnToast(string message, ToastKind kind)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnToast(message, kind)));
            return;
        }

        _toast.ShowMessage(message, kind);
        PositionToast();
    }

    private void PositionToast()
    {
        _toast.Location = new Point(
            Math.Max(12, ClientSize.Width - _toast.Width - 30),
            Math.Max(12, ClientSize.Height - _toast.Height - 26));
    }

    private async Task RefreshPresenceAsync()
    {
        if (_refreshingPresence)
        {
            return;
        }

        _refreshingPresence = true;
        try
        {
            await _app.RefreshPresenceAsync();
        }
        catch (Exception)
        {
            // Presence is decorative; a failure must not interrupt the UI.
        }
        finally
        {
            _refreshingPresence = false;
        }
    }

    private void ToggleMaximize() =>
        WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;

    private void UpdateRegion()
    {
        if (WindowState == FormWindowState.Maximized)
        {
            Region = null;
            return;
        }

        using var path = Theme.RoundedRect(new Rectangle(0, 0, Width, Height), 14);
        Region = new Region(path);
    }

    protected override void WndProc(ref Message m)
    {
        const int wmNcHitTest = 0x84;

        if (m.Msg == wmNcHitTest && WindowState == FormWindowState.Normal)
        {
            var raw = m.LParam.ToInt64();
            var screenPoint = new Point((short)(raw & 0xFFFF), (short)((raw >> 16) & 0xFFFF));
            var point = PointToClient(screenPoint);

            var left = point.X <= ResizeBorder;
            var right = point.X >= ClientSize.Width - ResizeBorder;
            var top = point.Y <= ResizeBorder;
            var bottom = point.Y >= ClientSize.Height - ResizeBorder;

            if (left && top) { m.Result = (IntPtr)13; return; }
            if (right && top) { m.Result = (IntPtr)14; return; }
            if (left && bottom) { m.Result = (IntPtr)16; return; }
            if (right && bottom) { m.Result = (IntPtr)17; return; }
            if (left) { m.Result = (IntPtr)10; return; }
            if (right) { m.Result = (IntPtr)11; return; }
            if (top) { m.Result = (IntPtr)12; return; }
            if (bottom) { m.Result = (IntPtr)15; return; }
        }

        base.WndProc(ref m);
    }
}

internal static class AppTimers
{
    public static void SetInterval(System.Windows.Forms.Timer timer, int milliseconds)
    {
        var interval = Math.Clamp(milliseconds, 5_000, 3_600_000);
        if (timer.Interval == interval)
        {
            return;
        }

        timer.Stop();
        timer.Interval = interval;
        timer.Start();
    }
}

/// <summary>Left navigation rail with the brand mark, section items and environment status.</summary>
internal sealed class RailPanel : Panel
{
    private readonly AppState _app;
    private readonly List<NavRailItem> _items = new();

    public RailPanel(AppState app)
    {
        _app = app;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Rail;
    }

    public void AddItem(NavRailItem item)
    {
        _items.Add(item);
        Controls.Add(item);
        LayoutItems();
    }

    private void LayoutItems()
    {
        var y = 108;
        foreach (var item in _items)
        {
            item.SetBounds(0, y, Width, 42);
            y += 44;
        }
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        LayoutItems();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);

        using (var brush = new SolidBrush(Theme.Rail))
        {
            g.FillRectangle(brush, ClientRectangle);
        }

        var mark = new Rectangle(20, 24, 38, 38);
        Theme.FillGradient(g, mark, 12, Theme.Accent, Color.FromArgb(64, 44, 168));
        var markFont = Theme.Font(17f, FontStyle.Bold);
        Theme.DrawText(g, "R", markFont, Color.White, mark,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        var titleFont = Theme.Font(11.5f, FontStyle.Bold);
        Theme.DrawText(g, "Reimagined", titleFont, Theme.Text, new Rectangle(68, 28, Width - 78, 20));
        var subFont = Theme.Font(7.75f);
        Theme.DrawText(g, "ROBLOX ACCOUNT MANAGER", subFont, Theme.Faint, new Rectangle(69, 46, Width - 78, 16));

        using (var pen = new Pen(Theme.StrokeSoft))
        {
            g.DrawLine(pen, 20, 88, Width - 20, 88);
        }

        // Footer: client detection + version.
        var footerY = Height - 74;
        var ready = _app.Launcher.PlayerPath is not null;
        Theme.FillRounded(g, new Rectangle(22, footerY + 4, 7, 7), 4, ready ? Theme.Success : Theme.Warning);
        Theme.DrawText(g, ready ? "Roblox client detected" : "Roblox client not found",
            Theme.Font(7.9f), ready ? Theme.Muted : Theme.Warning, new Rectangle(36, footerY - 4, Width - 48, 22));
        Theme.DrawText(g, "v1.0.0  ·  GPL-3.0  ·  fork of ic3w0lf22",
            Theme.Font(7.5f), Theme.Faint, new Rectangle(22, footerY + 22, Width - 32, 20));

        using (var pen = new Pen(Theme.StrokeSoft))
        {
            g.DrawLine(pen, Width - 1, 0, Width - 1, Height);
        }
    }
}
