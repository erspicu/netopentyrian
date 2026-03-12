namespace OpenTyrian.Core;

public sealed class QuitConfirmationScene : IScene
{
    private readonly EpisodeSessionState _sessionState;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private MenuState? _menuState;

    public QuitConfirmationScene(EpisodeSessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        MenuDefinition definition = CreateDefinition();
        EnsureMenuState(definition);
        if (_menuState is null)
        {
            _previousInput = input;
            return null;
        }

        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool leftPressed = input.Left && !_previousInput.Left;
        bool rightPressed = input.Right && !_previousInput.Right;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;

        int? hoveredIndex = input.PointerPresent
            ? TitleScreenRenderer.HitTestMenuItem(definition, input.PointerX, input.PointerY)
            : null;

        if (hoveredIndex is int pointerIndex)
        {
            if (_menuState.SelectedIndex != pointerIndex)
            {
                SceneAudio.PlayCursor(resources);
            }

            _menuState.SetSelectedIndex(pointerIndex);
        }

        if (cancelPressed)
        {
            SceneAudio.PlayCancel(resources);
            _previousInput = input;
            return new FullGameMenuScene(_sessionState);
        }

        if (leftPressed)
        {
            SceneAudio.PlayCursor(resources);
            _menuState.MovePrevious();
        }

        if (rightPressed)
        {
            SceneAudio.PlayCursor(resources);
            _menuState.MoveNext();
        }

        if (confirmPressed || (pointerConfirmPressed && hoveredIndex is not null))
        {
            if (_menuState.SelectedItem.Id == "yes")
            {
                SceneAudio.PlayConfirm(resources);
            }
            else
            {
                SceneAudio.PlayCancel(resources);
            }

            _previousInput = input;
            return _menuState.SelectedItem.Id == "yes"
                ? new EpisodeSelectScene(_sessionState.StartMode)
                : new FullGameMenuScene(_sessionState);
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        MenuDefinition definition = CreateDefinition();
        EnsureMenuState(definition);
        TitleScreenRenderer.RenderPictureBackground(surface, resources, 2, includeOverlays: false);
        if (_menuState is null || resources.FontRenderer is null)
        {
            return;
        }

        resources.FontRenderer.DrawText(surface, 160, 24, _sessionState.StartInfo.DisplayName, FontKind.Tiny, FontAlignment.Center, 14, 1, shadow: true);
        TitleScreenRenderer.RenderMenuOverlay(surface, resources.FontRenderer, definition, _menuState);
        resources.FontRenderer.DrawDark(surface, 160, 194, "Left/Right or mouse choose  Enter/click confirm  Esc cancel", FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private void EnsureMenuState(MenuDefinition definition)
    {
        if (_menuState is not null)
        {
            return;
        }

        _menuState = new MenuState(definition, selectedIndex: 1);
    }

    private static MenuDefinition CreateDefinition()
    {
        return new MenuDefinition
        {
            Title = "Quit Episode?",
            Footer = "Return to episode selection?",
            Items =
            [
                new MenuItemDefinition
                {
                    Id = "yes",
                    Label = "Yes",
                    Description = "Leave the current episode session.",
                },
                new MenuItemDefinition
                {
                    Id = "no",
                    Label = "No",
                    Description = "Go back to full-game menu.",
                },
            ],
        };
    }
}
