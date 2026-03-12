using OpenTyrian.Core;

namespace OpenTyrian.WinForms;

public sealed class MainForm : Form
{
    private readonly GameHost _gameHost;
    private readonly Panel _renderPanel;
    private readonly Label _statusLabel;
    private readonly System.Windows.Forms.Timer _frameTimer;
    private readonly GdiVideoDevice _videoDevice;
    private DateTime _lastFrameUtc;

    public MainForm(GameHost gameHost)
    {
        _gameHost = gameHost;

        Text = "OpenTyrian .NET";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(960, 600);
        MinimumSize = new Size(640, 480);
        KeyPreview = true;

        _renderPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            Margin = Padding.Empty
        };
        _renderPanel.Resize += (_, _) => _renderPanel.Invalidate();
        _renderPanel.Paint += RenderPanelOnPaint;

        _statusLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(32, 32, 32),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0)
        };

        Controls.Add(_renderPanel);
        Controls.Add(_statusLabel);

        _videoDevice = new GdiVideoDevice(_renderPanel, _gameHost.FrameBuffer.Width, _gameHost.FrameBuffer.Height);
        _frameTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _frameTimer.Tick += FrameTimerOnTick;
        _lastFrameUtc = DateTime.UtcNow;

        UpdateStatus();
        Shown += (_, _) => _frameTimer.Start();
        FormClosed += OnFormClosed;
    }

    private void UpdateStatus()
    {
        _statusLabel.Text = _gameHost.StatusText;
    }

    private void FrameTimerOnTick(object? sender, EventArgs e)
    {
        DateTime nowUtc = DateTime.UtcNow;
        double deltaSeconds = (nowUtc - _lastFrameUtc).TotalSeconds;
        _lastFrameUtc = nowUtc;

        _gameHost.Tick(deltaSeconds);
        _gameHost.FrameBuffer.Pixels.CopyTo(_videoDevice.LockFrame());
        _videoDevice.Present();
    }

    private void RenderPanelOnPaint(object? sender, PaintEventArgs e)
    {
        _videoDevice.Present();
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        _frameTimer.Stop();
        _frameTimer.Dispose();
        _videoDevice.Dispose();
    }
}
