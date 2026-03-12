namespace OpenTyrian.Core;

public sealed class EpisodeSelectScene : IScene
{
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private MenuState? _menuState;
    private IReadOnlyList<EpisodeInfo> _episodes = Array.Empty<EpisodeInfo>();

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        EnsureMenuState(resources);
        if (_menuState is null)
        {
            _previousInput = input;
            return null;
        }

        bool cancelPressed = input.Cancel && !_previousInput.Cancel;
        bool confirmPressed = input.Confirm && !_previousInput.Confirm;
        bool upPressed = input.Up && !_previousInput.Up;
        bool downPressed = input.Down && !_previousInput.Down;

        if (cancelPressed)
        {
            _previousInput = input;
            return new MainMenuScene();
        }

        if (upPressed)
        {
            _menuState.MovePrevious();
        }

        if (downPressed)
        {
            _menuState.MoveNext();
        }

        if (confirmPressed && _menuState.SelectedItem.IsEnabled)
        {
            _previousInput = input;
            EpisodeInfo? selectedEpisode = GetSelectedEpisode();
            if (selectedEpisode is not null)
            {
                EpisodeSessionState sessionState = new(selectedEpisode.StartInfo);
                return new EpisodeSessionScene(sessionState);
            }
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        EnsureMenuState(resources);
        TitleScreenRenderer.RenderBackground(surface, resources, timeSeconds);
        TitleScreenRenderer.RenderTitleOverlay(surface, resources.FontRenderer, resources.PaletteCount);
        if (_menuState is not null)
        {
            TitleScreenRenderer.RenderMenuOverlay(surface, resources.FontRenderer, CreateMenuDefinition(resources.Episodes), _menuState);
        }
    }

    private void EnsureMenuState(SceneResources resources)
    {
        if (_menuState is not null)
        {
            return;
        }

        _episodes = resources.Episodes.Skip(1).ToArray();
        MenuDefinition definition = CreateMenuDefinition(resources.Episodes);
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

    private static MenuDefinition CreateMenuDefinition(IReadOnlyList<EpisodeInfo> episodes)
    {
        List<MenuItemDefinition> items = new(episodes.Count);
        string title = "Select Episode";

        if (episodes.Count > 0 && !string.IsNullOrWhiteSpace(episodes[0].Label))
        {
            title = episodes[0].Label;
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
            Footer = "Esc returns to game mode select",
            Items = items,
        };
    }
}
