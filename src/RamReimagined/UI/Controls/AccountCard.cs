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

/// <summary>An account row: selection, avatar, identity, local note, presence and quick actions.</summary>
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
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);
        BackColor = Theme.Backdrop;
        Font = Theme.Font(9.5f);
        TabStop = true;

        _menu = DarkMenu.Create();
        _menu.Items.Add(DarkMenu.Item("Open in browser", () => BrowserRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(DarkMenu.Item("Open servers", () => ServersRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(DarkMenu.Item("Copy cookie", () => CopyCookieRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(DarkMenu.Item("Copy user ID", () => CopyUserIdRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(DarkMenu.Separator());
        _menu.Items.Add(DarkMenu.Item(Account.Enabled ? "Mute in batch launch" : "Include in batch launch",
            () => ToggleEnabledRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(DarkMenu.Item("Set tag", () => TagRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(DarkMenu.Item("Edit notes", () => NotesRequested?.Invoke(this, EventArgs.Empty)));
        _menu.Items.Add(DarkMenu.Separator());
        _menu.Items.Add(DarkMenu.Item("Remove account", () => RemoveRequested?.Invoke(this, EventArgs.Empty), danger: true));
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
    public event EventHandler? BrowserRequested;
    public event EventHandler? CopyCookieRequested;
    public event EventHandler? CopyUserIdRequested;
    public event EventHandler? ToggleEnabledRequested;
    public event EventHandler? TagRequested;
    public event EventHandler? NotesRequested;
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

    public void RefreshFromAccount() => Invalidate();

    private void ComputeRects()
    {
        var right = Width - 14;
        _menuRect = new Rectangle(right - 28, (Height - 28) / 2, 28, 28);
        _launchRect = new Rectangle(_menuRect.Left - 10 - 88, (Height - 30) / 2, 88, 30);
        _checkRect = new Rectangle(14, (Height - 18) / 2, 18, 18);
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
            Focus();
        }
        else if (_launchRect.Contains(e.Location))
        {
            LaunchRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (_menuRect.Contains(e.Location))
        {
            _menu.Show(this, new Point(Math.Max(0, _menuRect.Right - 200), _menuRect.Bottom + 6));
        }
        else
        {
            Selected = !Selected;
            Focus();
        }

        base.OnMouseClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Enter:
                e.Handled = true;
                LaunchRequested?.Invoke(this, EventArgs.Empty);
                return;
            case Keys.Space:
                e.Handled = true;
                Selected = !Selected;
                return;
            case Keys.Apps:
                e.Handled = true;
                _menu.Show(this, new Point(_menuRect.Left, _menuRect.Bottom + 4));
                return;
        }

        base.OnKeyDown(e);
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

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        var background = _selected ? Theme.Card : _hover ? Theme.WithAlpha(Theme.CardHover, 190) : Theme.Card;
        Theme.FillRounded(g, bounds, Theme.RadiusCard, background);
        Theme.DrawRounded(g, bounds, Theme.RadiusCard, _selected ? Theme.Accent : Theme.StrokeSoft);

        DrawCheck(g);

        var avatarRect = new Rectangle(42, (Height - 46) / 2, 46, 46);
        DrawAvatar(g, avatarRect);

        var textLeft = 100;
        var textRight = _launchRect.Left - 14;
        var nameWidth = Math.Max(40, textRight - textLeft);

        var nameFont = Theme.Font(10.8f, FontStyle.Bold);
        Theme.DrawText(g, Account.Name, nameFont, Account.Enabled ? Theme.Text : Theme.Muted,
            new Rectangle(textLeft, 13, nameWidth, 20),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        DrawIdentityLine(g, textLeft, 33, nameWidth);

        var noteFont = Theme.Font(8.9f);
        if (!string.IsNullOrWhiteSpace(Account.Notes))
        {
            Theme.DrawText(g, Account.Notes.Replace('\n', ' '), noteFont, Theme.Muted,
                new Rectangle(textLeft, 51, nameWidth, 18),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
        else if (!string.IsNullOrWhiteSpace(Account.Tag))
        {
            Theme.DrawText(g, Account.Tag.ToUpperInvariant(), Theme.Mono(9f), Theme.Faint,
                new Rectangle(textLeft, 51, nameWidth, 18),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
        else if (!Account.Enabled)
        {
            Theme.DrawText(g, "Muted for batches", noteFont, Theme.Faint,
                new Rectangle(textLeft, 51, nameWidth, 18),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        DrawPresence(g);
        DrawLaunch(g);
        DrawMenu(g);

        if (Focused)
        {
            Theme.DrawFocus(g, bounds, Theme.RadiusCard);
        }
    }

    private void DrawIdentityLine(Graphics g, int left, int y, int width)
    {
        var handle = string.IsNullOrWhiteSpace(Account.Username) ? "" : $"@{Account.Username}";
        var handleFont = Theme.Font(9f);
        var handleWidth = string.IsNullOrWhiteSpace(handle) ? 0 : TextRenderer.MeasureText(handle, handleFont).Width;
        Theme.DrawText(g, handle, handleFont, Theme.Muted, new Rectangle(left, y, Math.Max(10, width), 17),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        var idText = Account.UserId > 0 ? Account.UserId.ToString() : "no id";
        Theme.DrawText(g, idText, Theme.Mono(9f), Theme.Faint,
            new Rectangle(left + handleWidth + 8, y, Math.Max(10, width - handleWidth - 8), 17),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void DrawCheck(Graphics g)
    {
        Theme.FillRounded(g, _checkRect, 5, _selected ? Theme.Accent : Theme.Card);
        Theme.DrawRounded(g, _checkRect, 5, _selected ? Theme.Accent : Theme.Stroke);
        if (_selected)
        {
            Icons.Draw(g, IconKind.Check,
                new RectangleF(_checkRect.X + 1, _checkRect.Y + 1, _checkRect.Width - 2, _checkRect.Height - 2),
                Theme.InkOn(Theme.Accent), 2.1f);
        }
    }

    private void DrawAvatar(Graphics g, Rectangle rect)
    {
        using var path = Theme.RoundedRect(rect, 8);
        if (_avatar is not null)
        {
            var state = g.Save();
            g.SetClip(path);
            g.DrawImage(_avatar, rect);
            g.Restore(state);
        }
        else
        {
            using var brush = new SolidBrush(Theme.Elevated);
            g.FillPath(brush, path);
            Theme.DrawText(g, Account.Initials, Theme.Font(13f, FontStyle.Bold), Theme.Text, rect,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        var color = PresenceColor();
        if (color != Color.Empty)
        {
            Theme.FillRounded(g, new Rectangle(rect.Right - 12, rect.Bottom - 12, 12, 12), 6, Theme.Backdrop);
            Theme.FillRounded(g, new Rectangle(rect.Right - 10, rect.Bottom - 10, 8, 8), 4, color);
        }
    }

    private Color PresenceColor() => _presence?.Type switch
    {
        2 => Theme.Success,
        1 => Color.FromArgb(126, 158, 184),
        3 => Theme.Warning,
        _ => Color.Empty
    };

    private void DrawPresence(Graphics g)
    {
        var state = _presence;
        var text = state is null ? "Checking" : state.State;
        var color = state is null ? Theme.Faint : PresenceColor() is var c && c != Color.Empty ? c : Theme.Faint;
        var font = Theme.Font(8.9f);
        var width = TextRenderer.MeasureText(text, font).Width + 16;
        var rect = new Rectangle(_launchRect.Left - 12 - width, (Height - 22) / 2, width, 22);

        Theme.FillRounded(g, new Rectangle(rect.X, rect.Y + 8, 5, 5), 3, color);
        Theme.DrawText(g, text, font, color, new Rectangle(rect.X + 11, rect.Y, rect.Width - 11, rect.Height));

        if (state is not null)
        {
            _tip.SetToolTip(this, $"{state.State} · {state.Detail}");
        }
        else
        {
            _tip.SetToolTip(this, "");
        }
    }

    private void DrawLaunch(Graphics g)
    {
        Theme.FillRounded(g, _launchRect, Theme.RadiusControl, _hover ? Theme.Elevated : Theme.WithAlpha(Theme.Elevated, 150));
        Theme.DrawRounded(g, _launchRect, Theme.RadiusControl, _hover ? Theme.Stroke : Theme.StrokeSoft);
        Icons.Draw(g, IconKind.Play, new RectangleF(_launchRect.X + 11, _launchRect.Y + 8, 13, 13), Theme.Text, 1.5f);
        Theme.DrawText(g, "Launch", Theme.Font(9f, FontStyle.Bold), Theme.Text,
            new Rectangle(_launchRect.X + 28, _launchRect.Y, _launchRect.Width - 32, _launchRect.Height),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }

    private void DrawMenu(Graphics g)
    {
        if (_hover || _menu.Visible)
        {
            Theme.FillRounded(g, _menuRect, Theme.RadiusControl, Theme.Elevated);
        }

        Icons.Draw(g, IconKind.More, new RectangleF(_menuRect.X + 4, _menuRect.Y + 4, 20, 20),
            _hover ? Theme.Text : Theme.Muted, 1.4f);
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
}
