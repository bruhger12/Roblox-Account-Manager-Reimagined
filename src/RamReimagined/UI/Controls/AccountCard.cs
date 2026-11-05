using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace RamReimagined.UI;

/// <summary>Loads avatar headshots once per session and keeps them in memory.</summary>
internal sealed class AvatarCache
{
    private readonly Core.RobloxApi _api;
    private readonly Dictionary<long, Image?> _cache = new();

    public AvatarCache(Core.RobloxApi api) => _api = api;

    public Image? Cached(long userId) => _cache.TryGetValue(userId, out var image) ? image : null;

    public void Forget(long userId) => _cache.Remove(userId);

    public async Task<Image?> GetAsync(long userId, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(userId, out var cached))
        {
            return cached;
        }

        try
        {
            var url = await _api.GetAvatarHeadshotUrlAsync(userId, ct).ConfigureAwait(false);
            if (url is null)
            {
                _cache[userId] = null;
                return null;
            }

            var bytes = await _api.DownloadAsync(url, ct).ConfigureAwait(false);
            if (bytes is null)
            {
                _cache[userId] = null;
                return null;
            }

            using var stream = new MemoryStream(bytes);
            using var decoded = Image.FromStream(stream);
            var image = (Image)decoded.Clone();
            _cache[userId] = image;
            return image;
        }
        catch (Exception)
        {
            _cache[userId] = null;
            return null;
        }
    }
}

/// <summary>An account row: selection, avatar, identity, presence and quick actions.</summary>
internal sealed class AccountCard : Control
{
    private readonly ToolTip _tip = new() { InitialDelay = 400, AutoPopDelay = 8000 };
    private readonly ContextMenuStrip _menu;
    private Image? _avatar;
    private Core.PresenceInfo? _presence;
    private bool _hover;
    private bool _selected;
    private Rectangle _launchRect;
    private Rectangle _menuRect;
    private Rectangle _checkRect;

