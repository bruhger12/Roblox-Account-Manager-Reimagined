using System.Diagnostics;
using RamReimagined.Core;

namespace RamReimagined.UI;

internal sealed class AboutPage : PageView
{
    public AboutPage(AppState app) : base(app)
    {
        var card = new RoundedCard { Dock = DockStyle.Top, Height = 340, AccentEdge = true };

        var mark = new Panel { Bounds = new Rectangle(24, 24, 44, 44), BackColor = Theme.Card };
        mark.Paint += (_, e) =>
        {
            Theme.EnableQuality(e.Graphics);
            Theme.FillRounded(e.Graphics, new Rectangle(0, 0, 43, 43), Theme.RadiusCard, Theme.Text);
            Theme.DrawText(e.Graphics, "R", Theme.Font(16f, FontStyle.Bold), Theme.Backdrop,
                new Rectangle(0, 0, 44, 44),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
        card.Controls.Add(mark);

        var name = new Label
        {
            Text = "Roblox Account Manager Reimagined",
            Font = Theme.FontDisplay(15f),
            ForeColor = Theme.Text,
            BackColor = Theme.Card,
            AutoSize = false,
            Bounds = new Rectangle(84, 24, 520, 26)
        };
        card.Controls.Add(name);

        card.Controls.Add(new MutedLabel("A modern rebuild of ic3w0lf22's account manager · v1.1.0 · GPL-3.0", 9f)
        {
            Bounds = new Rectangle(84, 52, 620, 20)
        });

        var body = new Label
        {
            Text = string.Join(Environment.NewLine, new[]
            {
                "Sessions are encrypted with Windows DPAPI for the signed-in Windows user only. Cookies never leave this machine,",
                "and no password is ever asked for: the sign-in browser adds the account from the session it creates, then clears",
                "its cookies. Browser windows opened as an account load roblox.com with that account's stored session and nothing else.",
                string.Empty,
                "Launching trades a cookie for a fresh authentication ticket and starts RobloxPlayerBeta.exe with that ticket.",
                "Multi-instance support only creates the two session-wide kernel objects Roblox itself uses; nothing is injected into",
                "the client. Roblox has described multi-instancing as possibly malicious behaviour, so use it at your own risk.",
                string.Empty,
                "This is an independent GPL-3.0 fork. It is not affiliated with Roblox Corporation."
            }),
            Font = Theme.Font(9f),
            ForeColor = Theme.Muted,
            BackColor = Theme.Card,
            AutoSize = false,
            Bounds = new Rectangle(24, 96, 700, 150)
        };
        card.Controls.Add(body);

        var repo = new PillButton("Open upstream fork", IconKind.External) { Kind = ButtonKind.Subtle, Width = 174, Height = 36 };
        repo.Click += (_, _) => Open("https://github.com/bruhger12/Roblox-Account-Manager-Reimagined");
        card.Controls.Add(repo);

        var upstream = new PillButton("Original project", IconKind.External) { Kind = ButtonKind.Ghost, Width = 146, Height = 36 };
        upstream.Click += (_, _) => Open("https://github.com/ic3w0lf22/Roblox-Account-Manager");
        card.Controls.Add(upstream);

        var folder = new PillButton("Data folder", IconKind.Folder) { Kind = ButtonKind.Ghost, Width = 120, Height = 36 };
        folder.Click += (_, _) =>
        {
            try
            {
                Directory.CreateDirectory(AppPaths.DataDirectory);
                Process.Start(new ProcessStartInfo(AppPaths.DataDirectory) { UseShellExecute = true });
            }
            catch (Exception)
            {
                App.Toast("Could not open the data folder.", ToastKind.Warning);
            }
        };
        card.Controls.Add(folder);

        card.Resize += (_, _) =>
        {
            body.Width = Math.Max(320, card.Width - 48);
            repo.Location = new Point(24, card.Height - 58);
            upstream.Location = new Point(repo.Right + 10, repo.Top);
            folder.Location = new Point(upstream.Right + 10, repo.Top);
        };

        var status = new RoundedCard { Dock = DockStyle.Top, Height = 128 };
        status.Controls.Add(new SectionTitle("Environment") { Location = new Point(20, 18) });
        status.Controls.Add(new MutedLabel($"Roblox client: {app.Launcher.PlayerPath ?? "not found"}", 9f)
        {
            Location = new Point(20, 48),
            Size = new Size(760, 20)
        });
        status.Controls.Add(new MutedLabel($"Account file: {AppPaths.AccountsFile}", 9f)
        {
            Location = new Point(20, 72),
            Size = new Size(760, 20)
        });
        status.Controls.Add(new MutedLabel($"Multi-instance: {(app.Launcher.MultiInstance.Active ? "active" : "released")}  ·  Accounts: {app.Store.Count}", 9f)
        {
            Location = new Point(20, 96),
            Size = new Size(760, 20)
        });

        var stack = new StackPanel { Dock = DockStyle.Fill, Gap = 12 };
        stack.Controls.Add(card);
        stack.Controls.Add(status);
        Controls.Add(stack);
    }

    public override string NavLabel => "About";

    public override IconKind NavIcon => IconKind.Info;

    public override string Heading => "About";

    public override string Subheading => "What this fork changes, and the boundaries it keeps.";

    private static void Open(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // A missing default browser should not crash the app.
        }
    }
}
