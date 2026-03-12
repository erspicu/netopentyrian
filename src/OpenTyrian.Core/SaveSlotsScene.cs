namespace OpenTyrian.Core;

public sealed class SaveSlotsScene : IScene, IScenePresentation
{
    private const int MaxSaveNameLength = 14;
    private const int HeaderCenterX = 160;
    private const int HeaderY = 5;
    private const int RowStartY = 30;
    private const int RowHeight = 13;
    private const int RowHitHeight = 8;
    private const int NameColumnX = 10;
    private const int LastLevelColumnX = 120;
    private const int EpisodeColumnX = 250;
    private const int FooterY = 190;
    private const int NoticeY = 168;
    private const int EditPromptY = 170;
    private const int EditValueY = 182;

    private readonly EpisodeSessionState _sessionState;
    private readonly SaveSlotCatalog _catalog;
    private readonly SaveBrowserMode _mode;
    private readonly Func<IScene>? _returnSceneFactory;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _pageIndex;
    private int _selectedIndex;
    private bool _isEditingSaveName;
    private string _pendingSaveName;

    public SaveSlotsScene(EpisodeSessionState sessionState, SaveSlotCatalog catalog, SaveBrowserMode mode, Func<IScene>? returnSceneFactory = null)
    {
        _sessionState = sessionState;
        _catalog = catalog;
        _mode = mode;
        _returnSceneFactory = returnSceneFactory;
        _pageIndex = 0;
        _selectedIndex = 0;
        _pendingSaveName = string.Empty;
    }

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
        bool leftPressed = input.Left && !_previousInput.Left;
        bool rightPressed = input.Right && !_previousInput.Right;
        bool pointerConfirmPressed = input.PointerConfirm && !_previousInput.PointerConfirm;

        int pageSlotCount = GetPageSlotCount(_pageIndex);
        if (_isEditingSaveName)
        {
            IScene? nextScene = UpdateSaveNameEditor(resources, cancelPressed, confirmPressed);
            _previousInput = input;
            return nextScene;
        }

