namespace OpenTyrian.Core;

public sealed class OptionsScene : IScene
{
    private readonly EpisodeSessionState _sessionState;
    private readonly Func<IScene> _returnSceneFactory;
    private readonly bool _limitedMode;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private MenuState? _menuState;

    public OptionsScene(EpisodeSessionState sessionState)
        : this(sessionState, delegate { return new FullGameMenuScene(sessionState); }, limitedMode: false)
    {
    }

    public OptionsScene(EpisodeSessionState sessionState, Func<IScene> returnSceneFactory, bool limitedMode)
    {
        _sessionState = sessionState;
        _returnSceneFactory = returnSceneFactory;
        _limitedMode = limitedMode;
    }

    public IScene? Update(SceneResources resources, OpenTyrian.Platform.InputSnapshot input, double deltaSeconds)
    {
        MenuDefinition definition = CreateDefinition(resources.GameplayText, _limitedMode);
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
            return _returnSceneFactory();
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
            return ExecuteSelectedItem(resources);
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        MenuDefinition definition = CreateDefinition(resources.GameplayText, _limitedMode);
        EnsureMenuState(definition);
        TitleScreenRenderer.RenderPictureBackground(surface, resources, 2, includeOverlays: false);
        if (_menuState is not null)
        {
            if (resources.FontRenderer is not null)
            {
                resources.FontRenderer.DrawText(
                    surface,
                    160,
                    26,
                    _limitedMode ? "Arcade Options" : _sessionState.StartInfo.DisplayName,
                    FontKind.Tiny,
                    FontAlignment.Center,
                    14,
                    1,
                    shadow: true);
            }

            TitleScreenRenderer.RenderMenuOverlay(surface, resources.FontRenderer, definition, _menuState);
        }
    }

    private void EnsureMenuState(MenuDefinition definition)
    {
        if (_menuState is not null)
        {
            return;
        }

        _menuState = new MenuState(definition, selectedIndex: definition.Items.Count > 0 ? definition.Items.Count - 1 : 0);
    }

    private IScene ExecuteSelectedItem(SceneResources resources)
    {
        if (_menuState is null)
        {
            return _returnSceneFactory();
        }

        return _menuState.SelectedItem.Id switch
        {
            "load_game" => new SaveSlotsScene(_sessionState, resources.SaveSlots ?? BuildFallbackCatalog(), SaveBrowserMode.Load, delegate { return new OptionsScene(_sessionState, _returnSceneFactory, _limitedMode); }),
            "save_game" => new SaveSlotsScene(_sessionState, resources.SaveSlots ?? BuildFallbackCatalog(), SaveBrowserMode.Save, delegate { return new OptionsScene(_sessionState, _returnSceneFactory, _limitedMode); }),
            "joystick" => new JoystickSetupScene(_sessionState, _returnSceneFactory, _limitedMode),
            "keyboard" => new KeyboardSetupScene(_sessionState, _returnSceneFactory, _limitedMode),
            _ => _returnSceneFactory(),
        };
    }

    private static MenuDefinition CreateDefinition(GameplayTextInfo? gameplayText, bool limitedMode)
    {
        IList<string> labels = gameplayText?.OptionsMenu ?? [ "Options", "Load Game", "Save Game", string.Empty, string.Empty, "Joystick Setup", "Keyboard Setup", "Done" ];
        string title = labels.Count > 0 ? labels[0] : "Options";

        if (limitedMode)
        {
            return new MenuDefinition
            {
                Title = title,
                Footer = "Esc returns to arcade menu",
                Items =
                [
                    new MenuItemDefinition
                    {
                        Id = "joystick",
                        Label = GetLabel(labels, 5, "Joystick Setup"),
                        Description = "Inspect XInput/DirectInput status and rebind the six core buttons.",
                    },
                    new MenuItemDefinition
                    {
                        Id = "keyboard",
                        Label = GetLabel(labels, 6, "Keyboard Setup"),
                        Description = "Inspect and rebind the six core menu/game buttons.",
                    },
                    new MenuItemDefinition
                    {
                        Id = "done",
                        Label = GetLabel(labels, 7, "Done"),
                        Description = "Return to arcade menu.",
                    },
                ],
            };
        }

        return new MenuDefinition
        {
            Title = title,
            Footer = "Esc returns to full-game menu  Enter/click Done",
            Items =
            [
                new MenuItemDefinition
                {
                    Id = "load_game",
                    Label = GetLabel(labels, 1, "Load Game"),
                    Description = "Browse decoded tyrian.sav slots.",
                },
                new MenuItemDefinition
                {
                    Id = "save_game",
                    Label = GetLabel(labels, 2, "Save Game"),
                    Description = "Browse slots, edit a save name, and write the current session.",
                },
                new MenuItemDefinition
                {
                    Id = "joystick",
                    Label = GetLabel(labels, 5, "Joystick Setup"),
                    Description = "Inspect XInput/DirectInput status and rebind the six core buttons.",
                },
                new MenuItemDefinition
                {
                    Id = "keyboard",
                    Label = GetLabel(labels, 6, "Keyboard Setup"),
                    Description = "Inspect and rebind the six core menu/game buttons.",
                },
                new MenuItemDefinition
                {
                    Id = "done",
                    Label = GetLabel(labels, 7, "Done"),
                    Description = "Return to full-game menu.",
                },
            ],
        };
    }

    private static string GetLabel(IList<string> labels, int index, string fallback)
    {
        if (index < labels.Count && !string.IsNullOrWhiteSpace(labels[index]))
        {
            return labels[index];
        }

        return fallback;
    }

    public static SaveSlotCatalog BuildFallbackCatalog()
    {
        SaveSlotInfo[] slots = new SaveSlotInfo[22];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new SaveSlotInfo
            {
                SlotIndex = i + 1,
                PageIndex = i / 11,
                IsEmpty = true,
                Name = "EMPTY SLOT",
                LevelName = "-----",
                LevelNumber = 0,
                EpisodeNumber = 0,
                CubeCount = 0,
                Cash = 0,
                Cash2 = 0,
            };
        }

        return new SaveSlotCatalog
        {
            SourcePath = string.Empty,
            HasSaveFile = false,
            IsValid = false,
            Slots = slots,
        };
    }
}
