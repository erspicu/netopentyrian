namespace OpenTyrian.Core;

public sealed class TitleSetupScene : IScene, IScenePresentation
{
    private static readonly string[] Items =
    {
        "Jukebox",
        "Done",
    };

    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _selectedIndex;

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
            return _selectedIndex == 0 ? new JukeboxScene() : new TitleMenuScene();
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

        resources.FontRenderer.DrawShadowText(surface, 160, 20, "Setup", FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);
        resources.FontRenderer.DrawText(surface, 160, 190, "Title-level setup branch", FontKind.Tiny, FontAlignment.Center, 13, 0, shadow: true);

        for (int i = 0; i < Items.Length; i++)
        {
            int y = 60 + (i * 24);
            if (i == _selectedIndex)
            {
                resources.FontRenderer.DrawBlendText(surface, 160, y, Items[i], FontKind.Normal, FontAlignment.Center, 15, -1);
            }
            else
            {
                resources.FontRenderer.DrawText(surface, 160, y, Items[i], FontKind.Normal, FontAlignment.Center, 15, -4, shadow: true);
            }
        }
    }

    private static int? HitTestRow(int x, int y)
    {
        if (x < 90 || x > 230)
        {
            return null;
        }

        for (int i = 0; i < Items.Length; i++)
        {
            int top = 54 + (i * 24);
            int bottom = top + 14;
            if (y >= top && y <= bottom)
            {
                return i;
            }
        }

        return null;
    }
}
