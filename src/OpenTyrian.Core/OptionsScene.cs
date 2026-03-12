namespace OpenTyrian.Core;

public sealed class OptionsScene : IScene
{
    private readonly EpisodeSessionState _sessionState;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private MenuState? _menuState;

    public OptionsScene(EpisodeSessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        MenuDefinition definition = CreateDefinition(resources.GameplayText);
        EnsureMenuState(definition);
        if (_menuState is null)
        {
            _previousInput = input;
            return null;
        }

        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;

        int? hoveredIndex = input.PointerPresent
            ? TitleScreenRenderer.HitTestMenuItem(definition, input.PointerX, input.PointerY)
            : null;

        if (hoveredIndex is int pointerIndex)
        {
            _menuState.SetSelectedIndex(pointerIndex);
        }

        if (cancelPressed)
        {
            _previousInput = input;
            return new FullGameMenuScene(_sessionState);
        }

        if (upPressed)
        {
            _menuState.MovePrevious();
        }

        if (downPressed)
        {
            _menuState.MoveNext();
        }

        if ((confirmPressed || (pointerConfirmPressed && hoveredIndex is not null)) && _menuState.SelectedItem.IsEnabled)
        {
            _previousInput = input;
            return new FullGameMenuScene(_sessionState);
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        MenuDefinition definition = CreateDefinition(resources.GameplayText);
        EnsureMenuState(definition);
        TitleScreenRenderer.RenderBackground(surface, resources, timeSeconds);
        TitleScreenRenderer.RenderTitleOverlay(surface, resources.FontRenderer, resources.PaletteCount);
        if (_menuState is not null)
        {
            TitleScreenRenderer.RenderMenuOverlay(surface, resources.FontRenderer, definition, _menuState);
        }
    }

    private void EnsureMenuState(MenuDefinition definition)
    {
        if (_menuState is not null)
        {
            return;
        }

        _menuState = new MenuState(definition, selectedIndex: definition.Items.Count > 0 ? definition.Items.Count - 1 : 0);
    }

    private static MenuDefinition CreateDefinition(GameplayTextInfo? gameplayText)
    {
        IList<string> labels = gameplayText?.OptionsMenu ?? [ "Options", "Load Game", "Save Game", string.Empty, string.Empty, "Joystick Setup", "Keyboard Setup", "Done" ];
        string title = labels.Count > 0 ? labels[0] : "Options";

        return new MenuDefinition
        {
            Title = title,
            Footer = "Esc returns to full-game menu  Enter/click Done",
            Items =
            [
                new MenuItemDefinition
                {
                    Id = "load_game",
                    Label = GetLabel(labels, 1, "Load Game"),
                    Description = "Load/save flow is not wired yet.",
                    IsEnabled = false,
                },
                new MenuItemDefinition
                {
                    Id = "save_game",
                    Label = GetLabel(labels, 2, "Save Game"),
                    Description = "Load/save flow is not wired yet.",
                    IsEnabled = false,
                },
                new MenuItemDefinition
                {
                    Id = "joystick",
                    Label = GetLabel(labels, 5, "Joystick Setup"),
                    Description = "Joystick configuration is planned for the input layer stage.",
                    IsEnabled = false,
                },
                new MenuItemDefinition
                {
                    Id = "keyboard",
                    Label = GetLabel(labels, 6, "Keyboard Setup"),
                    Description = "Keyboard remapping is not wired yet.",
                    IsEnabled = false,
                },
                new MenuItemDefinition
                {
                    Id = "done",
                    Label = GetLabel(labels, 7, "Done"),
                    Description = "Return to full-game menu.",
                },
            ],
        };
    }

    private static string GetLabel(IList<string> labels, int index, string fallback)
    {
        if (index < labels.Count && !string.IsNullOrWhiteSpace(labels[index]))
        {
            return labels[index];
        }

        return fallback;
    }
}
