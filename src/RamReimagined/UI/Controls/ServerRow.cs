using RamReimagined.Core;

namespace RamReimagined.UI;

/// <summary>One row in the server browser: job id, population, network stats and a join action.</summary>
internal sealed class ServerRow : Control
{
    private bool _hover;
    private Rectangle _joinRect;

    public ServerRow(PublicServer server)
    {
        Server = server;
        Height = 58;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Backdrop;
    }

    public PublicServer Server { get; }

    public event EventHandler? JoinRequested;

    private void ComputeRects()
    {
        _joinRect = new Rectangle(Width - 18 - 84, (Height - 32) / 2, 84, 32);
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
        Cursor = _joinRect.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
        base.OnMouseMove(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        ComputeRects();
        if (_joinRect.Contains(e.Location))
        {
            JoinRequested?.Invoke(this, EventArgs.Empty);
        }

        base.OnMouseClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        ComputeRects();

        var bounds = new Rectangle(0, 1, Width - 1, Height - 3);
        Theme.FillRounded(g, bounds, 12, _hover ? Theme.CardHover : Theme.Card);
        Theme.DrawRounded(g, bounds, 12, Theme.StrokeSoft);

        var font = Theme.Font(9f);
        Theme.DrawText(g, Server.Id, font, Theme.Text, new Rectangle(18, 0, 210, Height),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        // Population bar
        var barBounds = new Rectangle(236, (Height - 6) / 2 + 8, 150, 5);
        Theme.FillRounded(g, barBounds, 3, Theme.WithAlpha(Theme.Stroke, 200));
        var fill = Math.Max(3, (int)(barBounds.Width * Server.Fill));
        Theme.FillRounded(g, new Rectangle(barBounds.X, barBounds.Y, fill, barBounds.Height), 3,
            Server.Fill > 0.85 ? Theme.Warning : Theme.Success);
        Theme.DrawText(g, $"{Server.Playing}/{Server.MaxPlayers}", font, Theme.Muted, new Rectangle(236, 10, 150, 20));

        // Network
        var pingColor = Server.Ping <= 0 ? Theme.Faint : Server.Ping < 80 ? Theme.Success : Server.Ping < 160 ? Theme.Warning : Theme.Danger;
        Theme.DrawText(g, Server.Ping <= 0 ? "—" : $"{Server.Ping} ms", font, pingColor, new Rectangle(400, 0, 80, Height));
        Theme.DrawText(g, Server.Fps <= 0 ? "—" : $"{Server.Fps:0} fps", font, Theme.Muted, new Rectangle(482, 0, 80, Height));

        Theme.FillRounded(g, _joinRect, 9, _hover ? Theme.Accent : Theme.AccentAlpha(190));
        var label = Theme.Font(8.75f, FontStyle.Bold);
        Theme.DrawText(g, "Join", label, Color.White, _joinRect,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

/// <summary>Column captions that track the server row layout.</summary>
internal sealed class ServerTableHeader : Control
{
    public ServerTableHeader()
    {
        Height = 30;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Backdrop;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        var font = Theme.Font(8.25f, FontStyle.Bold);
        Theme.DrawText(g, "JOB ID", font, Theme.Faint, new Rectangle(18, 0, 200, Height));
        Theme.DrawText(g, "PLAYERS", font, Theme.Faint, new Rectangle(236, 0, 150, Height));
        Theme.DrawText(g, "PING", font, Theme.Faint, new Rectangle(400, 0, 70, Height));
        Theme.DrawText(g, "FPS", font, Theme.Faint, new Rectangle(482, 0, 70, Height));
    }
}
