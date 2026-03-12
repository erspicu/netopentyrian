namespace OpenTyrian.Core;

public sealed class SaveSlotsScene : IScene
{
    private readonly EpisodeSessionState _sessionState;
    private readonly SaveSlotCatalog _catalog;
    private readonly SaveBrowserMode _mode;
    private OpenTyrian.Platform.InputSnapshot _previousInput;
    private int _pageIndex;
    private int _selectedIndex;

    public SaveSlotsScene(EpisodeSessionState sessionState, SaveSlotCatalog catalog, SaveBrowserMode mode)
    {
        _sessionState = sessionState;
        _catalog = catalog;
        _mode = mode;
        _pageIndex = 0;
        _selectedIndex = 0;
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
            return new OptionsScene(_sessionState);
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
            return ExecuteSelectedSlot(resources, pageSlotCount);
        }

        _previousInput = input;
        return null;
    }

    public void Render(IndexedFrameBuffer surface, SceneResources resources, double timeSeconds)
    {
        TitleScreenRenderer.RenderBackground(surface, resources, timeSeconds);
        TitleScreenRenderer.RenderTitleOverlay(surface, resources.FontRenderer, resources.PaletteCount);

        if (resources.FontRenderer is null)
        {
            return;
        }

        string title = _mode == SaveBrowserMode.Load ? "Load Game" : "Save Game";
        resources.FontRenderer.DrawShadowText(surface, 160, 78, title, FontKind.Normal, FontAlignment.Center, 15, 0, black: false, shadowDistance: 1);

        string status = !_catalog.HasSaveFile
            ? "tyrian.sav not found; showing empty slots"
            : _catalog.IsValid
                ? string.Format("source: {0}", Path.GetFileName(_catalog.SourcePath))
                : "tyrian.sav exists but failed validation; showing empty slots";
        resources.FontRenderer.DrawText(surface, 160, 90, status, FontKind.Tiny, FontAlignment.Center, 14, 0, shadow: true);

        IList<SaveSlotInfo> pageSlots = GetPageSlots(_pageIndex);
        for (int i = 0; i < pageSlots.Count; i++)
        {
            SaveSlotInfo slot = pageSlots[i];
            bool isSelected = i == _selectedIndex;
            int y = 100 + (i * 8);
            string line = BuildSlotLine(slot);

            if (isSelected)
            {
                resources.FontRenderer.DrawBlendText(surface, 18, y, $"> {line}", FontKind.Tiny, FontAlignment.Left, 15, 4);
            }
            else
            {
                resources.FontRenderer.DrawText(surface, 18, y, line, FontKind.Tiny, FontAlignment.Left, slot.IsEmpty ? (byte)8 : (byte)13, 0, shadow: true);
            }
        }

        SaveSlotInfo selectedSlot = pageSlots[_selectedIndex];
        resources.FontRenderer.DrawText(surface, 160, 194, BuildDetailLine(selectedSlot), FontKind.Tiny, FontAlignment.Center, 12, 0, shadow: true);
        resources.FontRenderer.DrawDark(surface, 160, 204, BuildFooterText(), FontKind.Tiny, FontAlignment.Center, black: false);
    }

    private IList<SaveSlotInfo> GetPageSlots(int pageIndex)
    {
        return _catalog.Slots.Where(slot => slot.PageIndex == pageIndex).ToArray();
    }

    private int GetPageSlotCount(int pageIndex)
    {
        return Math.Max(1, GetPageSlots(pageIndex).Count);
    }

    private static int? HitTestRow(int x, int y, int rowCount)
    {
        if (x < 12 || x > 308)
        {
            return null;
        }

        for (int i = 0; i < rowCount; i++)
        {
            int top = 98 + (i * 8);
            int bottom = top + 8;
            if (y >= top && y <= bottom)
            {
                return i;
            }
        }

        return null;
    }

    private static string BuildSlotLine(SaveSlotInfo slot)
    {
        return slot.IsEmpty
            ? string.Format("{0:00}  {1,-14}  last -----  ep --", slot.SlotIndex, slot.Name)
            : string.Format("{0:00}  {1,-14}  last {2,-10}  ep {3}", slot.SlotIndex, slot.Name, slot.LevelName, slot.EpisodeNumber);
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
        if (pageSlotCount <= 0 || _selectedIndex < 0 || _selectedIndex >= pageSlots.Count)
        {
            return null;
        }

        SaveSlotInfo selectedSlot = pageSlots[_selectedIndex];
        return _mode == SaveBrowserMode.Load
            ? ExecuteLoad(resources, selectedSlot)
            : ExecuteSave(resources, selectedSlot);
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
        string slotName = !slot.IsEmpty && !string.IsNullOrWhiteSpace(slot.Name)
            ? slot.Name
            : string.Format("EP{0}-LV{1}", _sessionState.CurrentEpisodeNumber, _sessionState.SaveLevel);
        _sessionState.WriteToSaveSlotRecord(slot, slotName);
        SaveGameFileManager.Save(resources.UserFileStore, saveFile);

        SaveSlotCatalog updatedCatalog = saveFile.ToCatalog();
        if (resources.SaveCatalogUpdater is not null)
        {
            resources.SaveCatalogUpdater(updatedCatalog);
        }

        return new OptionsScene(_sessionState);
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
        return _mode == SaveBrowserMode.Load
            ? "Left/Right page  Up/Down choose  Enter loads  Esc back"
            : "Left/Right page  Up/Down choose  Enter saves  Esc back";
    }
}
