using OpenTyrian.Platform;

namespace OpenTyrian.Core;

public sealed class KeyboardSetupScene : IScene
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

    public KeyboardSetupScene(EpisodeSessionState sessionState)
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

        IInputConfigurator? configurator = resources.InputConfigurator;
        if (configurator is null)
        {
            _previousInput = input;
            return cancelPressed ? new OptionsScene(_sessionState) : null;
        }

        int rowCount = ConfigurableButtons.Length + 2;
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
                if (_selectedIndex < ConfigurableButtons.Length)
                {
                    configurator.BeginRebind(ConfigurableButtons[_selectedIndex]);
                    return null;
                }

                if (_selectedIndex == ConfigurableButtons.Length)
                {
                    configurator.ResetToDefaults();
                    return null;
                }

                return new OptionsScene(_sessionState);
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

        IInputConfigurator? configurator = resources.InputConfigurator;
        resources.FontRenderer.DrawShadowText(surface, 160, 78, "Keyboard Setup", FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);

        if (configurator is null)
        {
            resources.FontRenderer.DrawText(surface, 160, 110, "Keyboard configurator is not available on this platform.", FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
            resources.FontRenderer.DrawDark(surface, 160, 188, "Esc returns to options", FontKind.Tiny, FontAlignment.Center, black: false);
            return;
        }

        for (int i = 0; i < ConfigurableButtons.Length; i++)
        {
            string label = string.Format("{0,-8} {1}", GetButtonLabel(ConfigurableButtons[i]), configurator.GetBindingLabel(ConfigurableButtons[i]));
            DrawRow(surface, resources.FontRenderer, i, label);
        }

        DrawRow(surface, resources.FontRenderer, ConfigurableButtons.Length, "Reset Defaults");
        DrawRow(surface, resources.FontRenderer, ConfigurableButtons.Length + 1, "Done");

        string footer = configurator.PendingBinding is InputButton pending
            ? string.Format("Press a key for {0}  Esc cancels", GetButtonLabel(pending))
            : "Up/Down choose  Enter/click rebind  Esc back";
        resources.FontRenderer.DrawDark(surface, 160, 194, footer, FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private void DrawRow(IndexedFrameBuffer surface, TyrianFontRenderer fontRenderer, int rowIndex, string label)
    {
        bool selected = rowIndex == _selectedIndex;
        int y = 100 + (rowIndex * 12);
        if (selected)
        {
            fontRenderer.DrawBlendText(surface, 88, y, $"> {label}", FontKind.Tiny, FontAlignment.Left, 15, 4);
        }
        else
        {
            fontRenderer.DrawText(surface, 88, y, label, FontKind.Tiny, FontAlignment.Left, 13, 0, shadow: true);
        }
    }

    private static int? HitTestRow(int x, int y, int rowCount)
    {
        if (x < 84 || x > 236)
        {
            return null;
        }

        for (int i = 0; i < rowCount; i++)
        {
            int top = 98 + (i * 12);
            int bottom = top + 10;
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
