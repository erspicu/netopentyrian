namespace OpenTyrian.Core;

public sealed class MainMenuScene : IScene
{
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private MenuState? _menuState;

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
            return new TitleScene();
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
            return ExecuteSelectedItem();
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

    private IScene? ExecuteSelectedItem()
    {
        if (_menuState is null)
        {
            return null;
        }

        return _menuState.SelectedItem.Id switch
        {
            "one_player_full_game" => new EpisodeSelectScene(GameStartMode.FullGame),
            "one_player_arcade" => new EpisodeSelectScene(GameStartMode.ArcadeOnePlayer),
            "two_player_arcade" => new EpisodeSelectScene(GameStartMode.ArcadeTwoPlayer),
            _ => null,
        };
    }

    private void EnsureMenuState(MenuDefinition definition)
    {
        if (_menuState is not null)
        {
            return;
        }

        _menuState = new MenuState(definition);
    }

    private static MenuDefinition CreateDefinition(GameplayTextInfo? gameplayText)
    {
        IList<string> names = gameplayText?.GameplayNames ??
            ["Select Game Mode", "1 Player Full Game", "1 Player Arcade", "2 Player Arcade", "Network Game"];
        IList<string> help = gameplayText?.MainMenuHelp ??
            ["Main campaign route.", "Single-player arcade mode.", "Local two-player arcade mode.", "Network mode is not wired yet."];

        string title = names.Count > 0 ? names[0] : "Select Game Mode";

        return new MenuDefinition
        {
            Title = title,
            Footer = "Esc returns to title  Mouse hover/click enabled",
            Items =
            [
                new MenuItemDefinition
                {
                    Id = "one_player_full_game",
                    Label = names.Count > 1 ? names[1] : "1 Player Full Game",
                    Description = help.Count > 0 ? help[0] : "Main campaign route.",
                },
                new MenuItemDefinition
                {
                    Id = "one_player_arcade",
                    Label = names.Count > 2 ? names[2] : "1 Player Arcade",
                    Description = help.Count > 1 ? help[1] : "Single-player arcade mode.",
                },
                new MenuItemDefinition
                {
                    Id = "two_player_arcade",
                    Label = names.Count > 3 ? names[3] : "2 Player Arcade",
                    Description = help.Count > 2 ? help[2] : "Local two-player arcade mode.",
                },
                new MenuItemDefinition
                {
                    Id = "network_game",
                    Label = names.Count > 4 ? names[4] : "Network Game",
                    Description = help.Count > 3 ? help[3] : "Network mode is not wired yet.",
                    IsEnabled = false,
                },
            ],
        };
    }
}
