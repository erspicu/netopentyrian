namespace OpenTyrian.Core;

public sealed class TitleSetupScene : IScene, IScenePresentation
{
    private const int HeaderX = 160;
    private const int HeaderY = 4;
    private const int ItemX = 45;
    private const int ItemStartY = 37;
    private const int ItemRowHeight = 21;
    private const int ItemHeight = 13;

    private static readonly string[] Items =
    {
        "Graphics...",
        "Sound...",
        "Jukebox",
        "Done",
    };

    private static readonly string[] FooterText =
    {
        "Change the graphics settings.",
        "Change the music or sound settings.",
        "Listen to the Tyrian soundtrack.",
        "Return to the main menu.",
    };

    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _selectedIndex;

    public int? BackgroundPictureNumber
    {
        get { return 2; }
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

        int? hoveredIndex = input.PointerPresent ? HitTestRow(input.PointerX, input.PointerY) : null;
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
            _previousInput = input;
            return new TitleMenuScene();
        }

        if (upPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedIndex = _selectedIndex == 0 ? Items.Length - 1 : _selectedIndex - 1;
        }

        if (downPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedIndex = (_selectedIndex + 1) % Items.Length;
        }

        if (confirmPressed || (pointerConfirmPressed && hoveredIndex.HasValue))
        {
            SceneAudio.PlayConfirm(resources);
            _previousInput = input;
            switch (_selectedIndex)
            {
                case 0:
                    return new TitleSetupDetailScene(
                        "Graphics",
                        new[]
                        {
                            "320x200 indexed rendering is active.",
                            "Window sizing stays under WinForms host control.",
                            "Press Enter or Esc to return.",
                        });

                case 1:
                    return new TitleSetupDetailScene(
                        "Sound",
                        new[]
                        {
                            "Scene music now uses music.mus + LDS + OPL.",
                            "Menu cues are still mixed by GameHost.",
                            "Press Enter or Esc to return.",
                        });

                case 2:
                    return new JukeboxScene();

                default:
                    return new TitleMenuScene();
            }
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        TitleScreenRenderer.RenderPictureBackground(surface, resources, 2, includeOverlays: false);
        if (resources.FontRenderer is null)
        {
            return;
        }

        resources.FontRenderer.DrawShadowText(surface, HeaderX, HeaderY, "Setup", FontKind.Normal, FontAlignment.Center, 15, -3, black: false, shadowDistance: 2);

        for (int i = 0; i < Items.Length; i++)
        {
            int y = ItemStartY + (i * ItemRowHeight);
            if (i == _selectedIndex)
            {
                resources.FontRenderer.DrawShadowText(surface, ItemX, y, Items[i], FontKind.Small, FontAlignment.Left, 15, -1, black: false, shadowDistance: 2);
            }
            else
            {
                resources.FontRenderer.DrawShadowText(surface, ItemX, y, Items[i], FontKind.Small, FontAlignment.Left, 15, -3, black: false, shadowDistance: 2);
            }
        }

        resources.FontRenderer.DrawText(surface, ItemX, 190, FooterText[_selectedIndex], FontKind.Tiny, FontAlignment.Left, 15, 4, shadow: true);
    }

    private static int? HitTestRow(int x, int y)
    {
        if (x < ItemX || x > 180)
        {
            return null;
        }

        for (int i = 0; i < Items.Length; i++)
        {
            int top = ItemStartY + (i * ItemRowHeight);
            int bottom = top + ItemHeight;
            if (y >= top && y <= bottom)
            {
                return i;
            }
        }

        return null;
    }
}