        int? hoveredIndex = input.PointerPresent
            ? HitTestRow(input.PointerX, input.PointerY, pageSlotCount)
            : null;
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
            return ReturnToCaller();
        }

        if (leftPressed && _pageIndex > 0)
        {
            SceneAudio.PlayCursor(resources);
            _pageIndex--;
            _selectedIndex = Math.Min(_selectedIndex, GetPageSlotCount(_pageIndex) - 1);
        }

        if (rightPressed && _pageIndex < 1)
        {
            SceneAudio.PlayCursor(resources);
            _pageIndex++;
            _selectedIndex = Math.Min(_selectedIndex, GetPageSlotCount(_pageIndex) - 1);
        }

        if (upPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedIndex = _selectedIndex == 0 ? pageSlotCount - 1 : _selectedIndex - 1;
        }

        if (downPressed)
        {
            SceneAudio.PlayCursor(resources);
            _selectedIndex = (_selectedIndex + 1) % pageSlotCount;
        }

        if (confirmPressed || (pointerConfirmPressed && hoveredIndex is not null))
        {
            SceneAudio.PlayConfirm(resources);
            _previousInput = input;
            return _mode == SaveBrowserMode.Save
                ? BeginSaveEdit(resources, pageSlotCount)
                : ExecuteSelectedSlot(resources, pageSlotCount);
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

        string title = _mode == SaveBrowserMode.Load ? "One Player Saved Games" : "Save Game";
        resources.FontRenderer.DrawShadowText(surface, HeaderCenterX, HeaderY, title, FontKind.Normal, FontAlignment.Center, 15, -3, black: false, shadowDistance: 2);

        IList<SaveSlotInfo> pageSlots = GetPageSlots(_pageIndex);
        int rowCount = GetPageSlotCount(_pageIndex);
        for (int i = 0; i < rowCount; i++)
        {
            bool isSelected = i == _selectedIndex;
            int y = RowStartY + (i * RowHeight);

            if (i == pageSlots.Count)
            {
                RenderExitRow(surface, resources.FontRenderer, y, isSelected);
            }
            else
            {
                RenderSlotRow(surface, resources.FontRenderer, pageSlots[i], y, isSelected);
            }
        }

        if (_catalog.HasSaveFile && !_catalog.IsValid)
        {
            resources.FontRenderer.DrawText(
                surface,
                HeaderCenterX,
                NoticeY,
                "tyrian.sav invalid; showing empty slots",
                FontKind.Tiny,
                FontAlignment.Center,
                14,
                0,
                shadow: true);
        }

        SaveSlotInfo? selectedSlot = GetSelectedSlot(pageSlots);
        if (_isEditingSaveName)
        {
            bool cursorVisible = ((int)(timeSeconds * 2.0) & 1) == 0;
            string editValue = _pendingSaveName.Length > 0 ? _pendingSaveName : string.Empty;
            if (cursorVisible && editValue.Length < MaxSaveNameLength)
            {
                editValue += "_";
            }

            resources.FontRenderer.DrawText(surface, HeaderCenterX, EditPromptY, "save name:", FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
            resources.FontRenderer.DrawBlendText(surface, HeaderCenterX, EditValueY, editValue, FontKind.Small, FontAlignment.Center, 15, 3);
        }

        resources.FontRenderer.DrawDark(surface, HeaderCenterX, FooterY, BuildFooterText(), FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private IList<SaveSlotInfo> GetPageSlots(int pageIndex)
    {
        return _catalog.Slots.Where(slot => slot.PageIndex == pageIndex).ToArray();
    }

    private int GetPageSlotCount(int pageIndex)
    {
        return GetPageSlots(pageIndex).Count + 1;
    }

    private static int? HitTestRow(int x, int y, int rowCount)
    {
        if (x < NameColumnX || x > 310)
        {
            return null;
        }

        for (int i = 0; i < rowCount; i++)
        {
            int top = RowStartY + (i * RowHeight);
            int bottom = top + RowHitHeight;
            if (y >= top && y <= bottom)
            {
                return i;
            }
        }

        return null;
    }

    private static string BuildDetailLine(SaveSlotInfo slot)
    {
        return slot.IsEmpty
            ? string.Format("slot {0}: empty", slot.SlotIndex)
            : string.Format("slot {0}: level {1}  cubes:{2}  cash:{3}/{4}", slot.SlotIndex, slot.LevelNumber, slot.CubeCount, slot.Cash, slot.Cash2);
    }

    private IScene? ExecuteSelectedSlot(SceneResources resources, int pageSlotCount)
    {
        IList<SaveSlotInfo> pageSlots = GetPageSlots(_pageIndex);
        if (pageSlotCount <= 0 || _selectedIndex < 0)
        {
            return null;
        }

        if (_selectedIndex >= pageSlots.Count)
        {
            return ReturnToCaller();
        }

        SaveSlotInfo selectedSlot = pageSlots[_selectedIndex];
        return _mode == SaveBrowserMode.Load
            ? ExecuteLoad(resources, selectedSlot)
            : ExecuteSave(resources, selectedSlot);
    }

    private IScene? BeginSaveEdit(SceneResources resources, int pageSlotCount)
    {
        IList<SaveSlotInfo> pageSlots = GetPageSlots(_pageIndex);
        if (pageSlotCount <= 0 || _selectedIndex < 0)
        {
            return null;
        }

        if (_selectedIndex >= pageSlots.Count)
        {
            return ReturnToCaller();
        }

        SaveSlotInfo selectedSlot = pageSlots[_selectedIndex];
        if (resources.TextEntrySource is null)
        {
            return ExecuteSave(resources, selectedSlot);
        }

        _isEditingSaveName = true;
        _pendingSaveName = BuildInitialSaveName(selectedSlot);
        resources.TextEntrySource.ClearPendingText();
        return null;
    }

    private IScene? UpdateSaveNameEditor(SceneResources resources, bool cancelPressed, bool confirmPressed)
    {
        OpenTyrian.Platform.ITextEntrySource? textEntrySource = resources.TextEntrySource;
        if (textEntrySource is not null)
        {
            int backspaceCount = textEntrySource.ConsumeBackspaceCount();
            bool consumedBackspace = backspaceCount > 0;
            while (backspaceCount > 0 && _pendingSaveName.Length > 0)
            {
                _pendingSaveName = _pendingSaveName.Substring(0, _pendingSaveName.Length - 1);
                backspaceCount--;
            }

            AppendSaveNameText(textEntrySource.ConsumeText());
            if (cancelPressed && !consumedBackspace)
            {
                SceneAudio.PlayCancel(resources);
                _isEditingSaveName = false;
                textEntrySource.ClearPendingText();
                return null;
            }
        }

        if (confirmPressed)
        {
            SceneAudio.PlayConfirm(resources);
            IList<SaveSlotInfo> pageSlots = GetPageSlots(_pageIndex);
            if (_selectedIndex < 0 || _selectedIndex >= pageSlots.Count)
            {
                _isEditingSaveName = false;
                return null;
            }

            SaveSlotInfo selectedSlot = pageSlots[_selectedIndex];
            string saveName = string.IsNullOrWhiteSpace(_pendingSaveName)
                ? BuildDefaultSaveName()
                : _pendingSaveName.Trim();
            _isEditingSaveName = false;
            return ExecuteSave(resources, selectedSlot, saveName);
        }

        return null;
    }

    private IScene? ExecuteLoad(SceneResources resources, SaveSlotInfo selectedSlot)
    {
        if (selectedSlot.IsEmpty || resources.UserFileStore is null)
        {
            return null;
        }

        SaveGameFile saveFile = SaveGameFileManager.Load(resources.UserFileStore);
        if (selectedSlot.SlotIndex < 1 || selectedSlot.SlotIndex > saveFile.Slots.Count)
        {
            return null;
        }

        SaveSlotRecord slot = saveFile.Slots[selectedSlot.SlotIndex - 1];
        EpisodeInfo? episode = ResolveEpisode(resources.Episodes, slot);
        if (episode is null)
        {
            return null;
        }

        EpisodeSessionState loadedSession = new EpisodeSessionState(episode.StartInfo, GameStartMode.FullGame);
        loadedSession.ApplySaveSlotRecord(slot, selectedSlot.SlotIndex > 11 ? 2 : 1);
        return new FullGameMenuScene(loadedSession);
    }

    private IScene? ExecuteSave(SceneResources resources, SaveSlotInfo selectedSlot)
    {
        return ExecuteSave(resources, selectedSlot, BuildInitialSaveName(selectedSlot));
    }

    private IScene? ExecuteSave(SceneResources resources, SaveSlotInfo selectedSlot, string slotName)
    {
        if (resources.UserFileStore is null)
        {
            return null;
        }

        SaveGameFile saveFile = SaveGameFileManager.Load(resources.UserFileStore);
        if (selectedSlot.SlotIndex < 1 || selectedSlot.SlotIndex > saveFile.Slots.Count)
        {
            return null;
        }

        SaveSlotRecord slot = saveFile.Slots[selectedSlot.SlotIndex - 1];
        _sessionState.WriteToSaveSlotRecord(slot, slotName);
        SaveGameFileManager.Save(resources.UserFileStore, saveFile);

        SaveSlotCatalog updatedCatalog = saveFile.ToCatalog();
        if (resources.SaveCatalogUpdater is not null)
        {
            resources.SaveCatalogUpdater(updatedCatalog);
        }

        return ReturnToCaller();
    }

    private static EpisodeInfo? ResolveEpisode(IList<EpisodeInfo> episodes, SaveSlotRecord slot)
    {
        int episodeNumber = slot.EpisodeNumber;
        if (string.Equals(slot.LevelName, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            episodeNumber = episodeNumber >= 4 ? 1 : episodeNumber + 1;
        }

        for (int i = 0; i < episodes.Count; i++)
        {
            if (episodes[i].EpisodeNumber == episodeNumber)
            {
                return episodes[i];
            }
        }

        return episodes.Count > 0 ? episodes[0] : null;
    }

    private string BuildFooterText()
    {
        if (_isEditingSaveName)
        {
            return "Type ASCII name  Backspace delete  Enter saves  Esc cancels";
        }

        if (_mode == SaveBrowserMode.Load)
        {
            return "Left or right for 1 or 2 player game.";
        }

        return "Left or right changes page.  Enter saves.  Esc returns.";
    }

    private string BuildInitialSaveName(SaveSlotInfo selectedSlot)
    {
        string slotName = !selectedSlot.IsEmpty && !string.IsNullOrWhiteSpace(selectedSlot.Name)
            ? selectedSlot.Name
            : BuildDefaultSaveName();

        return NormalizeSaveName(slotName);
    }

    private string BuildDefaultSaveName()
    {
        return string.Format("EP{0}-LV{1}", _sessionState.CurrentEpisodeNumber, _sessionState.SaveLevel);
    }

    private void AppendSaveNameText(string text)
    {
        if (string.IsNullOrEmpty(text) || _pendingSaveName.Length >= MaxSaveNameLength)
        {
            return;
        }

        foreach (char rawCharacter in text)
        {
            if (_pendingSaveName.Length >= MaxSaveNameLength)
            {
                break;
            }

            char character = NormalizeSaveNameCharacter(rawCharacter);
            if (character == '\0')
            {
                continue;
            }

            _pendingSaveName += character;
        }
    }

    private static string NormalizeSaveName(string text)
    {
        string result = string.Empty;
        foreach (char rawCharacter in text)
        {
            if (result.Length >= MaxSaveNameLength)
            {
                break;
            }

            char character = NormalizeSaveNameCharacter(rawCharacter);
            if (character == '\0')
            {
                continue;
            }

            result += character;
        }

        return result.Trim();
    }

    private static char NormalizeSaveNameCharacter(char character)
    {
        if (character >= 'a' && character <= 'z')
        {
            character = char.ToUpperInvariant(character);
        }

        return character >= 32 && character <= 126 ? character : '\0';
    }

    private IScene ReturnToCaller()
    {
        return _returnSceneFactory is not null ? _returnSceneFactory() : new OptionsScene(_sessionState);
    }

    private static void RenderSlotRow(IndexedFrameBuffer surface, TyrianFontRenderer fontRenderer, SaveSlotInfo slot, int y, bool isSelected)
    {
        byte nameHue = 13;
        int nameValue = isSelected ? 6 : (slot.IsEmpty ? 0 : 2);
        int detailValue = isSelected ? 6 : 2;

        fontRenderer.DrawText(surface, NameColumnX, y, slot.Name, FontKind.Tiny, FontAlignment.Left, nameHue, nameValue, shadow: true);
        fontRenderer.DrawText(surface, LastLevelColumnX, y, BuildLastLevelText(slot), FontKind.Tiny, FontAlignment.Left, 5, detailValue, shadow: true);

        if (!slot.IsEmpty)
        {
            fontRenderer.DrawText(surface, EpisodeColumnX, y, string.Format("EP {0}", slot.EpisodeNumber), FontKind.Tiny, FontAlignment.Left, 5, detailValue, shadow: true);
        }
    }

    private void RenderExitRow(IndexedFrameBuffer surface, TyrianFontRenderer fontRenderer, int y, bool isSelected)
    {
        string label = _mode == SaveBrowserMode.Load ? "Exit to Main Menu" : "Exit to Previous Menu";
        fontRenderer.DrawText(surface, NameColumnX, y, label, FontKind.Tiny, FontAlignment.Left, 13, isSelected ? 6 : 2, shadow: true);
    }

    private static string BuildLastLevelText(SaveSlotInfo slot)
    {
        return slot.IsEmpty
            ? "Last Level -----"
            : string.Format("Last Level {0}", slot.LevelName);
    }

    private SaveSlotInfo? GetSelectedSlot(IList<SaveSlotInfo> pageSlots)
    {
        if (_selectedIndex < 0 || _selectedIndex >= pageSlots.Count)
        {
            return null;
        }

        return pageSlots[_selectedIndex];
    }
}
