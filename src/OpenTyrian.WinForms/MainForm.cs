using OpenTyrian.Core;

namespace OpenTyrian.WinForms;

public sealed class MainForm : Form
{
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;

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
        string statusText = _gameHost.StatusText ?? string.Empty;
        bool visible = statusText.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
            statusText.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0;

        _statusLabel.Visible = visible;
        _statusLabel.Text = visible ? statusText : string.Empty;
    }

    private void FrameTimerOnTick(object? sender, EventArgs e)
    {
        DateTime nowUtc = DateTime.UtcNow;
        double deltaSeconds = (nowUtc - _lastFrameUtc).TotalSeconds;
        _lastFrameUtc = nowUtc;

        _gameHost.Tick(deltaSeconds);
        Array.Copy(_gameHost.FrameBuffer.Pixels, _videoDevice.LockFrame(), _gameHost.FrameBuffer.Pixels.Length);
        _videoDevice.Present();
        UpdateStatus();

        if (_gameHost.ExitRequested)
        {
            Close();
        }
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
        Activate();
        _renderPanel.Focus();
        _frameTimer.Start();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmKeyDown || m.Msg == WmSysKeyDown)
        {
            Keys key = (Keys)(int)m.WParam & Keys.KeyCode;
            if (ShouldCaptureKey(key))
            {
                _inputSource.SetKeyState(key, isDown: true);
            }
        }
        else if (m.Msg == WmKeyUp || m.Msg == WmSysKeyUp)
        {
            Keys key = (Keys)(int)m.WParam & Keys.KeyCode;
            if (ShouldCaptureKey(key))
            {
                _inputSource.SetKeyState(key, isDown: false);
            }
        }

        base.WndProc(ref m);
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

    private static bool ShouldCaptureKey(Keys key)
    {
        switch (key)
        {
            case Keys.Up:
            case Keys.Down:
            case Keys.Left:
            case Keys.Right:
            case Keys.Return:
            case Keys.Space:
            case Keys.Escape:
            case Keys.Back:
                return true;
            default:
                return false;
        }
    }
}
