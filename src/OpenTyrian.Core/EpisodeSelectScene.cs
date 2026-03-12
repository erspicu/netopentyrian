namespace OpenTyrian.Core;

public sealed class EpisodeSelectScene : IScene, IScenePresentation
{
    private readonly GameStartMode _startMode;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private IList<EpisodeInfo> _episodes = new EpisodeInfo[0];
    private int _selectedIndex;

    public EpisodeSelectScene(GameStartMode startMode = GameStartMode.FullGame)
    {
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
        EnsureEpisodes(resources);
        if (_episodes.Count == 0)
        {
            _previousInput = input;
            return null;
        }

        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;

        int? hoveredIndex = input.PointerPresent ? HitTestRow(resources.FontRenderer, input.PointerX, input.PointerY) : null;

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
            SceneAudio.PlayCancel(resources);
            _previousInput = input;
            return new MainMenuScene();
        }

        if (upPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedIndex = _selectedIndex == 0 ? _episodes.Count - 1 : _selectedIndex - 1;
        }

        if (downPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedIndex = (_selectedIndex + 1) % _episodes.Count;
        }

        if (confirmPressed || (pointerConfirmPressed && hoveredIndex.HasValue))
        {
            EpisodeInfo? selectedEpisode = GetSelectedEpisode();
            if (selectedEpisode is not null && selectedEpisode.IsAvailable)
            {
                SceneAudio.PlayConfirm(resources);
                _previousInput = input;
                return new DifficultySelectScene(selectedEpisode, _startMode);
            }

            SceneAudio.PlayCancel(resources);
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        EnsureEpisodes(resources);
        TitleScreenRenderer.RenderPictureBackground(surface, resources, 2, includeOverlays: false);
        if (resources.FontRenderer is null)
        {
            return;
        }

        resources.FontRenderer.DrawShadowText(surface, 160, 20, "Select Episode", FontKind.Normal, FontAlignment.Center, 15, -3, black: false, shadowDistance: 2);
        for (int i = 0; i < _episodes.Count; i++)
        {
            EpisodeInfo episode = _episodes[i];
            int value = -4 + (i == _selectedIndex ? 2 : 0) + (episode.IsAvailable ? 0 : -4);
            resources.FontRenderer.DrawShadowText(
                surface,
                20,
                50 + (i * 30),
                episode.Label,
                FontKind.Small,
                FontAlignment.Left,
                15,
                value,
                black: false,
                shadowDistance: 2);
        }
    }

    private void EnsureEpisodes(SceneResources resources)
    {
        if (_episodes.Count > 0)
        {
            return;
        }

        _episodes = resources.Episodes.Skip(1).ToArray();
        _selectedIndex = 0;
    }

    private EpisodeInfo? GetSelectedEpisode()
    {
        return _selectedIndex >= 0 && _selectedIndex < _episodes.Count ? _episodes[_selectedIndex] : null;
    }

    private int? HitTestRow(TyrianFontRenderer? fontRenderer, int x, int y)
    {
        for (int i = 0; i < _episodes.Count; i++)
        {
            int textWidth = fontRenderer is not null ? fontRenderer.MeasureText(_episodes[i].Label, FontKind.Small) : 200;
            int top = 50 + (i * 30);
            int bottom = top + 13;
            if (x >= 20 && x < 20 + textWidth && y >= top && y < bottom)
            {
                return i;
            }
        }

        return null;
    }
}
