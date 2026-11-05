using System.Runtime.InteropServices;

namespace RamReimagined.UI;

internal static class WindowChrome
{
    public const int WmNcLButtonDown = 0xA1;
    public const int HtCaption = 0x2;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public static void Drag(Form form)
    {
        if (form.WindowState == FormWindowState.Maximized)
        {
            return;
        }

        ReleaseCapture();
        SendMessage(form.Handle, WmNcLButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
    }
}

internal enum ToastKind
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>Floating status pill that confirms every action in the shell.</summary>
internal sealed class StatusToast : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 4200 };
    private string _message = "";
    private Color _accent = Theme.Accent;
    private IconKind _icon = IconKind.Info;

    public StatusToast()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Width = 360;
        Height = 46;
        Visible = false;
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Visible = false;
        };
    }

    public void ShowMessage(string message, ToastKind kind)
    {
        _message = message;
        (_accent, _icon) = kind switch
        {
            ToastKind.Success => (Theme.Success, IconKind.Check),
            ToastKind.Warning => (Theme.Warning, IconKind.Info),
            ToastKind.Error => (Theme.Danger, IconKind.Close),
            _ => (Theme.Accent, IconKind.Info)
        };

        var width = Math.Min(560, TextRenderer.MeasureText(message, Font).Width + 84);
        Width = Math.Max(240, width);
        Visible = true;
        BringToFront();
        Invalidate();
        _timer.Stop();
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.EnableQuality(g);
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        Theme.FillRounded(g, bounds, 12, Theme.Elevated);
        Theme.DrawRounded(g, bounds, 12, Theme.WithAlpha(_accent, 110));

        var circle = new Rectangle(12, (Height - 22) / 2, 22, 22);
        Theme.FillRounded(g, circle, 11, Theme.WithAlpha(_accent, 40));
        Icons.Draw(g, _icon, circle, _accent, 1.7f);

        Theme.DrawText(g, _message, Theme.Font(9.25f), Theme.Text,
            new Rectangle(44, 0, Width - 58, Height),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>Dark modal sheet used for prompts and confirmations.</summary>
internal sealed class SheetForm : Form
{
    private readonly Label _titleLabel;
    private readonly Label _messageLabel;
    private readonly Panel _bodyHost;
    private readonly PillButton _ok;
    private readonly PillButton _cancel;

    public SheetForm(string title, string message, string okText, int bodyHeight, bool danger)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = Theme.Panel;
        DoubleBuffered = true;
        KeyPreview = true;
        ClientSize = new Size(470, 196 + bodyHeight);
        Padding = new Padding(1);

        _titleLabel = new Label
        {
            Text = title,
            Font = Theme.Font(13f, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Color.Transparent,
            AutoSize = false,
            Bounds = new Rectangle(24, 20, 360, 28)
        };

        _messageLabel = new Label
        {
            Text = message,
            Font = Theme.Font(9.25f),
            ForeColor = Theme.Muted,
            BackColor = Color.Transparent,
            AutoSize = false,
            Bounds = new Rectangle(24, 52, 422, 42)
        };

        _bodyHost = new Panel
        {
            BackColor = Theme.Panel,
            Bounds = new Rectangle(24, 100, 422, bodyHeight)
        };

        _ok = new PillButton(okText) { Kind = danger ? ButtonKind.Danger : ButtonKind.Primary, Width = 130, Height = 38 };
        _ok.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        _cancel = new PillButton("Cancel") { Kind = ButtonKind.Ghost, Width = 96, Height = 38 };
        _cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var close = new WindowButton(IconKind.Close, close: true) { Bounds = new Rectangle(ClientSize.Width - 56, 12, 40, 32) };
        close.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        _ok.Location = new Point(ClientSize.Width - 24 - _ok.Width, ClientSize.Height - 58);
        _cancel.Location = new Point(_ok.Left - 8 - _cancel.Width, _ok.Top);

        Controls.Add(_titleLabel);
        Controls.Add(_messageLabel);
        Controls.Add(_bodyHost);
        Controls.Add(_cancel);
        Controls.Add(_ok);
        Controls.Add(close);

        MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                WindowChrome.Drag(this);
            }
        };

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };
    }

    public void SetBody(Control control, int height)
    {
        control.SetBounds(0, 0, _bodyHost.Width, height);
        _bodyHost.Controls.Add(control);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Theme.EnableQuality(e.Graphics);
        Theme.DrawRounded(e.Graphics, new Rectangle(0, 0, Width - 1, Height - 1), 16, Theme.Stroke);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = Theme.RoundedRect(new Rectangle(0, 0, Width, Height), 16);
        Region = new Region(path);
    }
}

internal static class Dialogs
{
    public static string? Ask(IWin32Window? owner, string title, string message, string placeholder = "",
        bool secret = false, string okText = "Continue", int bodyHeight = 42)
    {
        using var form = new SheetForm(title, message, okText, bodyHeight, danger: false);
        var field = new TextField(placeholder) { UseSystemPasswordChar = secret, Height = 40 };
        form.SetBody(field, 40);
        field.FocusInput();
        return form.ShowDialog(owner) == DialogResult.OK ? field.Text.Trim() : null;
    }

    public static bool Confirm(IWin32Window? owner, string title, string message, string okText = "Confirm", bool danger = true)
    {
        using var form = new SheetForm(title, message, okText, 0, danger);
        return form.ShowDialog(owner) == DialogResult.OK;
    }
}
