namespace OpenTyrian.Core;

public sealed class MainMenuScene : IScene, IScenePresentation
{
    private const int HeaderCenterX = 160;
    private const int HeaderY = 20;
    private const int ItemStartY = 54;
    private const int ItemRowHeight = 24;
    private const int ItemHeight = 13;

    private static readonly string[] Items =
    {
        "1 Player Full Game",
        "1 Player Arcade",
        "2 Player Arcade",
        "Network Game",
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

        int? hoveredIndex = input.PointerPresent
            ? HitTestRow(resources.FontRenderer, input.PointerX, input.PointerY)
            : null;
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
            if (IsNetworkItem(_selectedIndex))
            {
                SceneAudio.PlayCancel(resources);
            }
            else
            {
                SceneAudio.PlayConfirm(resources);
                _previousInput = input;
                return ExecuteSelectedItem();
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

        resources.FontRenderer.DrawShadowText(
            surface,
            HeaderCenterX,
            HeaderY,
            "Players",
            FontKind.Normal,
            FontAlignment.Center,
            15,
            -3,
            black: false,
            shadowDistance: 2);

        for (int i = 0; i < Items.Length; i++)
        {
            bool selected = i == _selectedIndex;
            bool disabled = IsNetworkItem(i);
            int value = -4 + (selected ? 2 : 0) + (disabled ? -4 : 0);

            resources.FontRenderer.DrawShadowText(
                surface,
                HeaderCenterX,
                ItemStartY + (i * ItemRowHeight),
                Items[i],
                FontKind.Normal,
                FontAlignment.Center,
                15,
                value,
                black: false,
                shadowDistance: 2);
        }
    }

    private IScene ExecuteSelectedItem()
    {
        switch (_selectedIndex)
        {
            case 0:
                return new EpisodeSelectScene(GameStartMode.FullGame);
            case 1:
                return new EpisodeSelectScene(GameStartMode.ArcadeOnePlayer);
            case 2:
                return new EpisodeSelectScene(GameStartMode.ArcadeTwoPlayer);
            default:
                return new TitleMenuScene();
        }
    }

    private static int? HitTestRow(TyrianFontRenderer? fontRenderer, int x, int y)
    {
        for (int i = 0; i < Items.Length; i++)
        {
            int textWidth = fontRenderer is not null ? fontRenderer.MeasureText(Items[i], FontKind.Normal) : 180;
            int left = HeaderCenterX - (textWidth / 2);
            int right = left + textWidth;
            int top = ItemStartY + (i * ItemRowHeight);
            int bottom = top + ItemHeight;

            if (x >= left && x < right && y >= top && y < bottom)
            {
                return i;
            }
        }

        return null;
    }

    private static bool IsNetworkItem(int index)
    {
        return index == 3;
    }
}
