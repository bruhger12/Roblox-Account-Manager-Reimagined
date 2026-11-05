using RamReimagined.Core;

namespace RamReimagined.UI;

internal sealed class AccountsPage : PageView
{
    private readonly TextField _search;
    private readonly TextField _place;
    private readonly PillButton _add;
    private readonly PillButton _refresh;
    private readonly PillButton _launch;
    private readonly PillButton _selectAll;
    private readonly Panel _list;
    private readonly Label _count;
    private readonly System.Windows.Forms.Timer _debounce = new() { Interval = 200 };
    private readonly Dictionary<Guid, AccountCard> _cards = new();
    private bool _busy;

    public AccountsPage(AppState app) : base(app)
    {
        _add = new PillButton("Add account", IconKind.Plus) { Kind = ButtonKind.Primary, Height = 38, Width = 148 };
        _add.Click += async (_, _) => await AddAccountAsync();

        _refresh = new PillButton("Validate", IconKind.Refresh) { Kind = ButtonKind.Subtle, Height = 38, Width = 116 };
        _refresh.Click += async (_, _) => await ValidateAllAsync();

        _search = new TextField("Search by name, @handle, tag or ID");
        _search.TextChanged += (_, _) =>
        {
            _debounce.Stop();
            _debounce.Start();
        };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            Rebuild();
        };

        _place = new TextField("Join place ID");
        if (app.Settings.DefaultPlaceId > 0)
        {
            _place.Text = app.Settings.DefaultPlaceId.ToString();
        }

        _launch = new PillButton("Launch selected", IconKind.Play) { Kind = ButtonKind.Primary, Height = 38, Width = 176 };
        _launch.Click += async (_, _) => await LaunchSelectionAsync();

        _selectAll = new PillButton("Select all") { Kind = ButtonKind.Ghost, Height = 38, Width = 104 };
        _selectAll.Click += (_, _) => ToggleSelectAll();

        _count = new Label
        {
            AutoSize = false,
            Font = Theme.Font(8.75f),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Backdrop };
        toolbar.Controls.Add(_search);
        toolbar.Controls.Add(_place);
        toolbar.Controls.Add(_count);
        toolbar.Controls.Add(_selectAll);
        toolbar.Controls.Add(_launch);
        toolbar.Resize += (_, _) => LayoutToolbar(toolbar);