    public AccountCard(Core.Account account)
    {
        Account = account;
        Height = 84;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Backdrop;
        Font = Theme.Font(9.5f);

        _menu = new ContextMenuStrip { BackColor = Theme.Elevated, ForeColor = Theme.Text, ShowImageMargin = false };
        _menu.Renderer = new DarkMenuRenderer();
        _menu.Items.Add(MenuItem("Open servers", IconKind.Server, () => ServersRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(MenuItem("Copy cookie", IconKind.Copy, () => CopyCookieRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(MenuItem("Copy user ID", IconKind.Copy, () => CopyUserIdRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(MenuItem(Account.Enabled ? "Mute in batch launch" : "Include in batch launch", IconKind.Check,
            () => ToggleEnabledRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(MenuItem("Set tag", IconKind.Sparkle, () => TagRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(MenuItem("Remove account", IconKind.Trash, () => RemoveRequested?.Invoke(this, EventArgs.Empty)));
    }

    public Core.Account Account { get; }

    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
            {
                return;
            }

            _selected = value;
            Invalidate();
            SelectedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? SelectedChanged;
    public event EventHandler? LaunchRequested;
    public event EventHandler? ServersRequested;
    public event EventHandler? CopyCookieRequested;
    public event EventHandler? CopyUserIdRequested;
    public event EventHandler? ToggleEnabledRequested;
    public event EventHandler? TagRequested;
    public event EventHandler? RemoveRequested;

    public void SetAvatar(Image? image)
    {
        _avatar = image;
        Invalidate();
    }

    public void SetPresence(Core.PresenceInfo? presence)
    {
        _presence = presence;
        Invalidate();
    }

    public void RefreshFromAccount()
    {
        Invalidate();
    }

    private static ToolStripMenuItem MenuItem(string text, IconKind icon, Action action)
    {
        var item = new ToolStripMenuItem(text) { ForeColor = Theme.Text, Padding = new Padding(4, 3, 8, 3) };
        item.Click += (_, _) => action();
        _ = icon;
        return item;
    }

    private void ComputeRects()
    {
        var right = Width - 18;
        _launchRect = new Rectangle(right - 92, (Height - 32) / 2, 92, 32);
        _menuRect = new Rectangle(_launchRect.Left - 40, (Height - 32) / 2, 32, 32);
        _checkRect = new Rectangle(18, (Height - 20) / 2, 20, 20);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ComputeRects();
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

    protected override void OnMouseMove(MouseEventArgs e)
    {
        ComputeRects();
        Cursor = _launchRect.Contains(e.Location) || _menuRect.Contains(e.Location) || _checkRect.Contains(e.Location)
            ? Cursors.Hand
            : Cursors.Default;
        base.OnMouseMove(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        ComputeRects();
        if (_checkRect.Contains(e.Location))
        {
            Selected = !Selected;
        }
        else if (_launchRect.Contains(e.Location))
        {
            LaunchRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (_menuRect.Contains(e.Location))
        {
            _menu.Show(this, new Point(Math.Max(0, _menuRect.Right - 180), _menuRect.Bottom + 6));
        }
        else
        {
            Selected = !Selected;
        }

        base.OnMouseClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        var background = _selected ? Theme.AccentAlpha(18) : _hover ? Theme.CardHover : Theme.Card;
        Theme.FillRounded(g, bounds, 14, background);
        Theme.DrawRounded(g, bounds, 14, _selected ? Theme.AccentAlpha(150) : Theme.StrokeSoft);

        DrawCheck(g);

        var avatarRect = new Rectangle(48, (Height - 48) / 2, 48, 48);
        DrawAvatar(g, avatarRect);

        var textLeft = 112;
        var textRight = _menuRect.Left - 12;
        var nameWidth = textRight - textLeft;

        var nameFont = Theme.Font(10.5f, FontStyle.Bold);
        Theme.DrawText(g, Account.Name, nameFont, Theme.Text,
            new Rectangle(textLeft, 20, Math.Max(40, nameWidth), 20),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        var handle = string.IsNullOrWhiteSpace(Account.Username) ? $"ID {Account.UserId}" : $"@{Account.Username}";
        var tagSuffix = string.IsNullOrWhiteSpace(Account.Tag) ? "" : $"  ·  {Account.Tag}";
        var smallFont = Theme.Font(8.75f);
        Theme.DrawText(g, $"{handle}  ·  {Account.UserId}{tagSuffix}", smallFont, Theme.Muted,
            new Rectangle(textLeft, 41, Math.Max(40, nameWidth), 18),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        DrawPresence(g);
        DrawLaunch(g);
        DrawMenu(g);
    }

    private void DrawCheck(Graphics g)
    {
        var on = _selected;
        Theme.FillRounded(g, _checkRect, 6, on ? Theme.Accent : Theme.WithAlpha(Theme.Elevated, 220));
        Theme.DrawRounded(g, _checkRect, 6, on ? Theme.Accent : Theme.Stroke);
        if (on)
        {
            Icons.Draw(g, IconKind.Check, new RectangleF(_checkRect.X, _checkRect.Y, _checkRect.Width, _checkRect.Height),
                Color.White, 2.1f);
        }
    }

    private void DrawAvatar(Graphics g, Rectangle rect)
    {
        using var path = Theme.RoundedRect(rect, 14);
        if (_avatar is not null)
        {
            var state = g.Save();
            g.SetClip(path);
            g.DrawImage(_avatar, rect);
            g.Restore(state);
        }
        else
        {
            using var gradient = new LinearGradientBrush(rect, Theme.AccentAlpha(150), Theme.AccentAlpha(60), 45f);
            g.FillPath(gradient, path);
            var font = Theme.Font(14f, FontStyle.Bold);
            Theme.DrawText(g, Account.Initials, font, Color.White, rect,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // Presence ring
        var color = PresenceColor();
        if (color != Color.Empty)
        {
            using var ring = new Pen(Theme.Backdrop, 3f);
            using var path2 = Theme.RoundedRect(new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4), 16);
            g.DrawPath(ring, path2);
            Theme.FillRounded(g, new Rectangle(rect.Right - 12, rect.Bottom - 12, 16, 16), 8, Theme.Backdrop);
            Theme.FillRounded(g, new Rectangle(rect.Right - 10, rect.Bottom - 10, 12, 12), 6, color);
        }
    }

    private Color PresenceColor() => _presence?.Type switch
    {
        2 => Theme.Success,
        1 => Color.FromArgb(86, 180, 240),
        3 => Color.FromArgb(190, 140, 255),
        _ => Color.Empty
    };

    private void DrawPresence(Graphics g)
    {
        var state = _presence;
        var text = state is null ? "Checking" : state.State;
        var color = state is null ? Theme.Faint : PresenceColor() is var c && c != Color.Empty ? c : Theme.Faint;

        var width = TextRenderer.MeasureText(text, Theme.Font(8.5f, FontStyle.Bold)).Width + 30;
        var rect = new Rectangle(_menuRect.Left - width - 12, (Height - 26) / 2, width, 26);
        Theme.FillRounded(g, rect, 13, Theme.WithAlpha(color, 26));
        Theme.DrawRounded(g, rect, 13, Theme.WithAlpha(color, 60));
        Theme.FillRounded(g, new Rectangle(rect.X + 10, rect.Y + 11, 5, 5), 3, color);
        var font = Theme.Font(8.5f, FontStyle.Bold);
        Theme.DrawText(g, text, font, color, new Rectangle(rect.X + 20, rect.Y, rect.Width - 26, rect.Height));

        if (state is not null)
        {
            _tip.SetToolTip(this, $"{state.State} — {state.Detail}");
        }
    }

    private void DrawLaunch(Graphics g)
    {
        var enabled = _hover;
        Theme.FillRounded(g, _launchRect, 9, enabled ? Theme.Accent : Theme.AccentAlpha(200));
        Icons.Draw(g, IconKind.Play, new RectangleF(_launchRect.X + 12, _launchRect.Y + 9, 14, 14), Color.White, 1.6f);
        var font = Theme.Font(8.75f, FontStyle.Bold);
        Theme.DrawText(g, "Launch", font, Color.White, new Rectangle(_launchRect.X + 30, _launchRect.Y, _launchRect.Width - 34, _launchRect.Height),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }

    private void DrawMenu(Graphics g)
    {
        Theme.FillRounded(g, _menuRect, 9, Theme.WithAlpha(Theme.Elevated, _hover ? 230 : 150));
        Icons.Draw(g, IconKind.More, new RectangleF(_menuRect.X + 6, _menuRect.Y + 6, 20, 20), Theme.Muted, 1.4f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _menu.Dispose();
            _tip.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColors())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item?.ForeColor ?? Theme.Text;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(Theme.Stroke);
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        private sealed class DarkColors : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Theme.Elevated;
            public override Color ImageMarginGradientBegin => Theme.Elevated;
            public override Color ImageMarginGradientMiddle => Theme.Elevated;
            public override Color ImageMarginGradientEnd => Theme.Elevated;
            public override Color MenuItemSelected => Theme.CardHover;
            public override Color MenuItemBorder => Theme.Stroke;
            public override Color SeparatorDark => Theme.Stroke;
            public override Color SeparatorLight => Theme.Elevated;
        }
    }
}
