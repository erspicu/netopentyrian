namespace OpenTyrian.Core;

public sealed class EpisodeSelectScene : IScene
{
    private readonly GameStartMode _startMode;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private MenuState? _menuState;
    private IList<EpisodeInfo> _episodes = new EpisodeInfo[0];

    public EpisodeSelectScene(GameStartMode startMode = GameStartMode.FullGame)
    {
        _startMode = startMode;
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        MenuDefinition definition = CreateMenuDefinition(resources.Episodes, _startMode);
        EnsureMenuState(resources, definition);
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
            return new MainMenuScene();
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
            EpisodeInfo? selectedEpisode = GetSelectedEpisode();
            if (selectedEpisode is not null)
            {
                EpisodeSessionState sessionState = new(selectedEpisode.StartInfo, _startMode);
                return new FullGameMenuScene(sessionState);
            }
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        MenuDefinition definition = CreateMenuDefinition(resources.Episodes, _startMode);
        EnsureMenuState(resources, definition);
        TitleScreenRenderer.RenderBackground(surface, resources, timeSeconds);
        TitleScreenRenderer.RenderTitleOverlay(surface, resources.FontRenderer, resources.PaletteCount);
        if (_menuState is not null)
        {
            TitleScreenRenderer.RenderMenuOverlay(surface, resources.FontRenderer, definition, _menuState);
        }
    }

    private void EnsureMenuState(SceneResources resources, MenuDefinition definition)
    {
        if (_menuState is not null)
        {
            return;
        }

        _episodes = resources.Episodes.Skip(1).ToArray();
        _menuState = new MenuState(definition);
    }

    private EpisodeInfo? GetSelectedEpisode()
    {
        if (_menuState is null)
        {
            return null;
        }

        int index = _menuState.SelectedIndex;
        return index >= 0 && index < _episodes.Count ? _episodes[index] : null;
    }

    private static MenuDefinition CreateMenuDefinition(IList<EpisodeInfo> episodes, GameStartMode startMode)
    {
        List<MenuItemDefinition> items = new(episodes.Count);
        string title = "Select Episode";

        if (episodes.Count > 0 && !string.IsNullOrWhiteSpace(episodes[0].Label))
        {
            title = episodes[0].Label;
        }

        if (startMode != GameStartMode.FullGame)
        {
            title = string.Format("{0} ({1})", title, startMode.GetDisplayName());
        }

        foreach (EpisodeInfo episode in episodes.Skip(1))
        {
            items.Add(new MenuItemDefinition
            {
                Id = $"episode_{episode.EpisodeNumber}",
                Label = episode.Label,
                Description = episode.Description,
                IsEnabled = episode.IsAvailable,
            });
        }

        return new MenuDefinition
        {
            Title = title,
            Footer = string.Format("Mode: {0}  Esc returns  Mouse hover/click enabled", startMode.GetDisplayName()),
            Items = items,
        };
    }
}