        _list = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Backdrop, AutoScroll = true };
        _list.Resize += (_, _) => LayoutCards();
        _list.Paint += (_, e) => PaintEmptyState(e.Graphics);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Backdrop,
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(_list, 0, 1);
        Controls.Add(root);

        App.AccountsChanged += (_, _) => Rebuild();
        App.PresenceChanged += (_, _) => ApplyPresence();

        Rebuild();
    }

    public override string NavLabel => "Accounts";

    public override IconKind NavIcon => IconKind.Users;

    public override string Heading => "Your accounts";

    public override string Subheading => "Sessions are encrypted with Windows DPAPI and never leave this machine.";

    public override IReadOnlyList<Control> Actions => new Control[] { _add, _refresh };

    public event EventHandler? OpenServersRequested;

    public override void Activated() => Rebuild();

    private void LayoutToolbar(Panel toolbar)
    {
        _search.SetBounds(0, 9, Math.Min(300, Math.Max(180, toolbar.ClientSize.Width / 3)), 38);
        _place.SetBounds(_search.Right + 10, 9, 164, 38);
        _launch.Location = new Point(toolbar.ClientSize.Width - _launch.Width, 9);
        _selectAll.Location = new Point(_launch.Left - 8 - _selectAll.Width, 9);
        _count.SetBounds(_place.Right + 14, 0,
            Math.Max(80, _selectAll.Left - _place.Right - 26), 56);
    }

    private void PaintEmptyState(Graphics g)
    {
        if (_cards.Values.Any(card => card.Visible))
        {
            return;
        }

        Theme.EnableQuality(g);
        var empty = App.Store.Count == 0;
        var title = empty ? "No accounts yet" : "Nothing matches that search";
        var hint = empty
            ? "Add an account with its .ROBLOSECURITY cookie, or paste one from a browser session."
            : "Try another name, handle, tag or user ID.";
        var width = Math.Max(240, _list.ClientSize.Width - 80);
        var centerY = Math.Max(60, _list.ClientSize.Height / 2 - 40);

        var titleFont = Theme.Font(13f, FontStyle.Bold);
        Theme.DrawText(g, title, titleFont, Theme.Muted, new Rectangle(40, centerY, width, 26),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        Theme.DrawText(g, hint, Theme.Font(9f), Theme.Faint, new Rectangle(40, centerY + 30, width, 40),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak);
    }

    private void Rebuild()
    {
        var query = _search.Text.Trim();
        var accounts = App.Store.Accounts
            .Where(a => query.Length == 0
                        || a.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || a.Username.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (a.Tag?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                        || a.UserId.ToString().Contains(query, StringComparison.Ordinal))
            .ToList();

        foreach (var stale in _cards.Keys.Except(App.Store.Accounts.Select(a => a.Id)).ToList())
        {
            _cards[stale].Dispose();
            _cards.Remove(stale);
        }

        foreach (var account in accounts)
        {
            if (!_cards.TryGetValue(account.Id, out var card))
            {
                card = new AccountCard(account);
                Wire(card);
                _cards[account.Id] = card;
                _list.Controls.Add(card);
                _ = LoadAvatarAsync(card);
            }

            card.SetPresence(App.Presence.TryGetValue(account.UserId, out var presence) ? presence : null);
            card.Selected = App.SelectedUserIds.Contains(account.UserId);
            card.Visible = true;
        }

        foreach (var card in _cards.Values)
        {
            if (!accounts.Contains(card.Account))
            {
                card.Visible = false;
            }
        }

        LayoutCards();
        UpdateCounts();
        _list.Invalidate();
    }

    private void LayoutCards()
    {
        var y = 2;
        var width = Math.Max(420, _list.ClientSize.Width - 14);
        foreach (var account in App.Store.Accounts)
        {
            if (!_cards.TryGetValue(account.Id, out var card) || !card.Visible)
            {
                continue;
            }

            card.SetBounds(0, y, width, 84);
            y += 94;
        }

        _list.AutoScrollMinSize = new Size(0, y + 2);
    }

    private void UpdateCounts()
    {
        var visible = _cards.Values.Count(card => card.Visible);
        var total = App.Store.Count;
        var selected = App.SelectedUserIds.Count;
        var shown = visible == total ? $"{total} account{(total == 1 ? "" : "s")}" : $"{visible} of {total} shown";
        _count.Text = $"{shown}  ·  {selected} selected";
    }

    private void Wire(AccountCard card)
    {
        card.SelectedChanged += (_, _) =>
        {
            if (card.Selected)
            {
                App.SelectedUserIds.Add(card.Account.UserId);
            }
            else
            {
                App.SelectedUserIds.Remove(card.Account.UserId);
            }

            UpdateCounts();
        };

        card.LaunchRequested += async (_, _) => await LaunchAsync(new[] { card.Account });
        card.ServersRequested += (_, _) => OpenServersRequested?.Invoke(this, EventArgs.Empty);
        card.CopyCookieRequested += (_, _) => CopyCookie(card.Account);
        card.CopyUserIdRequested += (_, _) =>
        {
            SafeCopy(card.Account.UserId.ToString());
            App.Toast("User ID copied.", ToastKind.Success);
        };
        card.ToggleEnabledRequested += (_, _) =>
        {
            card.Account.Enabled = !card.Account.Enabled;
            App.Store.Save();
            card.RefreshFromAccount();
            App.Toast(card.Account.Enabled
                ? $"{card.Account.Name} will be included in batch launches."
                : $"{card.Account.Name} is muted for batch launches.", ToastKind.Info);
        };
        card.TagRequested += (_, _) => EditTag(card.Account);
        card.RemoveRequested += (_, _) => RemoveAccount(card.Account);
    }

    private void CopyCookie(Account account)
    {
        var cookie = App.Store.CookieFor(account);
        if (string.IsNullOrWhiteSpace(cookie))
        {
            App.Toast("That session is not readable on this Windows account.", ToastKind.Warning);
            return;
        }

        SafeCopy(cookie);
        App.Toast("Cookie copied to the clipboard.", ToastKind.Success);
    }

    private static void SafeCopy(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (Exception)
        {
            // The clipboard can be locked by another process; nothing to recover here.
        }
    }

    private async Task LoadAvatarAsync(AccountCard card)
    {
        var image = await App.Avatars.GetAsync(card.Account.UserId);
        if (!IsDisposed && !card.IsDisposed)
        {
            card.SetAvatar(image);
        }
    }

    private void ApplyPresence()
    {
        foreach (var card in _cards.Values)
        {
            card.SetPresence(App.Presence.TryGetValue(card.Account.UserId, out var presence) ? presence : null);
        }
    }

    private void ToggleSelectAll()
    {
        var visible = _cards.Values.Where(card => card.Visible).ToList();
        var allSelected = visible.Count > 0 && visible.All(card => card.Selected);
        foreach (var card in visible)
        {
            card.Selected = !allSelected;
        }

        UpdateCounts();
    }

    private long PlaceId()
    {
        var digits = new string(_place.Text.Where(char.IsDigit).ToArray());
        return long.TryParse(digits, out var id) && id > 0 ? id : App.Settings.DefaultPlaceId;
    }

    private async Task LaunchSelectionAsync()
    {
        var targets = App.SelectedAccounts().ToList();
        if (targets.Count == 0)
        {
            App.Toast("Select at least one account first.", ToastKind.Warning);
            return;
        }

        await LaunchAsync(targets);
    }

    private async Task LaunchAsync(IReadOnlyList<Account> accounts)
    {
        if (_busy)
        {
            return;
        }

        var placeId = PlaceId();
        if (placeId <= 0)
        {
            App.Toast("Set a join place ID first.", ToastKind.Warning);
            _place.FocusInput();
            return;
        }

        var targets = accounts.Where(a => a.Enabled).ToList();
        if (targets.Count == 0)
        {
            App.Toast("Those accounts are muted.", ToastKind.Warning);
            return;
        }

        App.Settings.DefaultPlaceId = placeId;
        App.Settings.Save();

        _busy = true;
        _launch.Busy = true;
        try
        {
            var summary = await App.LaunchAsync(targets, placeId, null);
            if (summary.Failed == 0)
            {
                App.Toast($"Launched {summary.Launched} client(s) into {placeId}.", ToastKind.Success);
            }
            else
            {
                App.Toast($"{summary.Launched} launched, {summary.Failed} failed. {summary.Errors.FirstOrDefault()}",
                    ToastKind.Warning);
            }

            await App.RefreshPresenceAsync();
        }
        catch (Exception ex)
        {
            App.Toast(ex.Message, ToastKind.Error);
        }
        finally
        {
            _busy = false;
            _launch.Busy = false;
        }
    }

    private async Task AddAccountAsync()
    {
        var raw = Dialogs.Ask(this,
            "Add an account",
            "Paste the .ROBLOSECURITY value: DevTools → Application → Cookies → roblox.com. The cookie is validated and then encrypted locally.",
            "Paste cookie", secret: true, okText: "Check & add");

        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var cookie = AccountStore.NormalizeCookie(raw);
        if (cookie is null)
        {
            App.Toast("That does not look like a Roblox cookie.", ToastKind.Error);
            return;
        }

        try
        {
            App.Toast("Checking the session with Roblox…", ToastKind.Info);
            var user = await App.Api.GetAuthenticatedAsync(cookie);
            if (user is null)
            {
                App.Toast("Roblox rejected that cookie.", ToastKind.Error);
                return;
            }

            var existed = App.Store.FindByUserId(user.Id) is not null;
            App.Store.AddOrUpdate(user, cookie);
            App.SelectedUserIds.Add(user.Id);
            App.Avatars.Forget(user.Id);
            App.NotifyAccountsChanged();
            await App.RefreshPresenceAsync();
            App.Toast(existed ? $"Session refreshed for {user.DisplayName}." : $"Added {user.DisplayName}.", ToastKind.Success);
        }
        catch (Exception ex)
        {
            App.Toast(ex.Message, ToastKind.Error);
        }
    }

    private async Task ValidateAllAsync()
    {
        if (App.Store.Count == 0)
        {
            App.Toast("There are no accounts to check.", ToastKind.Warning);
            return;
        }

        _refresh.Busy = true;
        try
        {
            var expired = new List<Account>();
            foreach (var account in App.Store.Accounts.ToList())
            {
                var cookie = App.Store.CookieFor(account);
                if (cookie is null)
                {
                    expired.Add(account);
                    continue;
                }

                var user = await App.Api.GetAuthenticatedAsync(cookie);
                if (user is null)
                {
                    expired.Add(account);
                    continue;
                }

                if (user.Name != account.Username || user.DisplayName != account.DisplayName)
                {
                    account.Username = user.Name;
                    account.DisplayName = user.DisplayName;
                    App.Avatars.Forget(account.UserId);
                }
            }

            App.Store.Save();
            App.NotifyAccountsChanged();
            await App.RefreshPresenceAsync();

            if (expired.Count == 0)
            {
                App.Toast("Every session is still valid.", ToastKind.Success);
            }
            else
            {
                App.Toast($"{expired.Count} expired: {string.Join(", ", expired.Select(a => a.Name))}", ToastKind.Warning);
            }
        }
        catch (Exception ex)
        {
            App.Toast(ex.Message, ToastKind.Error);
        }
        finally
        {
            _refresh.Busy = false;
        }
    }

    private void EditTag(Account account)
    {
        var tag = Dialogs.Ask(this, "Account tag", "A short label used for searching, e.g. main, alt, group.",
            account.Tag ?? "", okText: "Save tag");
        if (tag is null)
        {
            return;
        }

        account.Tag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim();
        App.Store.Save();
        App.NotifyAccountsChanged();
        App.Toast("Tag saved.", ToastKind.Success);
    }

    private void RemoveAccount(Account account)
    {
        if (!Dialogs.Confirm(this, "Remove account",
                $"Remove {account.Name}? The stored session is deleted from this machine.", "Remove"))
        {
            return;
        }

        App.SelectedUserIds.Remove(account.UserId);
        App.Store.Remove(account);
        App.NotifyAccountsChanged();
        App.Toast($"{account.Name} removed.", ToastKind.Info);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _debounce.Dispose();
        }

        base.Dispose(disposing);
    }
}
