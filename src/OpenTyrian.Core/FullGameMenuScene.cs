namespace OpenTyrian.Core;

public sealed class FullGameMenuScene : IScene
{
    private readonly EpisodeSessionState _sessionState;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private MenuState? _menuState;
    private EpisodeCommandExecutionResult _lastExecutionResult;

    public FullGameMenuScene(EpisodeSessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        MenuDefinition definition = CreateDefinition(resources.GameplayText, _sessionState);
        EnsureMenuState(definition);
        if (_menuState is null)
        {
            _previousInput = input;
            return null;
        }

        EpisodeCommandExecutionResult autoExecutionResult = EpisodePendingCommandRunner.ExecutePending(_sessionState);
        if (autoExecutionResult.ExecutedCommands > 0)
        {
            _lastExecutionResult = autoExecutionResult;
        }

        if (_lastExecutionResult.ShopRequested && _sessionState.ShopCategories.Count > 0)
        {
            _previousInput = input;
            return new UpgradeMenuScene(_sessionState, returnToFullGameMenu: true);
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
        MenuDefinition definition = CreateDefinition(resources.GameplayText, _sessionState);
        EnsureMenuState(definition);
        TitleScreenRenderer.RenderBackground(surface, resources, timeSeconds);
        TitleScreenRenderer.RenderTitleOverlay(surface, resources.FontRenderer, resources.PaletteCount);

        if (_menuState is null || resources.FontRenderer is null)
        {
            return;
        }

        resources.FontRenderer.DrawText(
            surface,
            160,
            86,
            string.Format(
                "{0}  level:{1}/{2}  cash:{3}  cubes:{4}",
                _sessionState.StartInfo.DisplayName,
                _sessionState.CurrentLevelNumber,
                Math.Max(1, _sessionState.MainLevelEntries.Count),
                _sessionState.Cash,
                _sessionState.CubeEntries.Count),
            FontKind.Tiny,
            FontAlignment.Center,
            14,
            1,
            shadow: true);
        TitleScreenRenderer.RenderMenuOverlay(surface, resources.FontRenderer, definition, _menuState);
        resources.FontRenderer.DrawDark(
            surface,
            160,
            194,
            string.Format(
                "last exec: cmds={0} changed={1} jumped={2} shop={3}",
                _lastExecutionResult.ExecutedCommands,
                _lastExecutionResult.StateChanged,
                _lastExecutionResult.Jumped,
                _lastExecutionResult.ShopRequested),
            FontKind.Tiny,
            FontAlignment.Center,
            black: false);
    }

    private IScene? ExecuteSelectedItem()
    {
        if (_menuState is null)
        {
            return null;
        }

        return _menuState.SelectedItem.Id switch
        {
            "data_cubes" => new DataCubeScene(_sessionState, returnToFullGameMenu: true),
            "ship_specs" => new ShipSpecsScene(_sessionState),
            "upgrade_ship" => new UpgradeMenuScene(_sessionState, returnToFullGameMenu: true),
            "options" => new OptionsScene(_sessionState),
            "next_level" => new LevelSelectScene(_sessionState),
            "session_state" => new EpisodeSessionScene(_sessionState, returnToFullGameMenu: true),
            "quit_episode" => new QuitConfirmationScene(_sessionState),
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

    private static MenuDefinition CreateDefinition(GameplayTextInfo? gameplayText, EpisodeSessionState sessionState)
    {
        IList<string> labels = gameplayText?.FullGameMenu ?? [ "Full Game", "Data Cubes", "Ship Specs", "Upgrade Ship", "Options", "Next Level", "Quit" ];
        string title = labels.Count > 0 ? labels[0] : "Full Game";

        return new MenuDefinition
        {
            Title = title,
            Footer = "Esc returns to episode select  Mouse hover/click enabled",
            Items =
            [
                new MenuItemDefinition
                {
                    Id = "data_cubes",
                    Label = labels.Count > 1 ? labels[1] : "Data Cubes",
                    Description = sessionState.CubeEntries.Count > 0
                        ? string.Format("Read {0} decoded data cube entries.", sessionState.CubeEntries.Count)
                        : "No data cubes decoded yet.",
                },
                new MenuItemDefinition
                {
                    Id = "ship_specs",
                    Label = labels.Count > 2 ? labels[2] : "Ship Specs",
                    Description = string.Format("Inspect {0} and current equipped systems.", ItemNameResolver.GetCompactItemName(ItemCategoryKind.Ship, sessionState.PlayerLoadout.GetEquippedItemId(ItemCategoryKind.Ship), null)),
                },
                new MenuItemDefinition
                {
                    Id = "upgrade_ship",
                    Label = labels.Count > 3 ? labels[3] : "Upgrade Ship",
                    Description = sessionState.ShopCategories.Count > 0
                        ? string.Format("Open {0} shop categories and trade equipment.", sessionState.ShopCategories.Count)
                        : "Open the upgrade shop prototype.",
                },
                new MenuItemDefinition
                {
                    Id = "options",
                    Label = labels.Count > 4 ? labels[4] : "Options",
                    Description = "Open the configuration menu slice.",
                },
                new MenuItemDefinition
                {
                    Id = "next_level",
                    Label = labels.Count > 5 ? labels[5] : "Next Level",
                    Description = string.Format("Choose from {0} parsed main-level sections.", sessionState.MainLevelEntries.Count),
                    IsEnabled = sessionState.MainLevelEntries.Count > 0,
                },
                new MenuItemDefinition
                {
                    Id = "session_state",
                    Label = "Session State",
                    Description = "Open the current debug view for script/session metadata.",
                },
                new MenuItemDefinition
                {
                    Id = "quit_episode",
                    Label = labels.Count > 6 ? labels[6] : "Quit",
                    Description = "Return to episode selection.",
                },
            ],
        };
    }
}
