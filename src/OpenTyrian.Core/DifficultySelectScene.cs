namespace OpenTyrian.Core;

public sealed class DifficultySelectScene : IScene, IScenePresentation
{
    private static readonly string[] DifficultyNames =
    {
        "Easy",
        "Normal",
        "Hard",
        "Impossible",
        "Suicide",
        "Zinglon",
    };

    private readonly EpisodeInfo _episode;
    private readonly GameStartMode _startMode;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _selectedIndex = 1;

    public DifficultySelectScene(EpisodeInfo episode, GameStartMode startMode)
    {
        _episode = episode;
        _startMode = startMode;
    }

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
            return new EpisodeSelectScene(_startMode);
        }

        if (upPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedIndex = _selectedIndex == 0 ? DifficultyNames.Length - 1 : _selectedIndex - 1;
        }

        if (downPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedIndex = (_selectedIndex + 1) % DifficultyNames.Length;
        }

        if (confirmPressed || (pointerConfirmPressed && hoveredIndex.HasValue))
        {
            SceneAudio.PlayConfirm(resources);
            _previousInput = input;

            EpisodeSessionState? sessionState = TitleFlowHelper.CreateSession(_episode, _startMode, _selectedIndex + 1);
            return sessionState is null ? null : new FullGameMenuScene(sessionState);
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

        resources.FontRenderer.DrawShadowText(surface, 160, 20, "Select Difficulty", FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);
        for (int i = 0; i < DifficultyNames.Length; i++)
        {
            int y = 54 + (i * 24);
            bool selected = i == _selectedIndex;
            if (selected)
            {
                resources.FontRenderer.DrawBlendText(surface, 160, y, DifficultyNames[i], FontKind.Normal, FontAlignment.Center, 15, -1);
            }
            else
            {
                resources.FontRenderer.DrawText(surface, 160, y, DifficultyNames[i], FontKind.Normal, FontAlignment.Center, 15, -4, shadow: true);
            }
        }

        resources.FontRenderer.DrawDark(surface, 160, 190, _episode.Label, FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private static int? HitTestRow(int x, int y)
    {
        if (x < 78 || x > 242)
        {
            return null;
        }

        for (int i = 0; i < DifficultyNames.Length; i++)
        {
            int top = 48 + (i * 24);
            int bottom = top + 14;
            if (y >= top && y <= bottom)
            {
                return i;
            }
        }

        return null;
    }
}
