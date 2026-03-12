using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public sealed class JoystickSetupScene : IScene
{
    private static readonly InputButton[] ConfigurableButtons =
    {
        InputButton.Up,
        InputButton.Down,
        InputButton.Left,
        InputButton.Right,
        InputButton.Confirm,
        InputButton.Cancel,
    };

    private readonly EpisodeSessionState _sessionState;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _selectedIndex;

    public JoystickSetupScene(EpisodeSessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;

        IJoystickConfigurator? configurator = resources.JoystickConfigurator;
        if (configurator is null || !configurator.IsSupported)
        {
            _previousInput = input;
            return cancelPressed ? new OptionsScene(_sessionState) : null;
        }

        int rowCount = ConfigurableButtons.Length + 4;
        int? hoveredIndex = input.PointerPresent
            ? HitTestRow(input.PointerX, input.PointerY, rowCount)
            : null;
        if (hoveredIndex is int pointerIndex)
        {
            if (_selectedIndex != pointerIndex)
            {
                SceneAudio.PlayCursor(resources);
            }

            _selectedIndex = pointerIndex;
        }

        if (cancelPressed)
        {
            if (configurator.PendingBinding is not null)
            {
                SceneAudio.PlayCancel(resources);
                configurator.CancelRebind();
                _previousInput = input;
                return null;
            }

            SceneAudio.PlayCancel(resources);
            _previousInput = input;
            return new OptionsScene(_sessionState);
        }

        if (configurator.PendingBinding is null)
        {
            if (upPressed)
            {
                SceneAudio.PlayCursor(resources);
                _selectedIndex = _selectedIndex == 0 ? rowCount - 1 : _selectedIndex - 1;
            }

            if (downPressed)
            {
                SceneAudio.PlayCursor(resources);
                _selectedIndex = (_selectedIndex + 1) % rowCount;
            }

            if (confirmPressed || (pointerConfirmPressed && hoveredIndex is not null))
            {
                SceneAudio.PlayConfirm(resources);
                _previousInput = input;
                return ExecuteSelectedRow(configurator);
            }
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        TitleScreenRenderer.RenderBackground(surface, resources, timeSeconds);
        TitleScreenRenderer.RenderTitleOverlay(surface, resources.FontRenderer, resources.PaletteCount);

        if (resources.FontRenderer is null)
        {
            return;
        }

        IJoystickConfigurator? configurator = resources.JoystickConfigurator;
        resources.FontRenderer.DrawShadowText(surface, 160, 72, "Joystick Setup", FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);

        if (configurator is null || !configurator.IsSupported)
        {
            resources.FontRenderer.DrawText(surface, 160, 110, "Joystick configurator is not available on this platform.", FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
            resources.FontRenderer.DrawDark(surface, 160, 188, "Esc returns to options", FontKind.Tiny, FontAlignment.Center, black: false);
            return;
        }

        resources.FontRenderer.DrawText(surface, 160, 88, string.Format("Backend: {0}", configurator.BackendName), FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
        resources.FontRenderer.DrawText(surface, 160, 96, string.Format("Device: {0}", configurator.DeviceSummary), FontKind.Tiny, FontAlignment.Center, 14, 0, shadow: true);

        DrawRow(surface, resources.FontRenderer, 0, string.Format("Joystick Input  {0}", configurator.IsEnabled ? "ON" : "OFF"));

        for (int i = 0; i < ConfigurableButtons.Length; i++)
        {
            string label = string.Format("{0,-8} {1}", GetButtonLabel(ConfigurableButtons[i]), configurator.GetBindingLabel(ConfigurableButtons[i]));
            DrawRow(surface, resources.FontRenderer, i + 1, label);
        }

        DrawRow(surface, resources.FontRenderer, ConfigurableButtons.Length + 1, "Refresh Devices");
        DrawRow(surface, resources.FontRenderer, ConfigurableButtons.Length + 2, "Reset Defaults");
        DrawRow(surface, resources.FontRenderer, ConfigurableButtons.Length + 3, "Done");

        string footer = configurator.PendingBinding is InputButton pending
            ? string.Format("Move stick/pad or press a button for {0}  Esc cancels", GetButtonLabel(pending))
            : "Up/Down choose  Enter/click adjust  Esc back";
        resources.FontRenderer.DrawDark(surface, 160, 194, footer, FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private IScene? ExecuteSelectedRow(IJoystickConfigurator configurator)
    {
        if (_selectedIndex == 0)
        {
            configurator.SetEnabled(!configurator.IsEnabled);
            return null;
        }

        if (_selectedIndex <= ConfigurableButtons.Length)
        {
            if (!configurator.HasConnectedDevice)
            {
                return null;
            }

            configurator.BeginRebind(ConfigurableButtons[_selectedIndex - 1]);
            return null;
        }

        if (_selectedIndex == ConfigurableButtons.Length + 1)
        {
            configurator.RefreshStatus();
            return null;
        }

        if (_selectedIndex == ConfigurableButtons.Length + 2)
        {
            configurator.ResetToDefaults();
            return null;
        }

        return new OptionsScene(_sessionState);
    }

    private void DrawRow(IndexedFrameBuffer surface, TyrianFontRenderer fontRenderer, int rowIndex, string label)
    {
        bool selected = rowIndex == _selectedIndex;
        int y = 108 + (rowIndex * 10);
        if (selected)
        {
            fontRenderer.DrawBlendText(surface, 78, y, string.Format("> {0}", label), FontKind.Tiny, FontAlignment.Left, 15, 4);
        }
        else
        {
            fontRenderer.DrawText(surface, 78, y, label, FontKind.Tiny, FontAlignment.Left, 13, 0, shadow: true);
        }
    }

    private static int? HitTestRow(int x, int y, int rowCount)
    {
        if (x < 74 || x > 246)
        {
            return null;
        }

        for (int i = 0; i < rowCount; i++)
        {
            int top = 106 + (i * 10);
            int bottom = top + 9;
            if (y >= top && y <= bottom)
            {
                return i;
            }
        }

        return null;
    }

    private static string GetButtonLabel(InputButton button)
    {
        return button switch
        {
            InputButton.Up => "Up",
            InputButton.Down => "Down",
            InputButton.Left => "Left",
            InputButton.Right => "Right",
            InputButton.Confirm => "Fire",
            InputButton.Cancel => "Back",
            _ => button.ToString(),
        };
    }
}
