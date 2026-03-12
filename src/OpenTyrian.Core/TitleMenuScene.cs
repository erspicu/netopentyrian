namespace OpenTyrian.Core;

public sealed class TitleMenuScene : IScene, IScenePresentation
{
    private const int MenuCenterX = 160;
    private const int MenuStartY = 104;
    private const int MenuRowHeight = 13;

    private static readonly string[] DefaultItems =
    {
        "Start New Game",
        "Load Game",
        "High Scores",
        "Instructions",
        "Setup",
        "Demo",
        "Quit",
    };

    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _selectedIndex;

    public int? BackgroundPictureNumber
    {
        get { return 4; }
    }

    public SceneMusicKind? MusicOverride
    {
        get { return SceneMusicKind.Title; }
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;

        int? hoveredIndex = input.PointerPresent ? HitTestMenuItem(resources.FontRenderer, input.PointerX, input.PointerY) : null;
        if (hoveredIndex.HasValue)
        {
            if (_selectedIndex != hoveredIndex.Value)
            {
                SceneAudio.PlayCursor(resources);
            }

            _selectedIndex = hoveredIndex.Value;
        }

        if (cancelPressed)
        {
            SceneAudio.PlayCancel(resources);
            resources.ExitGame?.Invoke();
            _previousInput = input;
            return null;
        }

        if (upPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedIndex = _selectedIndex == 0 ? DefaultItems.Length - 1 : _selectedIndex - 1;
        }

        if (downPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedIndex = (_selectedIndex + 1) % DefaultItems.Length;
        }

        if (confirmPressed || (pointerConfirmPressed && hoveredIndex.HasValue))
        {
            SceneAudio.PlayConfirm(resources);
            _previousInput = input;
            return ExecuteSelectedItem(resources);
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        TitleScreenRenderer.RenderPictureBackground(surface, resources, 4, includeOverlays: false);
        TitleScreenRenderer.RenderTyrianLogo(surface, resources.MainShapeTables, 11, 4);
        if (resources.FontRenderer is null)
        {
            return;
        }

        for (int i = 0; i < DefaultItems.Length; i++)
        {
            int y = MenuStartY + (i * MenuRowHeight);
            DrawMenuText(surface, resources.FontRenderer, MenuCenterX, y, DefaultItems[i], -3);
        }

        resources.FontRenderer.DrawText(
            surface,
            MenuCenterX,
            MenuStartY + (_selectedIndex * MenuRowHeight),
            DefaultItems[_selectedIndex],
            FontKind.Small,
            FontAlignment.Center,
            15,
            -1,
            shadow: false);
    }

    private IScene? ExecuteSelectedItem(SceneResources resources)
    {
        switch (_selectedIndex)
        {
            case 0:
                return new MainMenuScene();

            case 1:
                {
                    EpisodeSessionState? transientSession = TitleFlowHelper.CreateFirstAvailableSession(resources.Episodes, GameStartMode.FullGame, 2);
                    if (transientSession is null)
                    {
                        return null;
                    }

                    return new SaveSlotsScene(
                        transientSession,
                        resources.SaveSlots ?? OptionsScene.BuildFallbackCatalog(),
                        SaveBrowserMode.Load,
                        delegate { return new TitleMenuScene(); });
                }

            case 2:
                return new HighScoresScene();

            case 3:
                return new InstructionsScene();

            case 4:
                return new TitleSetupScene();

            case 5:
                {
                    EpisodeSessionState? demoSession = TitleFlowHelper.CreateFirstAvailableSession(resources.Episodes, GameStartMode.ArcadeOnePlayer, 2);
                    return demoSession is null ? null : new GameplayScene(demoSession);
                }

            case 6:
                resources.ExitGame?.Invoke();
                return null;

            default:
                return null;
        }
    }

    private static int? HitTestMenuItem(TyrianFontRenderer? fontRenderer, int x, int y)
    {
        for (int i = 0; i < DefaultItems.Length; i++)
        {
            int textWidth = fontRenderer is not null ? fontRenderer.MeasureText(DefaultItems[i], FontKind.Small) : 160;
            int left = MenuCenterX - (textWidth / 2);
            int right = left + textWidth;
            int top = MenuStartY + (i * MenuRowHeight);
            int bottom = top + MenuRowHeight;
            if (x < left || x > right)
            {
                continue;
            }

            if (y >= top && y < bottom)
            {
                return i;
            }
        }

        return null;
    }

    private static void DrawMenuText(IndexedFrameBuffer surface, TyrianFontRenderer fontRenderer, int x, int y, string text, int value)
    {
        fontRenderer.DrawText(surface, x - 1, y - 1, text, FontKind.Small, FontAlignment.Center, 15, -10, shadow: false);
        fontRenderer.DrawText(surface, x + 1, y + 1, text, FontKind.Small, FontAlignment.Center, 15, -10, shadow: false);
        fontRenderer.DrawText(surface, x + 1, y - 1, text, FontKind.Small, FontAlignment.Center, 15, -10, shadow: false);
        fontRenderer.DrawText(surface, x - 1, y + 1, text, FontKind.Small, FontAlignment.Center, 15, -10, shadow: false);
        fontRenderer.DrawText(surface, x, y, text, FontKind.Small, FontAlignment.Center, 15, value, shadow: false);
    }
}
