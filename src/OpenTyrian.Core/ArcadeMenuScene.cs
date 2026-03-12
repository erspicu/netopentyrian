namespace OpenTyrian.Core;

public sealed class ArcadeMenuScene : IScene, IScenePresentation
{
    private readonly EpisodeSessionState _sessionState;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private MenuState? _menuState;

    public ArcadeMenuScene(EpisodeSessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public int? BackgroundPictureNumber
    {
        get { return 2; }
    }

    public SceneMusicKind? MusicOverride
    {
        get { return SceneMusicKind.Menu; }
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        MenuDefinition definition = CreateDefinition(_sessionState);
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
            return new EpisodeSelectScene(_sessionState.StartMode);
        }

        if (upPressed)
        {
            SceneAudio.PlayCursor(resources);
            _menuState.MovePrevious();
        }

        if (downPressed)
        {
            SceneAudio.PlayCursor(resources);
            _menuState.MoveNext();
        }

        if ((confirmPressed || (pointerConfirmPressed && hoveredIndex is not null)) && _menuState.SelectedItem.IsEnabled)
        {
            SceneAudio.PlayConfirm(resources);
            _previousInput = input;
            return ExecuteSelectedItem();
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        MenuDefinition definition = CreateDefinition(_sessionState);
        EnsureMenuState(definition);
        TitleScreenRenderer.RenderPictureBackground(surface, resources, 2, includeOverlays: false);
        if (_menuState is null || resources.FontRenderer is null)
        {
            return;
        }

        resources.FontRenderer.DrawText(
            surface,
            160,
            24,
            _sessionState.StartInfo.DisplayName,
            FontKind.Tiny,
            FontAlignment.Center,
            14,
            1,
            shadow: true);
        resources.FontRenderer.DrawText(
            surface,
            160,
            34,
            string.Format("episode {0}  difficulty {1}  players {2}", _sessionState.InitialEpisodeNumber, _sessionState.Difficulty, _sessionState.PlayerCount),
            FontKind.Tiny,
            FontAlignment.Center,
            13,
            0,
            shadow: true);
        TitleScreenRenderer.RenderMenuOverlay(surface, resources.FontRenderer, definition, _menuState);
    }

    private IScene ExecuteSelectedItem()
    {
        if (_menuState is null)
        {
            return new EpisodeSelectScene(_sessionState.StartMode);
        }

        return _menuState.SelectedItem.Id switch
        {
            "play_arcade" => new GameplayScene(_sessionState),
            "options" => new OptionsScene(_sessionState, delegate { return new ArcadeMenuScene(_sessionState); }, limitedMode: true),
            "quit" => new EpisodeSelectScene(_sessionState.StartMode),
            _ => new EpisodeSelectScene(_sessionState.StartMode),
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

    private static MenuDefinition CreateDefinition(EpisodeSessionState sessionState)
    {
        string title = sessionState.StartMode == GameStartMode.ArcadeTwoPlayer
            ? "2 Player Arcade"
            : "1 Player Arcade";

        return new MenuDefinition
        {
            Title = title,
            Footer = "Esc returns to episode select",
            Items =
            [
                new MenuItemDefinition
                {
                    Id = "play_arcade",
                    Label = "Play Arcade",
                    Description = "Launch the current arcade mission.",
                },
                new MenuItemDefinition
                {
                    Id = "options",
                    Label = "Options",
                    Description = "Open limited arcade setup and input options.",
                },
                new MenuItemDefinition
                {
                    Id = "quit",
                    Label = "Quit",
                    Description = "Return to episode selection.",
                },
            ],
        };
    }
}
