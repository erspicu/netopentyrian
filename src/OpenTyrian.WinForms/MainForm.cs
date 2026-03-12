using OpenTyrian.Core;

namespace OpenTyrian.WinForms;

public sealed class MainForm : Form
{
    private readonly GameHost _gameHost;
    private readonly WinFormsInputSource _inputSource;
    private readonly Panel _renderPanel;
    private readonly Label _statusLabel;
    private readonly System.Windows.Forms.Timer _frameTimer;
    private readonly GdiVideoDevice _videoDevice;
    private DateTime _lastFrameUtc;

    public MainForm(GameHost gameHost, WinFormsInputSource inputSource)
    {
        _gameHost = gameHost;
        _inputSource = inputSource;

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
        _renderPanel.MouseMove += OnRenderPanelMouseMove;
        _renderPanel.MouseLeave += OnRenderPanelMouseLeave;
        _renderPanel.MouseDown += OnRenderPanelMouseDown;
        _renderPanel.MouseUp += OnRenderPanelMouseUp;

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
        Shown += OnShown;
        FormClosed += OnFormClosed;
        KeyDown += OnKeyDown;
        KeyPress += OnKeyPress;
        KeyUp += OnKeyUp;
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
        Array.Copy(_gameHost.FrameBuffer.Pixels, _videoDevice.LockFrame(), _gameHost.FrameBuffer.Pixels.Length);
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
        _inputSource.Shutdown();
        _gameHost.Shutdown();
        _videoDevice.Dispose();
    }

    private void OnShown(object? sender, EventArgs e)
    {
        _inputSource.InitializeJoystick(Handle);
        _frameTimer.Start();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _inputSource.SetKeyState(e.KeyCode, isDown: true);
    }

    private void OnKeyPress(object? sender, KeyPressEventArgs e)
    {
        _inputSource.QueueTextInput(e.KeyChar);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        _inputSource.SetKeyState(e.KeyCode, isDown: false);
    }

    private void OnRenderPanelMouseMove(object? sender, MouseEventArgs e)
    {
        UpdatePointerPosition(e.Location);
    }

    private void OnRenderPanelMouseLeave(object? sender, EventArgs e)
    {
        _inputSource.ClearPointer();
    }

    private void OnRenderPanelMouseDown(object? sender, MouseEventArgs e)
    {
        UpdatePointerPosition(e.Location);
        _inputSource.SetPointerButtonState(e.Button, isDown: true);
    }

    private void OnRenderPanelMouseUp(object? sender, MouseEventArgs e)
    {
        UpdatePointerPosition(e.Location);
        _inputSource.SetPointerButtonState(e.Button, isDown: false);
    }

    private void UpdatePointerPosition(Point clientPoint)
    {
        if (_videoDevice.TryMapClientPointToFrame(clientPoint, out Point framePoint))
        {
            _inputSource.SetPointerPosition(framePoint.X, framePoint.Y, present: true);
            return;
        }

        _inputSource.SetPointerPosition(0, 0, present: false);
    }
}
